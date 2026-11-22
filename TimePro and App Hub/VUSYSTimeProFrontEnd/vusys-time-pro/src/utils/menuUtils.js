import { STORAGE_KEYS } from "./storageKeys";
import {
  normalizeBoolean,
  normalizeNumber,
  normalizeNumberArray,
  normalizeString,
} from "./valueUtils";
import { getSecurityAccessIds } from "./userSession";

// ============================================================
// EVENTS
// ============================================================

const MENU_ACCESS_UPDATED_EVENT = "timepro:menu-access-updated";
const ACTION_ACCESS_UPDATED_EVENT = "timepro:action-access-updated";

// ============================================================
// HOME MENU ITEM
// ============================================================

export const HOME_MENU_ITEM = Object.freeze({
  key: "home",
  label: "Home",
  path: "/",
  group: "Workspace",
  svgIcon: "home",
  tone: "indigo",
  keywords: ["dashboard", "overview", "home"],
});

// ============================================================
// SAFE JSON PARSE
// ============================================================

const safeParse = (value, fallback = []) => {
  try {
    return value ? JSON.parse(value) : fallback;
  } catch {
    return fallback;
  }
};

// ============================================================
// SECURITY ACCESS INTERSECTION
// ============================================================

const intersects = (left = [], right = []) => {
  if (!left.length || !right.length) {
    return true;
  }

  const rightSet = new Set(right);

  return left.some((value) => rightSet.has(value));
};

// ============================================================
// NORMALIZE SEARCH KEYWORDS
// ============================================================

const normalizeSearchKeywords = (value) => {
  if (!Array.isArray(value)) {
    return [];
  }

  return [
    ...new Set(value.map((item) => normalizeString(item)).filter(Boolean)),
  ];
};

// ============================================================
// GET DYNAMIC SUB MENU METADATA
// ============================================================

/*
 * Route/presentation/search metadata now comes directly from
 * /api/Master/GetUserMenuAccess.
 *
 * There is intentionally no hard-coded SubMenuId registry in React.
 */
const getDynamicSubMenuMetadata = (subMenu = {}) => ({
  path: normalizeString(subMenu?.RoutePath ?? subMenu?.routePath),

  svgIcon: normalizeString(subMenu?.SvgIcon ?? subMenu?.svgIcon) || "home",

  tone: normalizeString(subMenu?.Tone ?? subMenu?.tone) || "indigo",

  keywords: normalizeSearchKeywords(
    subMenu?.SearchKeywords ??
      subMenu?.searchKeywords ??
      subMenu?.Keywords ??
      subMenu?.keywords,
  ),
});

// ============================================================
// GET DYNAMIC MAIN MENU PRESENTATION
// ============================================================

/*
 * The current API response does not expose MainMenu-level SvgIcon/Tone.
 * To keep the menu fully API-driven, the parent presentation is derived
 * from the first child that has presentation metadata.
 *
 * If MainMenu SvgIcon/Tone are added to the API later, this helper already
 * supports them without requiring a hard-coded React registry.
 */
const getDynamicMainMenuPresentation = (mainMenu = {}, children = []) => {
  const apiSvgIcon = normalizeString(mainMenu?.SvgIcon ?? mainMenu?.svgIcon);

  const apiTone = normalizeString(mainMenu?.Tone ?? mainMenu?.tone);

  const presentationChild = (Array.isArray(children) ? children : []).find(
    (child) => child?.svgIcon || child?.tone,
  );

  return {
    svgIcon: apiSvgIcon || presentationChild?.svgIcon || "home",
    tone: apiTone || presentationChild?.tone || "indigo",
  };
};

// ============================================================
// NORMALIZE ACCESS TREE
// ============================================================

const normalizeAccessTree = (
  rows = [],
  userSecurityAccessIds = [],
  accessType = "Menu",
) => {
  const normalizedUserSecurityIds = normalizeNumberArray(userSecurityAccessIds);

  const normalizedAccessType = String(accessType || "")
    .trim()
    .toLowerCase();

  const isMenuAccess = normalizedAccessType === "menu";

  const isActionAccess = normalizedAccessType === "action";

  const isRoleMenuAccess = normalizedAccessType === "rolemenuaccess";

  return (Array.isArray(rows) ? rows : [])
    .map((mainMenu) => {
      // ======================================================
      // MAIN MENU
      // ======================================================

      const mainMenuId = normalizeNumber(
        mainMenu?.MainMenuId ?? mainMenu?.mainMenuId,
      );

      const mainMenuName = normalizeString(
        mainMenu?.MainMenuName ?? mainMenu?.mainMenuName,
      );

      const rawSubMenus = mainMenu?.SubMenus ?? mainMenu?.subMenus;

      const subMenus = Array.isArray(rawSubMenus) ? rawSubMenus : [];

      // ======================================================
      // SUB MENUS
      // ======================================================

      const children = subMenus
        .map((subMenu) => {
          const securityAccessIds = normalizeNumberArray(
            subMenu?.SecurityAccessIds ?? subMenu?.securityAccessIds ?? [],
          );

          const actionSecurityAccessIds = normalizeNumberArray(
            subMenu?.ActionSecurityAccessIds ??
              subMenu?.actionSecurityAccessIds ??
              [],
          );

          const idsToAuthorize = isActionAccess
            ? actionSecurityAccessIds
            : securityAccessIds;

          /*
           * RoleMenuAccess returns all menus.
           *
           * IsSelected determines whether the selected Role
           * currently has access.
           *
           * Therefore this response must not be filtered using
           * the logged-in user's SecurityAccessIds.
           */
          if (
            !isRoleMenuAccess &&
            !intersects(idsToAuthorize, normalizedUserSecurityIds)
          ) {
            return null;
          }

          const subMenuId = normalizeNumber(
            subMenu?.SubMenuId ?? subMenu?.subMenuId,
          );

          const label = normalizeString(subMenu?.SubMenu ?? subMenu?.subMenu);

          const routeMetadata = getDynamicSubMenuMetadata(subMenu);

          /*
           * Sidebar / Global Search / ProtectedRoute need an actual
           * RoutePath from the API.
           *
           * Do not invent a slug when RoutePath is blank. This keeps the
           * frontend truly backend-driven and prevents navigation to routes
           * that the API has not configured.
           *
           * RoleMenuAccess must still show every menu, including menus whose
           * RoutePath has not been configured yet.
           */
          if (isMenuAccess && !routeMetadata.path) {
            return null;
          }

          const isSelected = isRoleMenuAccess
            ? normalizeBoolean(subMenu?.IsSelected ?? subMenu?.isSelected)
            : false;

          return {
            key: `submenu-${subMenuId}`,

            subMenuId,

            label,

            ...routeMetadata,

            securityAccessIds,

            actionSecurityAccessIds,

            isSelected,

            mainMenuId,

            mainMenuName,
          };
        })
        .filter(Boolean);

      if (!children.length) {
        return null;
      }

      const presentation = getDynamicMainMenuPresentation(mainMenu, children);

      return {
        key: `mainmenu-${mainMenuId}`,

        mainMenuId,

        label: mainMenuName,

        group: "Navigation",

        ...presentation,

        children,
      };
    })
    .filter(Boolean);
};

// ============================================================
// NORMALIZE USER MENU ACCESS
// ============================================================

/**
 * Used by:
 *
 * - Sidebar
 * - Global Search
 * - ProtectedRoute
 */
export const normalizeUserMenuAccess = (
  rows = [],
  userSecurityAccessIds = [],
) => normalizeAccessTree(rows, userSecurityAccessIds, "Menu");

// ============================================================
// NORMALIZE USER ACTION ACCESS
// ============================================================

/**
 * Used by:
 *
 * - Create
 * - Edit
 * - View
 * - Other action permissions
 */
export const normalizeUserActionAccess = (
  rows = [],
  userSecurityAccessIds = [],
) => normalizeAccessTree(rows, userSecurityAccessIds, "Action");

// ============================================================
// NORMALIZE ROLE MENU ACCESS
// ============================================================

/**
 * Used only by Role Menu Access TreeView.
 *
 * Preserves IsSelected.
 */
export const normalizeRoleMenuAccess = (rows = []) =>
  normalizeAccessTree(rows, [], "RoleMenuAccess");

// ============================================================
// SAVE USER MENU ACCESS
// ============================================================

export const saveUserMenuAccess = (items = []) => {
  const normalizedItems = Array.isArray(items) ? items : [];

  localStorage.setItem(
    STORAGE_KEYS.userMenuAccess,
    JSON.stringify(normalizedItems),
  );

  window.dispatchEvent(
    new CustomEvent(MENU_ACCESS_UPDATED_EVENT, {
      detail: {
        items: normalizedItems,
      },
    }),
  );

  return normalizedItems;
};

// ============================================================
// SAVE USER ACTION ACCESS
// ============================================================

export const saveUserActionAccess = (items = []) => {
  const normalizedItems = Array.isArray(items) ? items : [];

  localStorage.setItem(
    STORAGE_KEYS.userActionAccess,
    JSON.stringify(normalizedItems),
  );

  window.dispatchEvent(
    new CustomEvent(ACTION_ACCESS_UPDATED_EVENT, {
      detail: {
        items: normalizedItems,
      },
    }),
  );

  return normalizedItems;
};

// ============================================================
// GET STORED ACCESS
// ============================================================

export const getStoredUserMenuAccess = () =>
  safeParse(localStorage.getItem(STORAGE_KEYS.userMenuAccess), []);

export const getStoredUserActionAccess = () =>
  safeParse(localStorage.getItem(STORAGE_KEYS.userActionAccess), []);

// ============================================================
// HAS STORED ACCESS
// ============================================================

export const hasStoredUserMenuAccess = () => {
  try {
    return localStorage.getItem(STORAGE_KEYS.userMenuAccess) !== null;
  } catch {
    return false;
  }
};

export const hasStoredUserActionAccess = () => {
  try {
    return localStorage.getItem(STORAGE_KEYS.userActionAccess) !== null;
  } catch {
    return false;
  }
};

// ============================================================
// CLEAR USER MENU ACCESS
// ============================================================

export const clearUserMenuAccess = () => {
  try {
    localStorage.removeItem(STORAGE_KEYS.userMenuAccess);
  } catch {
    /*
     * Ignore storage cleanup errors during
     * logout/session reset.
     */
  }

  window.dispatchEvent(
    new CustomEvent(MENU_ACCESS_UPDATED_EVENT, {
      detail: {
        items: [],
      },
    }),
  );
};

// ============================================================
// CLEAR USER ACTION ACCESS
// ============================================================

export const clearUserActionAccess = () => {
  try {
    localStorage.removeItem(STORAGE_KEYS.userActionAccess);
  } catch {
    /*
     * Ignore storage cleanup errors during
     * logout/session reset.
     */
  }

  window.dispatchEvent(
    new CustomEvent(ACTION_ACCESS_UPDATED_EVENT, {
      detail: {
        items: [],
      },
    }),
  );
};

// ============================================================
// CLEAR ALL USER ACCESS
// ============================================================

export const clearUserAccess = () => {
  clearUserMenuAccess();

  clearUserActionAccess();
};

// ============================================================
// FLATTEN MENU ITEMS
// ============================================================

export const flattenMenuItems = (items = []) =>
  (Array.isArray(items) ? items : []).flatMap((item) => {
    if (!item.children?.length) {
      return item.path
        ? [
            {
              ...item,

              parentLabel: "",

              displayLabel: item.label,
            },
          ]
        : [];
    }

    return item.children.map((child) => ({
      ...child,

      group: item.group,

      parentKey: item.key,

      parentLabel: item.label,

      displayLabel: `${item.label} → ${child.label}`,
    }));
  });

// ============================================================
// NORMALIZE PROTECTED PATH
// ============================================================

/*
 * Resolve a nested application route back to the menu route supplied by
 * GetUserMenuAccess / GetUserActionAccess.
 *
 * Examples:
 *   /projects/new              -> /projects
 *   /projects/<encoded-id>     -> /projects
 *   /employees/<encoded-id>    -> /employees
 *   /users/new                 -> /users
 *
 * This deliberately avoids hard-coding module names. Any future screen that
 * follows the standard parent/list route + nested add/view/edit route pattern
 * automatically inherits the parent menu/action permissions.
 */
const normalizeProtectedPath = (pathname = "", items = []) => {
  const value = String(pathname || "").trim() || "/";

  if (value === "/") {
    return value;
  }

  const matchingRoute = flattenMenuItems(items)
    .map((item) => String(item?.path || "").trim())
    .filter(Boolean)
    .filter(
      (path) =>
        value === path ||
        (path !== "/" && value.startsWith(`${path}/`)),
    )
    .sort((left, right) => right.length - left.length)[0];

  return matchingRoute || value;
};

// ============================================================
// ALWAYS ALLOWED PATHS
// ============================================================

const ALWAYS_ALLOWED_PATHS = new Set([
  "/",
  "/user-profile",
  "/change-password",
  "/error",
]);

// ============================================================
// HAS PATH ACCESS
// ============================================================

export const hasPathAccess = (pathname, items = getStoredUserMenuAccess()) => {
  const normalizedPath = normalizeProtectedPath(pathname, items);
  if (ALWAYS_ALLOWED_PATHS.has(normalizedPath)) {
    return true;
  }

  return flattenMenuItems(items).some((item) => item.path === normalizedPath);
};

// ============================================================
// GET MENU ITEM BY PATH
// ============================================================

export const getMenuItemByPath = (
  pathname,
  items = getStoredUserMenuAccess(),
) => {
  const normalizedPath = normalizeProtectedPath(pathname, items);

  return (
    flattenMenuItems(items).find((item) => item.path === normalizedPath) || null
  );
};

// ============================================================
// GET ACTION ACCESS ITEM BY PATH
// ============================================================

export const getActionAccessItemByPath = (
  pathname,
  items = getStoredUserActionAccess(),
) => {
  const normalizedPath = normalizeProtectedPath(pathname, items);

  return (
    flattenMenuItems(items).find((item) => item.path === normalizedPath) || null
  );
};

// ============================================================
// GET ACTION ACCESS ITEM BY SUB MENU ID
// ============================================================

export const getActionAccessItemBySubMenuId = (
  subMenuId,
  items = getStoredUserActionAccess(),
) => {
  const normalizedSubMenuId = normalizeNumber(subMenuId);

  return (
    flattenMenuItems(items).find(
      (item) => normalizeNumber(item.subMenuId) === normalizedSubMenuId,
    ) || null
  );
};

// ============================================================
// GET ACTION SECURITY ACCESS IDS
// ============================================================

export const getActionSecurityAccessIds = (
  pathname,
  items = getStoredUserActionAccess(),
) =>
  normalizeNumberArray(
    getActionAccessItemByPath(pathname, items)?.actionSecurityAccessIds || [],
  );

// ============================================================
// HAS ACTION SECURITY ACCESS
// ============================================================

export const hasActionSecurityAccess = (
  pathname,
  securityAccessId,
  items = getStoredUserActionAccess(),
) => {
  const requiredId = normalizeNumber(securityAccessId);

  if (!requiredId) {
    return false;
  }

  const routeActionIds = getActionSecurityAccessIds(pathname, items);

  const userSecurityIds = getSecurityAccessIds();

  return (
    routeActionIds.includes(requiredId) &&
    (userSecurityIds.length === 0 || userSecurityIds.includes(requiredId))
  );
};
