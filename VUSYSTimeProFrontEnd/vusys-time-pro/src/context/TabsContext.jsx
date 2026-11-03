import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { STORAGE_KEYS } from "../utils/storageKeys";
import { APP_CONFIG } from "../utils/environmentConfig";
import {
  getUserTabList,
  saveAndUpdateUserTabs,
} from "../services/frontEndUserTabsService";
import { useAuth } from "./AuthContext";
import { getMenuItemByPath } from "../utils/menuUtils";
import DirtyStateModal from "../components/common/DirtyStateModal";
import { ROUTE_CONSTANTS } from "../constants/RouteConstants";

const TabsContext = createContext(null);

// ============================================================
// HOME TAB
// ============================================================

const HOME_TAB = {
  path: "/",
  label: "Home",
  svgIcon: "home",
  tone: "indigo",
  closable: false,
};

// ============================================================
// NORMALIZE STORED TABS
// ============================================================

function normalizeStoredTabs(tabs) {
  const normalized = [];

  for (const tab of tabs) {
    /*
     * Employee View/Edit/Add routes must continue to use the
     * single Employee List tab.
     */
    const nextTab = tab?.path?.startsWith(
      `${ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES}/`,
    )
      ? {
          path: ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES,
          label: "Employee List",
          svgIcon: "employees",
          tone: "rose",
          closable: true,
        }
      : tab?.path?.startsWith(
            `${ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS}/`,
          )
        ? {
            path: ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS,
            label: "My Leave Requests",
            svgIcon: "leaveType",
            tone: "violet",
            closable: true,
          }
        : tab?.path?.startsWith(
              `${ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE}/`,
            )
          ? {
              path: ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE,
              label: "Employee Leave Balance List",
              svgIcon: "leaveBalance",
              tone: "emerald",
              closable: true,
            }
          : tab?.path?.startsWith(`${ROUTE_CONSTANTS.ADMINISTRATION.USERS}/`)
            ? {
                path: ROUTE_CONSTANTS.ADMINISTRATION.USERS,
                label: "User List",
                svgIcon: "employees",
                tone: "indigo",
                closable: true,
              }
            : tab?.path?.startsWith(
                  `${ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION}/`,
                )
              ? {
                  path: ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION,
                  label: "Email Configuration List",
                  svgIcon: "settings",
                  tone: "indigo",
                  closable: true,
                }
              : tab?.path?.startsWith(
                    `${ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET}/`,
                  )
                ? {
                    path: ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET,
                    label: "Log Timesheet",
                    svgIcon: "timeEntry",
                    tone: "coral",
                    closable: true,
                  }
                : tab;

    /*
     * Ignore invalid and duplicate tabs.
     */
    if (
      !nextTab?.path ||
      normalized.some((item) => item.path === nextTab.path)
    ) {
      continue;
    }

    normalized.push(nextTab);
  }

  return normalized.length ? normalized : [HOME_TAB];
}

// ============================================================
// USER-SPECIFIC LOCAL STORAGE (WHEN SERVER STORAGE IS DISABLED)
// ============================================================

function getLocalTabsStorageKey(userId, roleId) {
  return `${STORAGE_KEYS.tabs}:${userId}:${roleId}`;
}

function readLocalUserTabs(userId, roleId) {
  try {
    const stored = localStorage.getItem(getLocalTabsStorageKey(userId, roleId));
    const parsed = stored ? JSON.parse(stored) : [];
    return normalizeStoredTabs([HOME_TAB, ...(Array.isArray(parsed) ? parsed : [])]);
  } catch {
    return [HOME_TAB];
  }
}

// ============================================================
// SERVER-SIDE USER TABS
// ============================================================

const getTabUserId = (user) =>
  Number(user?.userId ?? user?.id ?? user?.Id ?? user?.ID ?? 0);
const getTabRoleId = (user) =>
  Number(user?.roleId ?? user?.RoleID ?? user?.roleID ?? 0);

// Deduplicate the GET while React StrictMode replays mount effects.
// A new login token produces a new key and therefore a new GET.
const pendingTabLoads = new Map();
function loadTabsOnce(userId, roleId) {
  const token = localStorage.getItem(STORAGE_KEYS.token) || "";
  const key = `${userId}:${roleId}:${token}`;
  const existing = pendingTabLoads.get(key);
  if (existing) return existing;
  const promise = getUserTabList({ userId, roleId });
  pendingTabLoads.set(key, promise);
  // Keep the completed promise for this authentication session so that
  // StrictMode and unrelated AuthContext updates never re-fetch.
  return promise;
}

// ============================================================
// RESOLVE ROUTE TAB
// ============================================================

function resolveRouteTab(pathname, menuItems) {
  // ----------------------------------------------------------
  // HOME
  // ----------------------------------------------------------

  if (pathname === "/") {
    return HOME_TAB;
  }

  // ----------------------------------------------------------
  // USER PROFILE
  // ----------------------------------------------------------

  if (pathname === ROUTE_CONSTANTS.AUTH.USER_PROFILE) {
    return {
      path: ROUTE_CONSTANTS.AUTH.USER_PROFILE,
      label: "User Profile",
      svgIcon: "employees",
      tone: "cyan",
      closable: true,
    };
  }

  // ----------------------------------------------------------
  // CHANGE PASSWORD
  // ----------------------------------------------------------

  if (pathname === ROUTE_CONSTANTS.AUTH.CHANGE_PASSWORD) {
    return {
      path: ROUTE_CONSTANTS.AUTH.CHANGE_PASSWORD,
      label: "Change Password",
      svgIcon: "role",
      tone: "violet",
      closable: true,
    };
  }

  // ----------------------------------------------------------
  // EMPLOYEE ADD / VIEW / EDIT
  // ----------------------------------------------------------

  /*
   * Employee List, Add Employee, View Employee and Edit Employee
   * all use the same Employee List tab.
   */
  if (
    pathname === ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES ||
    pathname.startsWith(`${ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES}/`)
  ) {
    const employeeMenu = getMenuItemByPath(
      ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES,
      menuItems,
    );

    return {
      path: ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES,

      label: employeeMenu?.label || "Employee List",

      svgIcon: employeeMenu?.svgIcon || "employees",

      tone: employeeMenu?.tone || "rose",

      closable: true,
    };
  }

  // ----------------------------------------------------------
  // MY LEAVE REQUESTS ADD / VIEW
  // ----------------------------------------------------------

  if (
    pathname === ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS ||
    pathname.startsWith(
      `${ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS}/`,
    )
  ) {
    const menu = getMenuItemByPath(
      ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS,
      menuItems,
    );
    return {
      path: ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS,
      label: menu?.label || "My Leave Requests",
      svgIcon: menu?.svgIcon || "leaveType",
      tone: menu?.tone || "violet",
      closable: true,
    };
  }

  // ----------------------------------------------------------
  // EMPLOYEE LEAVE BALANCE ADD / VIEW / EDIT
  // ----------------------------------------------------------

  if (
    pathname === ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE ||
    pathname.startsWith(
      `${ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE}/`,
    )
  ) {
    const leaveBalanceMenu = getMenuItemByPath(
      ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE,
      menuItems,
    );

    return {
      path: ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE,
      label: leaveBalanceMenu?.label || "Employee Leave Balance List",
      svgIcon: leaveBalanceMenu?.svgIcon || "leaveBalance",
      tone: leaveBalanceMenu?.tone || "emerald",
      closable: true,
    };
  }

  // ----------------------------------------------------------
  // USER ADD / VIEW / EDIT
  // ----------------------------------------------------------

  if (
    pathname === ROUTE_CONSTANTS.ADMINISTRATION.USERS ||
    pathname.startsWith(`${ROUTE_CONSTANTS.ADMINISTRATION.USERS}/`)
  ) {
    const userMenu = getMenuItemByPath(
      ROUTE_CONSTANTS.ADMINISTRATION.USERS,
      menuItems,
    );

    return {
      path: ROUTE_CONSTANTS.ADMINISTRATION.USERS,
      label: userMenu?.label || "User List",
      svgIcon: userMenu?.svgIcon || "employees",
      tone: userMenu?.tone || "indigo",
      closable: true,
    };
  }

  // ----------------------------------------------------------
  // EMAIL CONFIGURATION ADD / VIEW / EDIT
  // ----------------------------------------------------------

  if (
    pathname === ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION ||
    pathname.startsWith(`${ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION}/`)
  ) {
    const emailConfigurationMenu = getMenuItemByPath(
      ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION,
      menuItems,
    );

    return {
      path: ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION,
      label: emailConfigurationMenu?.label || "Email Configuration List",
      svgIcon: emailConfigurationMenu?.svgIcon || "settings",
      tone: emailConfigurationMenu?.tone || "indigo",
      closable: true,
    };
  }

  // ----------------------------------------------------------
  // LOG TIMESHEET ADD / VIEW / EDIT
  // ----------------------------------------------------------

  if (
    pathname === ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET ||
    pathname.startsWith(`${ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET}/`)
  ) {
    const logTimesheetMenu = getMenuItemByPath(
      ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET,
      menuItems,
    );

    return {
      path: ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET,
      label: logTimesheetMenu?.label || "Log Timesheet",
      svgIcon: logTimesheetMenu?.svgIcon || "timeEntry",
      tone: logTimesheetMenu?.tone || "coral",
      closable: true,
    };
  }

  // ----------------------------------------------------------
  // DYNAMIC API MENU
  // ----------------------------------------------------------

  const item = getMenuItemByPath(pathname, menuItems);

  if (!item) {
    return null;
  }

  return {
    path: item.path,
    label: item.label,
    svgIcon: item.svgIcon,
    tone: item.tone,
    closable: true,
  };
}

// ============================================================
// TABS PROVIDER
// ============================================================

export function TabsProvider({ children }) {
  const [tabs, setTabs] = useState([HOME_TAB]);
  const [dirtyTabs, setDirtyTabs] = useState({});
  const [pendingClosePath, setPendingClosePath] = useState("");
  const [loadedTabsStorageKey, setLoadedTabsStorageKey] = useState("");
  const lastSavedTabsRef = useRef("");
  const saveQueueRef = useRef(Promise.resolve());
  const loadGenerationRef = useRef(0);
  const activeUserKeyRef = useRef("");
  const lastSavedUserKeyRef = useRef("");

  const location = useLocation();

  const navigate = useNavigate();
  // In declarative React Router, navigate may change identity on navigation.
  // Keep the latest callback without making the tab-loading effect rerun
  // whenever the user switches between application screens.
  const navigateRef = useRef(navigate);
  navigateRef.current = navigate;

  const { isAuthenticated, menuItems = [], user } = useAuth();

  const userId = getTabUserId(user);
  const roleId = getTabRoleId(user);
  const tabsStorageKey =
    isAuthenticated && userId > 0 && roleId > 0 ? `${userId}:${roleId}` : "";

  // Never expose tabs belonging to the previous signed-in user while
  // React is switching authentication sessions and restoring stored tabs.
  const visibleTabs = useMemo(
    () =>
      isAuthenticated &&
      tabsStorageKey &&
      loadedTabsStorageKey === tabsStorageKey
        ? tabs
        : [HOME_TAB],
    [isAuthenticated, tabsStorageKey, loadedTabsStorageKey, tabs],
  );

  // ==========================================================
  // LOAD TABS ONCE FOR THE AUTHENTICATED USER
  // ==========================================================

  useEffect(() => {
    const generation = ++loadGenerationRef.current;
    activeUserKeyRef.current = tabsStorageKey;

    // Clear the previous user's state immediately. The GET response is the
    // only source of persisted tabs and is never allowed to race with saves.
    setLoadedTabsStorageKey("");
    setTabs([HOME_TAB]);
    setDirtyTabs({});
    setPendingClosePath("");
    lastSavedTabsRef.current = "";
    lastSavedUserKeyRef.current = "";

    if (!isAuthenticated || !tabsStorageKey) return;

    // Local mode must never invoke either User Tabs API.
    if (!APP_CONFIG.isEnableServerSideUserTabs) {
      const restoredTabs = readLocalUserTabs(userId, roleId);
      lastSavedTabsRef.current = JSON.stringify(restoredTabs);
      lastSavedUserKeyRef.current = tabsStorageKey;
      setTabs(restoredTabs);
      setLoadedTabsStorageKey(tabsStorageKey);
      return;
    }

    loadTabsOnce(userId, roleId)
      .then((serverTabs) => {
        if (loadGenerationRef.current !== generation) return;
        const restoredTabs = normalizeStoredTabs([HOME_TAB, ...serverTabs]);
        lastSavedTabsRef.current = JSON.stringify(restoredTabs);
        lastSavedUserKeyRef.current = tabsStorageKey;
        setTabs(restoredTabs);
        setLoadedTabsStorageKey(tabsStorageKey);
      })
      .catch(() => {
        if (loadGenerationRef.current !== generation) return;
        // On GET failure show Home only; do not write this fallback to server.
        const homeTabs = [HOME_TAB];
        lastSavedTabsRef.current = JSON.stringify(homeTabs);
        lastSavedUserKeyRef.current = tabsStorageKey;
        setTabs(homeTabs);
        setLoadedTabsStorageKey(tabsStorageKey);
        navigateRef.current("/", { replace: true });
      });

    return () => {
      if (loadGenerationRef.current === generation) {
        loadGenerationRef.current += 1;
      }
    };
  }, [isAuthenticated, tabsStorageKey, userId, roleId]);

  // ==========================================================
  // CREATE / UPDATE TAB FOR CURRENT ROUTE
  // ==========================================================

  useEffect(() => {
    /*
     * Preserve the current/stored application tabs while logged out.
     *
     * TabsContext is not rendered as the authenticated application UI on
     * the Login screen, so there is no need to reset the in-memory tabs.
     * Keeping them here allows the same tabs to reappear immediately after
     * the next successful Login.
     */
    if (
      !isAuthenticated ||
      !tabsStorageKey ||
      loadedTabsStorageKey !== tabsStorageKey
    ) {
      return;
    }

    const nextTab = resolveRouteTab(location.pathname, menuItems);

    /*
     * Routes which do not represent a tab are ignored.
     */
    if (!nextTab || nextTab.path === "/error") {
      return;
    }

    /*
     * IMPORTANT:
     *
     * This state updater remains completely PURE.
     *
     * Do not call:
     *
     * navigate()
     * setAnotherState()
     * ToastService()
     *
     * inside this callback.
     */
    setTabs((current) => {
      const existing = current.find((tab) => tab.path === nextTab.path);

      // ------------------------------------------------------
      // ADD NEW TAB
      // ------------------------------------------------------

      if (!existing) {
        return [...current, nextTab];
      }

      // ------------------------------------------------------
      // TAB ALREADY CURRENT
      // ------------------------------------------------------

      if (
        existing.label === nextTab.label &&
        existing.svgIcon === nextTab.svgIcon &&
        existing.tone === nextTab.tone
      ) {
        return current;
      }

      // ------------------------------------------------------
      // UPDATE TAB METADATA
      // ------------------------------------------------------

      return current.map((tab) =>
        tab.path === nextTab.path
          ? {
              ...tab,
              ...nextTab,
            }
          : tab,
      );
    });
  }, [
    location.pathname,
    isAuthenticated,
    menuItems,
    tabsStorageKey,
    loadedTabsStorageKey,
  ]);

  // ==========================================================
  // SAVE TABS SILENTLY (ONLY AFTER LOAD AND WHEN CHANGED)
  // ==========================================================

  useEffect(() => {
    if (
      !isAuthenticated ||
      !tabsStorageKey ||
      loadedTabsStorageKey !== tabsStorageKey ||
      lastSavedUserKeyRef.current !== tabsStorageKey
    )
      return;

    const serialized = JSON.stringify(tabs);
    if (serialized === lastSavedTabsRef.current) return;

    // Persist only in the current user's browser when server storage is off.
    // No API request or toast is generated in this mode.
    if (!APP_CONFIG.isEnableServerSideUserTabs) {
      try {
        localStorage.setItem(getLocalTabsStorageKey(userId, roleId), serialized);
        lastSavedTabsRef.current = serialized;
      } catch {
        // Storage can be unavailable or full; tab UI continues normally.
      }
      return;
    }

    const timeout = window.setTimeout(() => {
      // The save is associated with the authenticated user that created it.
      // Never enqueue a stale user's state after an account switch.
      if (activeUserKeyRef.current !== tabsStorageKey) return;
      if (serialized === lastSavedTabsRef.current) return;

      // Queue all writes in order. A slow older POST cannot overwrite a
      // newer tab list, and the saved signature changes only on success.
      saveQueueRef.current = saveQueueRef.current
        .catch(() => {})
        .then(async () => {
          const saved = await saveAndUpdateUserTabs({ userId, roleId, tabs });
          if (saved && activeUserKeyRef.current === tabsStorageKey) {
            lastSavedTabsRef.current = serialized;
            lastSavedUserKeyRef.current = tabsStorageKey;
          }
        })
        .catch(() => {});
    }, 450);

    return () => window.clearTimeout(timeout);
  }, [
    tabs,
    isAuthenticated,
    tabsStorageKey,
    loadedTabsStorageKey,
    userId,
    roleId,
  ]);

  // ==========================================================
  // CLOSE TAB
  // ==========================================================

  const closeTabImmediately = useCallback(
    (path) => {
      const index = tabs.findIndex((tab) => tab.path === path);

      if (index < 0) {
        return;
      }

      const remainingTabs = tabs.filter((tab) => tab.path !== path);
      const nextTabs = remainingTabs.length ? remainingTabs : [HOME_TAB];

      const isActivePath =
        path === ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES
          ? location.pathname === ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES ||
            location.pathname.startsWith(
              `${ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES}/`,
            )
          : path === ROUTE_CONSTANTS.ADMINISTRATION.USERS
            ? location.pathname === ROUTE_CONSTANTS.ADMINISTRATION.USERS ||
              location.pathname.startsWith(
                `${ROUTE_CONSTANTS.ADMINISTRATION.USERS}/`,
              )
            : path === ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION
              ? location.pathname ===
                  ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION ||
                location.pathname.startsWith(
                  `${ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION}/`,
                )
              : path === ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS
                ? location.pathname ===
                    ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS ||
                  location.pathname.startsWith(
                    `${ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS}/`,
                  )
                : path ===
                    ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE
                  ? location.pathname ===
                      ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE ||
                    location.pathname.startsWith(
                      `${ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE}/`,
                    )
                  : path === ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPES
                    ? location.pathname ===
                        ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPES ||
                      location.pathname.startsWith(
                        `${ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPES}/`,
                      )
                    : path === ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECTS
                      ? location.pathname ===
                          ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECTS ||
                        location.pathname.startsWith(
                          `${ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECTS}/`,
                        )
                      : path ===
                          ROUTE_CONSTANTS.MANAGE_PROJECTS
                            .EMPLOYEE_PROJECT_MAPPING
                        ? location.pathname ===
                            ROUTE_CONSTANTS.MANAGE_PROJECTS
                              .EMPLOYEE_PROJECT_MAPPING ||
                          location.pathname.startsWith(
                            `${ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING}/`,
                          )
                        : path ===
                            ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET
                          ? location.pathname ===
                              ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET ||
                            location.pathname.startsWith(
                              `${ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET}/`,
                            )
                          : location.pathname === path;

      const fallback = remainingTabs[Math.max(0, index - 1)] || HOME_TAB;

      setTabs(nextTabs);
      setDirtyTabs((current) => {
        if (!current[path]) return current;
        const next = { ...current };
        delete next[path];
        return next;
      });

      if (isActivePath) {
        navigate(fallback.path);
      }
    },
    [tabs, location.pathname, navigate],
  );

  const registerTabDirtyState = useCallback((path, isDirty) => {
    const normalizedPath = String(path || "").trim();
    if (!normalizedPath) return;

    setDirtyTabs((current) => {
      if (Boolean(current[normalizedPath]) === Boolean(isDirty)) {
        return current;
      }

      if (!isDirty) {
        if (!current[normalizedPath]) return current;
        const next = { ...current };
        delete next[normalizedPath];
        return next;
      }

      return {
        ...current,
        [normalizedPath]: true,
      };
    });
  }, []);

  const closeTab = useCallback(
    (path) => {
      if (dirtyTabs[path]) {
        setPendingClosePath(path);
        return;
      }

      closeTabImmediately(path);
    },
    [closeTabImmediately, dirtyTabs],
  );

  const stayOnDirtyTab = useCallback(() => {
    setPendingClosePath("");
  }, []);

  const discardDirtyTab = useCallback(() => {
    if (!pendingClosePath) return;

    const path = pendingClosePath;
    setPendingClosePath("");
    setDirtyTabs((current) => {
      const next = { ...current };
      delete next[path];
      return next;
    });
    closeTabImmediately(path);
  }, [closeTabImmediately, pendingClosePath]);

  // ==========================================================
  // RESET TABS
  // ==========================================================

  const resetTabs = useCallback(() => {
    // Clearing tabs is a normal change and is persisted by the save effect.
    setTabs([HOME_TAB]);
  }, [tabsStorageKey]);

  // ==========================================================
  // CLOSE ALL TABS
  // ==========================================================

  const closeAllTabs = useCallback(() => {
    /*
     * Reset tab state first.
     */
    resetTabs();

    /*
     * Navigation is intentionally outside the setTabs()
     * state updater.
     */
    navigate("/");
  }, [navigate, resetTabs]);

  // ==========================================================
  // CONTEXT VALUE
  // ==========================================================

  const value = useMemo(
    () => ({
      tabs: visibleTabs,

      closeTab,

      closeAllTabs,

      resetTabs,

      registerTabDirtyState,
    }),
    [visibleTabs, closeTab, closeAllTabs, resetTabs, registerTabDirtyState],
  );

  return (
    <TabsContext.Provider value={value}>
      {children}

      <DirtyStateModal
        open={Boolean(pendingClosePath)}
        onStay={stayOnDirtyTab}
        onDiscard={discardDirtyTab}
      />
    </TabsContext.Provider>
  );
}

// ============================================================
// USE TABS
// ============================================================

export const useTabs = () => useContext(TabsContext);
