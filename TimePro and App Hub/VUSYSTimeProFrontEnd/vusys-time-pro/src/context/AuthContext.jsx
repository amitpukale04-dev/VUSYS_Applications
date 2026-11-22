import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { STORAGE_KEYS } from "../utils/storageKeys";
import { loginUser, logoutUser } from "../services/authService";
import {
  beginExplicitLogout,
  cancelExplicitLogout,
  clearAuthenticationRuntimeState,
  storeAuthenticationTokens,
} from "../services/axiosSetup";
import { b64Decode } from "../utils/base64EncodeDecode";
import {
  clearLoginSession,
  getAuthStage,
  getUser,
  getUserProfileImage,
  saveLoginSession,
  setAuthStage,
} from "../utils/userSession";
import {
  clearUserAccess,
  clearUserActionAccess,
  clearUserMenuAccess,
  getStoredUserActionAccess,
  getStoredUserMenuAccess,
  hasStoredUserActionAccess,
  hasStoredUserMenuAccess,
} from "../utils/menuUtils";

import {
  getUserMenuAccess,
  getUserActionAccess,
} from "../services/menuAccessService";
import { getUserSecurityAccessList } from "../services/securityAccessService";
import {
  getUserSecurityAccessIds as getDynamicUserSecurityAccessIds,
  normalizeUserSecurityAccessItems,
} from "../utils/securityPermissionUtils";

const AuthContext = createContext(null);

const readJson = (storage, key, fallback = null) => {
  try {
    return JSON.parse(storage.getItem(key)) ?? fallback;
  } catch {
    return fallback;
  }
};

const getUserId = (user) =>
  user?.userId ?? user?.id ?? user?.Id ?? user?.ID ?? 0;
const getUserRoleId = (user) =>
  user?.roleId ?? user?.RoleID ?? user?.roleID ?? 0;
const getUserSecurityAccessIds = (user) =>
  user?.securityAccessIds ??
  user?.security_access_ids ??
  user?.SecurityAccessIds ??
  [];

export function AuthProvider({ children }) {
  const [user, setUser] = useState(getUser);
  const [authStage, setAuthStageState] = useState(() => {
    const savedStage = getAuthStage();
    if (savedStage) return savedStage;
    return getUser() ? "authenticated" : "";
  });
  const [pendingTwoFactorUser, setPendingTwoFactorUser] = useState(() =>
    readJson(sessionStorage, STORAGE_KEYS.pendingTwoFactorUser, null),
  );
  const [menuItems, setMenuItems] = useState(getStoredUserMenuAccess);
  const [actionAccessItems, setActionAccessItems] = useState(
    getStoredUserActionAccess,
  );
  const [userSecurityAccessItems, setUserSecurityAccessItems] = useState([]);
  const [userSecurityAccessLoading, setUserSecurityAccessLoading] = useState(
    () => Boolean(getUser() && getAuthStage() === "authenticated"),
  );
  const [menuAccessLoading, setMenuAccessLoading] = useState(() => {
    const existingUser = getUser();
    const existingStage = getAuthStage();
    return Boolean(
      existingUser &&
      existingStage === "authenticated" &&
      (!hasStoredUserMenuAccess() || !hasStoredUserActionAccess()),
    );
  });

  const accessInitializedDuringAuthenticationRef = useRef(false);

  const currentUserId = getUserId(user);
  const currentProfileImage = user?.profileImage || "";

  /*
   * auth_user intentionally excludes the base64 profile image.
   * Restore it from IndexedDB after a browser reload.
   */
  useEffect(() => {
    if (!currentUserId || currentProfileImage) {
      return undefined;
    }

    let cancelled = false;

    getUserProfileImage(currentUserId).then((profileImage) => {
      if (cancelled || !profileImage) {
        return;
      }

      setUser((currentUser) => {
        if (!currentUser || getUserId(currentUser) !== currentUserId) {
          return currentUser;
        }

        return {
          ...currentUser,
          profileImage,
        };
      });
    });

    return () => {
      cancelled = true;
    };
  }, [currentProfileImage, currentUserId]);

  const updateStage = (stage) => {
    setAuthStage(stage);
    setAuthStageState(stage);
  };

  // ==========================================================
  // LOAD AUTHENTICATED USER SECURITY ACCESS
  // ==========================================================

  const loadUserSecurityAccess = async (signedInUser = user) => {
    if (!signedInUser) {
      setUserSecurityAccessItems([]);
      setUserSecurityAccessLoading(false);
      return null;
    }

    const userId = getUserId(signedInUser);
    const roleId = getUserRoleId(signedInUser);

    if (!userId || !roleId) {
      setUserSecurityAccessItems([]);
      setUserSecurityAccessLoading(false);
      return signedInUser;
    }

    setUserSecurityAccessLoading(true);

    try {
      const result = await getUserSecurityAccessList({
        userID: userId,
        roleID: roleId,
      });

      const permissions = normalizeUserSecurityAccessItems(
        result?.selectedRows || [],
      );
      const dynamicSecurityAccessIds =
        getDynamicUserSecurityAccessIds(permissions);

      setUserSecurityAccessItems(permissions);

      /*
       * Keep only the dynamically loaded IDs on auth_user for existing menu
       * filtering helpers. The frontend never defines what those IDs are.
       */
      const updatedUser = saveLoginSession({
        ...signedInUser,
        securityAccessIds: dynamicSecurityAccessIds,
        security_access_ids: dynamicSecurityAccessIds,
        SecurityAccessIds: dynamicSecurityAccessIds,
      });

      setUser(updatedUser);

      return updatedUser;
    } catch (error) {
      setUserSecurityAccessItems([]);

      /*
       * Fail closed: if the selected-access API cannot be loaded, do not fall
       * back to Security IDs that may have arrived in the Login response.
       */
      const updatedUser = saveLoginSession({
        ...signedInUser,
        securityAccessIds: [],
        security_access_ids: [],
        SecurityAccessIds: [],
      });

      setUser(updatedUser);

      return updatedUser;
    } finally {
      setUserSecurityAccessLoading(false);
    }
  };

  const loadMenuAccess = async (signedInUser) => {
    if (!signedInUser) {
      clearUserAccess();
      setMenuItems([]);
      setActionAccessItems([]);
      return { menuItems: [], actionAccessItems: [] };
    }

    setMenuAccessLoading(true);

    const request = {
      userId: getUserId(signedInUser),
      roleID: getUserRoleId(signedInUser),
      securityAccessIds: getUserSecurityAccessIds(signedInUser),
    };

    let loadedMenuItems = [];
    let loadedActionAccessItems = [];

    try {
      const menuResult = await getUserMenuAccess(request);
      loadedMenuItems = menuResult.menuItems || [];
      setMenuItems(loadedMenuItems);
    } catch (error) {
      /*
       * Sidebar/Search/ProtectedRoute are controlled only by paramType=Menu.
       * If that request fails, expose no backend-controlled menu.
       */
      clearUserMenuAccess();
      setMenuItems([]);
    }

    try {
      const actionResult = await getUserActionAccess(request);
      loadedActionAccessItems = actionResult.actionAccessItems || [];
      setActionAccessItems(loadedActionAccessItems);
    } catch (error) {
      /*
       * Action authorization is controlled only by paramType=Action.
       * If that request fails, keep menu navigation available but deny actions.
       */
      clearUserActionAccess();
      setActionAccessItems([]);
    } finally {
      setMenuAccessLoading(false);
    }

    return {
      menuItems: loadedMenuItems,
      actionAccessItems: loadedActionAccessItems,
    };
  };

  const completeAuthentication = async (signedInUser) => {
    sessionStorage.removeItem(STORAGE_KEYS.pendingTwoFactorUser);
    setPendingTwoFactorUser(null);
    setUser(signedInUser);

    const userWithSecurityAccess =
      (await loadUserSecurityAccess(signedInUser)) || signedInUser;

    await loadMenuAccess(userWithSecurityAccess);

    /* Prevent the authenticated-stage restore effect from duplicating the
       same API calls immediately after a successful Login/OTP completion. */
    accessInitializedDuringAuthenticationRef.current = true;

    updateStage("authenticated");
  };

  /*
   * Restore menu access when the browser reloads with an authenticated user
   * but the menu cache is unavailable (for example after a version update or
   * storage cleanup).
   */
  useEffect(() => {
    if (!user || authStage !== "authenticated") {
      return;
    }

    if (accessInitializedDuringAuthenticationRef.current) {
      accessInitializedDuringAuthenticationRef.current = false;
      return;
    }

    let cancelled = false;

    const restoreAuthenticatedAccess = async () => {
      const userWithSecurityAccess =
        (await loadUserSecurityAccess(user)) || user;

      if (cancelled) return;

      /*
       * On browser reload, re-read both User Security Access and Menu/Action
       * access from the backend so cached permissions cannot become stale.
       */
      await loadMenuAccess(userWithSecurityAccess);
    };

    restoreAuthenticatedAccess();

    return () => {
      cancelled = true;
    };
  }, [authStage]);

  const login = async ({ email, password }) => {
    if (!email || !password) {
      throw new Error("Email ID and password are required.");
    }

    const { details } = await loginUser({ email, password });
    storeAuthenticationTokens(details);
    const signedInUser = saveLoginSession(details);
    setUser(signedInUser);

    if (signedInUser.isReset) {
      sessionStorage.removeItem(STORAGE_KEYS.pendingTwoFactorUser);
      setPendingTwoFactorUser(null);
      clearUserAccess();
      setMenuItems([]);
      setActionAccessItems([]);
      updateStage("reset-password");
      return {
        user: signedInUser,
        requiresPasswordReset: true,
        requiresTwoFactor: false,
      };
    }

    if (signedInUser.twoFactorEnabled) {
      sessionStorage.setItem(
        STORAGE_KEYS.pendingTwoFactorUser,
        JSON.stringify(signedInUser),
      );
      setPendingTwoFactorUser(signedInUser);
      clearUserAccess();
      setMenuItems([]);
      setActionAccessItems([]);
      updateStage("otp");
      return {
        user: signedInUser,
        requiresPasswordReset: false,
        requiresTwoFactor: true,
      };
    }

    await completeAuthentication(signedInUser);
    return {
      user: signedInUser,
      requiresPasswordReset: false,
      requiresTwoFactor: false,
    };
  };

  const verifyOtp = async (otp) => {
    const normalizedOtp = String(otp || "").trim();

    if (!/^\d{4}$/.test(normalizedOtp)) {
      throw new Error("Enter the Valid OTP");
    }

    const otpUser = pendingTwoFactorUser || getUser();
    if (!otpUser) {
      throw new Error("Your OTP session has expired. Please sign in again.");
    }

    const decodedOtp = String(b64Decode(otpUser.otp || "")).trim();
    if (!decodedOtp || normalizedOtp !== decodedOtp) {
      throw new Error("Enter the Valid OTP");
    }

    await completeAuthentication(otpUser);
    return otpUser;
  };

  const cancelOtp = () => {
    clearLoginSession();
    clearUserAccess();
    sessionStorage.removeItem(STORAGE_KEYS.pendingTwoFactorUser);
    setPendingTwoFactorUser(null);
    setUser(null);
    setMenuItems([]);
    setActionAccessItems([]);
    setUserSecurityAccessItems([]);
    setUserSecurityAccessLoading(false);
    setAuthStageState("");
  };

  const updateUser = (updates) => {
    if (!user) return null;

    /*
     * Keep profileImage in IndexedDB while updating AuthContext immediately
     * so Header/UserProfile reflect the change without a reload.
     */
    const updatedUser = saveLoginSession({
      ...user,
      ...updates,
    });

    setUser(updatedUser);

    return updatedUser;
  };

  const setTwoFactorEnabled = (enabled) => {
    if (!user) return;
    updateUser({
      twoFactorEnabled: Boolean(enabled),
      IsEnableTwoFactorAuthentication: Boolean(enabled),
    });
  };

  const refreshUserSecurityAccess = async (signedInUser = user) =>
    loadUserSecurityAccess(signedInUser);

  const refreshMenuAccess = async (signedInUser = user) =>
    loadMenuAccess(signedInUser);

  const logout = async () => {
    const emailID = user?.email || user?.EmailID || user?.emailID || "";

    if (!emailID) {
      throw new Error("Email ID is not available.");
    }

    /*
     * Mark explicit Logout BEFORE calling the Logout API.
     *
     * This prevents any parallel protected request/token check from
     * interpreting the Logout transition as Session Expiration.
     */
    beginExplicitLogout();

    try {
      const result = await logoutUser({
        email: emailID,
      });

      /*
       * Clear Axios runtime/session-expiry state before clearing the
       * persisted login/session data.
       */
      clearAuthenticationRuntimeState();

      // Prevent the next account from inheriting this user's protected URL.
      // Keep this marker until standard Login or OTP authentication finishes.
      sessionStorage.setItem(STORAGE_KEYS.redirectHomeAfterLogout, "true");

      clearLoginSession();
      clearUserAccess();
      /*
       * Preserve the user's opened application tabs across Logout/Login.
       * The tabs are still cleared when the user explicitly chooses
       * Close All Tabs through TabsContext.
       */
      sessionStorage.removeItem(STORAGE_KEYS.pendingTwoFactorUser);

      setPendingTwoFactorUser(null);
      setUser(null);
      setMenuItems([]);
      setActionAccessItems([]);
      setUserSecurityAccessItems([]);
      setUserSecurityAccessLoading(false);
      setMenuAccessLoading(false);
      setAuthStageState("");

      /*
       * Do NOT cancel explicit Logout here.
       *
       * The caller will navigate to /login immediately after this method
       * succeeds. The in-memory flag is automatically reset on the next
       * successful Login/RefreshToken through storeAuthenticationTokens()
       * -> markSessionRestored().
       */
      return result;
    } catch (error) {
      /*
       * Logout failed, so the authenticated session should continue.
       * Restore normal Session Timeout / token refresh behavior.
       */
      cancelExplicitLogout();

      throw error;
    }
  };

  const value = useMemo(
    () => ({
      user,
      authStage,
      isAuthenticated: Boolean(user) && authStage === "authenticated",
      requiresPasswordReset: Boolean(user) && authStage === "reset-password",
      pendingTwoFactorUser,
      menuItems,
      actionAccessItems,
      userSecurityAccessItems,
      userSecurityAccessLoading,
      menuAccessLoading,
      login,
      verifyOtp,
      cancelOtp,
      updateUser,
      setTwoFactorEnabled,
      refreshUserSecurityAccess,
      refreshMenuAccess,
      logout,
    }),
    [
      user,
      authStage,
      pendingTwoFactorUser,
      menuItems,
      actionAccessItems,
      userSecurityAccessItems,
      userSecurityAccessLoading,
      menuAccessLoading,
    ],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);
