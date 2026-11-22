import { useCallback, useMemo } from "react";
import { useAuth } from "../context/AuthContext";
import {
  hasAllUserSecurityPermissions,
  hasAnyUserSecurityPermission,
  hasUserSecurityPermission,
  normalizeUserSecurityAccessItems,
} from "../utils/securityPermissionUtils";

// ============================================================
// USER SECURITY PERMISSION HOOK
// ============================================================

/**
 * Reactive permission helper backed by USER_SECURITY_ACCESS_SELECTED_LIST.
 * No SecurityID is hardcoded in the frontend.
 */
export default function useUserSecurityPermission() {
  const { userSecurityAccessItems = [], userSecurityAccessLoading = false } =
    useAuth();

  const permissions = useMemo(
    () => normalizeUserSecurityAccessItems(userSecurityAccessItems),
    [userSecurityAccessItems],
  );

  const hasPermission = useCallback(
    (screenName, action) =>
      hasUserSecurityPermission(screenName, action, permissions),
    [permissions],
  );

  const hasAnyPermission = useCallback(
    (requiredPermissions) =>
      hasAnyUserSecurityPermission(requiredPermissions, permissions),
    [permissions],
  );

  const hasAllPermissions = useCallback(
    (requiredPermissions) =>
      hasAllUserSecurityPermissions(requiredPermissions, permissions),
    [permissions],
  );

  return {
    permissions,
    isLoading: userSecurityAccessLoading,
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
  };
}
