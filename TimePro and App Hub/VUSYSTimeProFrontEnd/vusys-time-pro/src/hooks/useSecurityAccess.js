import { useMemo } from "react";
import { useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import {
  getActionSecurityAccessIds,
  hasActionSecurityAccess,
} from "../utils/menuUtils";

/**
 * Action-level security helper for Create/Edit/View/etc. controls.
 *
 * IMPORTANT:
 * Action access now comes from GetUserMenuAccess with paramType="Action".
 * Sidebar/Search/ProtectedRoute continue to use only paramType="Menu" data.
 */
export default function useSecurityAccess(pathnameOverride = "") {
  const location = useLocation();
  const { actionAccessItems = [] } = useAuth();
  const pathname = pathnameOverride || location.pathname;

  const actionSecurityAccessIds = useMemo(
    () => getActionSecurityAccessIds(pathname, actionAccessItems),
    [pathname, actionAccessItems],
  );

  const hasActionAccess = (securityAccessId) =>
    hasActionSecurityAccess(pathname, securityAccessId, actionAccessItems);

  return {
    actionSecurityAccessIds,
    hasActionAccess,
  };
}
