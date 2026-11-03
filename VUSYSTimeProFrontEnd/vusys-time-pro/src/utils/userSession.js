import { STORAGE_KEYS } from "./storageKeys";
import { idbGet, idbSet } from "./idbStorage";
import { normalizeNumberArray } from "./valueUtils";

const USER_PROFILE_IMAGE_KEY_PREFIX = "timepro_user_profile_image";

const safeJsonParse = (value, fallback = null) => {
  try {
    return value ? JSON.parse(value) : fallback;
  } catch {
    return fallback;
  }
};

const getUserIdValue = (user = {}) =>
  user?.userId ?? user?.id ?? user?.Id ?? user?.ID ?? "";

const getUserProfileImageKey = (userOrId) => {
  const userId =
    typeof userOrId === "object" ? getUserIdValue(userOrId) : userOrId;

  return `${USER_PROFILE_IMAGE_KEY_PREFIX}:${String(userId || "default")}`;
};

const getProfileImageValue = (details = {}) =>
  details.UserImage ??
  details.userImage ??
  details.ProfileImage ??
  details.profileImage ??
  "";

const stripProfileImageFromUser = (user = {}) => {
  const {
    UserImage,
    userImage,
    ProfileImage,
    profileImage,
    ...userWithoutProfileImage
  } = user;

  void UserImage;
  void userImage;
  void ProfileImage;
  void profileImage;

  return userWithoutProfileImage;
};

const normalizeUser = (details = {}) => {
  const profileImage = getProfileImageValue(details);
  const detailsWithoutProfileImage = stripProfileImageFromUser(details);
  const securityAccessIds = normalizeNumberArray(
    details.security_access_ids ??
      details.securityAccessIds ??
      details.SecurityAccessIds ??
      [],
  );

  return {
    ...detailsWithoutProfileImage,
    id: details.Id ?? details.ID ?? details.id ?? "",
    userId: details.Id ?? details.ID ?? details.userId ?? details.id ?? "",
    userName: details.UserName ?? details.userName ?? "",
    name: details.FullName ?? details.name ?? "",
    fullName: details.FullName ?? details.fullName ?? "",
    email: details.EmailID ?? details.email ?? "",
    mobileNo: details.MobileNo ?? details.mobileNo ?? "",
    employeeCode: details.EmployeeCode ?? details.employeeCode ?? "",
    employeeID: details.EmployeeCode ?? details.employeeID ?? "",
    roleId: details.RoleID ?? details.roleId ?? "",
    role: details.UserRole ?? details.user_role ?? details.role ?? "Employee",
    isReset: Boolean(details.IsReset ?? details.isReset),
    isApprover: Boolean(details.IsApprover ?? details.isApprover),
    twoFactorEnabled: Boolean(
      details.IsEnableTwoFactorAuthentication ?? details.twoFactorEnabled,
    ),
    profileImage,
    userImageFileName:
      details.UserImageFileName ?? details.userImageFileName ?? "",
    securityAccessIds,
    security_access_ids: securityAccessIds,
  };
};

const saveProfileImageToIndexedDb = (user = {}) => {
  const profileImage = String(user?.profileImage || "");

  if (!profileImage) {
    return;
  }

  void idbSet(getUserProfileImageKey(user), profileImage).catch(() => {
    /*
     * IndexedDB profile-image persistence must never interrupt Login,
     * OTP completion or User Profile updates.
     */
  });
};

/**
 * Persists auth_user without the base64 profile image.
 * The image is stored separately in IndexedDB.
 */
export function saveLoginSession(details) {
  const user = normalizeUser(details);

  localStorage.setItem(
    STORAGE_KEYS.user,
    JSON.stringify(stripProfileImageFromUser(user)),
  );

  saveProfileImageToIndexedDb(user);

  return user;
}

/** Loads a user's profile image from IndexedDB. */
export async function getUserProfileImage(userOrId) {
  try {
    return (await idbGet(getUserProfileImageKey(userOrId))) || "";
  } catch {
    return "";
  }
}

export function setAuthStage(stage) {
  if (stage) localStorage.setItem(STORAGE_KEYS.authStage, stage);
  else localStorage.removeItem(STORAGE_KEYS.authStage);
}

export function getAuthStage() {
  try {
    return localStorage.getItem(STORAGE_KEYS.authStage) || "";
  } catch {
    return "";
  }
}

export function clearLoginSession() {
  [
    STORAGE_KEYS.token,
    STORAGE_KEYS.refreshToken,
    STORAGE_KEYS.tokenExpiryDateTime,
    STORAGE_KEYS.refreshTokenExpiryDateTime,
    STORAGE_KEYS.tokenExpirationInMinutes,
    STORAGE_KEYS.refreshTokenExpiresAt,
    STORAGE_KEYS.user,
    STORAGE_KEYS.authStage,
  ].forEach((key) => localStorage.removeItem(key));

  sessionStorage.removeItem(STORAGE_KEYS.pendingTwoFactorUser);
}

/**
 * Safely returns the authenticated user from localStorage.
 *
 * Legacy migration:
 * If an older auth_user still contains UserImage/profileImage, move that
 * image to IndexedDB and immediately rewrite auth_user without the image.
 */
export function getUser() {
  try {
    const storedUser = safeJsonParse(
      localStorage.getItem(STORAGE_KEYS.user),
      null,
    );

    if (!storedUser) {
      return null;
    }

    const user = normalizeUser(storedUser);
    const legacyProfileImage = getProfileImageValue(storedUser);

    if (legacyProfileImage) {
      saveProfileImageToIndexedDb({
        ...user,
        profileImage: legacyProfileImage,
      });

      localStorage.setItem(
        STORAGE_KEYS.user,
        JSON.stringify(stripProfileImageFromUser(user)),
      );

      return {
        ...user,
        profileImage: legacyProfileImage,
      };
    }

    return user;
  } catch {
    return null;
  }
}

export function getCreatedOrUpdatedBy() {
  const user = getUser();
  return String(user?.userId ?? user?.id ?? user?.Id ?? "");
}

export function getRoleId() {
  const user = getUser();
  return String(user?.roleId ?? user?.RoleID ?? "");
}

export function getAppVersion() {
  try {
    return String(
      localStorage.getItem(STORAGE_KEYS.appVersion) ||
        getUser()?.AppVersionNo ||
        getUser()?.appVersionNo ||
        "1.0",
    );
  } catch {
    return "1.0";
  }
}

export function getEmployeeID() {
  const user = getUser();
  return String(
    user?.employeeID ?? user?.employeeCode ?? user?.EmployeeCode ?? "",
  );
}

export function getUserName() {
  const user = getUser();
  return String(
    user?.userName ?? user?.UserName ?? user?.name ?? user?.email ?? "",
  );
}

export function getUserEmailID() {
  const user = getUser();
  return String(
    user?.email || user?.EmailID || user?.emailID || "",
  );
}

/** Returns the normalized security access IDs for the authenticated user. */
export function getSecurityAccessIds() {
  const user = getUser();

  return normalizeNumberArray(
    user?.securityAccessIds ??
      user?.security_access_ids ??
      user?.SecurityAccessIds ??
      [],
  );
}

/**
 * Returns Logged In Employee Code.
 * @param {*} user 
 * @returns 
 */
export const getLoggedInEmployeeCode = (user) =>
  String(
    user?.employeeCode ??
      user?.employeeID ??
      user?.EmployeeCode ??
      user?.EmployeeID ??
      "",
  ).trim();


  /**
   * Returns Logged In Employee Name
   * @param {*} user 
   * @returns 
   */
  export const getLoggedInEmployeeName = (user) =>
    String(
      user?.fullName ??
        user?.name ??
        user?.FullName ??
        user?.userName ??
        "Employee",
    ).trim();
