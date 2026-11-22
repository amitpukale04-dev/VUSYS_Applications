import { normalizeNumber, normalizeString } from "./valueUtils";

// ============================================================
// DYNAMIC USER SECURITY PERMISSION HELPERS
// ============================================================

const normalizePermissionText = (value) =>
  normalizeString(value).trim().toLowerCase();

/**
 * Normalizes USER_SECURITY_ACCESS_SELECTED_LIST rows returned by
 * /api/Master/GetUserSecurityAccessList.
 */
export const normalizeUserSecurityAccessItems = (rows = []) =>
  (Array.isArray(rows) ? rows : [])
    .map((row) => ({
      securityId: normalizeNumber(
        row?.SecurityID ??
          row?.SecurityId ??
          row?.securityID ??
          row?.securityId,
      ),
      userId: normalizeNumber(
        row?.UserID ?? row?.UserId ?? row?.userID ?? row?.userId,
      ),
      securityName: normalizeString(row?.SecurityName ?? row?.securityName),
      screenName: normalizeString(row?.ScreenName ?? row?.screenName),
    }))
    .filter(
      (item) => item.securityId > 0 && item.securityName && item.screenName,
    );

export const getUserSecurityAccessIds = (items = []) =>
  normalizeUserSecurityAccessItems(items).map((item) => item.securityId);

/**
 * Finds whether the selected-access API contains the requested permission.
 *
 * screenName comes directly from Security Master, e.g. "Employee Master".
 * action is semantic (Create/Edit/View/Open) rather than a hardcoded ID.
 * The backend remains the source of truth for the actual SecurityID value.
 */
export const hasUserSecurityPermission = (
  screenName,
  action,
  userSecurityAccessItems = [],
) => {
  const requiredScreen = normalizePermissionText(screenName);
  const requiredAction = normalizePermissionText(action);

  if (!requiredScreen || !requiredAction) {
    return false;
  }

  return normalizeUserSecurityAccessItems(userSecurityAccessItems).some(
    (item) => {
      if (normalizePermissionText(item.screenName) !== requiredScreen) {
        return false;
      }

      const permissionName = normalizePermissionText(item.securityName);

      if (requiredAction === "open" || requiredAction === "open list") {
        return permissionName.startsWith("allow to open ");
      }

      return permissionName.startsWith(`allow to ${requiredAction} `);
    },
  );
};

/** Checks whether any requested semantic permission is available. */
export const hasAnyUserSecurityPermission = (
  permissions = [],
  userSecurityAccessItems = [],
) =>
  (Array.isArray(permissions) ? permissions : []).some((permission) =>
    hasUserSecurityPermission(
      permission?.screenName,
      permission?.action,
      userSecurityAccessItems,
    ),
  );

/** Checks whether every requested semantic permission is available. */
export const hasAllUserSecurityPermissions = (
  permissions = [],
  userSecurityAccessItems = [],
) => {
  const required = Array.isArray(permissions) ? permissions : [];
  return (
    required.length > 0 &&
    required.every((permission) =>
      hasUserSecurityPermission(
        permission?.screenName,
        permission?.action,
        userSecurityAccessItems,
      ),
    )
  );
};
