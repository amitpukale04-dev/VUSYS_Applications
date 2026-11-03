import { Navigate, Outlet, matchPath, useLocation } from "react-router-dom";
import AccessDenied from "../components/common/AccessDenied";
import NotFound from "../pages/errors/NotFound";
import { useAuth } from "../context/AuthContext";
import { hasPathAccess } from "../utils/menuUtils";
import { ROUTE_CONSTANTS } from "../constants/RouteConstants";

const PROTECTED_ROUTE_PATTERNS = Object.freeze([
  ROUTE_CONSTANTS.AUTH.USER_PROFILE,
  ROUTE_CONSTANTS.AUTH.CHANGE_PASSWORD,
  ROUTE_CONSTANTS.COMMON.ERROR,
  ROUTE_CONSTANTS.COMMON.HOME,
  ROUTE_CONSTANTS.ADMINISTRATION.SECURITY_MASTER,
  ROUTE_CONSTANTS.ADMINISTRATION.SECURITY_ACCESS,
  ROUTE_CONSTANTS.ADMINISTRATION.USERS,
  ROUTE_CONSTANTS.ADMINISTRATION.USER_ADD,
  ROUTE_CONSTANTS.ADMINISTRATION.USER_EDIT,
  ROUTE_CONSTANTS.ADMINISTRATION.ROLE_MENU_ACCESS,
  ROUTE_CONSTANTS.ADMINISTRATION.ROLE_MASTER,
  ROUTE_CONSTANTS.MASTER.GENDER_MASTER,
  ROUTE_CONSTANTS.MASTER.COUNTRY_MASTER,
  ROUTE_CONSTANTS.MASTER.STATE_MASTER,
  ROUTE_CONSTANTS.MASTER.HOLIDAY_MASTER,
  ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION,
  ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION_ADD,
  ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION_EDIT,
  ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES,
  ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEE_ADD,
  ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEE_EDIT,
  ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEE_TYPE,
  ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECTS,
  ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECT_ADD,
  ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECT_EDIT,
  ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING,
  ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING_ADD,
  ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING_EDIT,
  ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECT_ACTIVITIES,
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET,
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET_ADD,
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET_EDIT,
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.DOWNLOAD_TIMESHEET,
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_SUBMISSION,
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_APPROVAL_REJECTION_LIST,
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPES,
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPE_ADD,
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPE_EDIT,
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS,
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.APPLY_LEAVE,
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.APPLY_LEAVE_EDIT,
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_APPROVAL_REJECTION_LIST,
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE,
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE_ADD,
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE_EDIT,
  ROUTE_CONSTANTS.REPORTS.TIMESHEET_REPORT,
  ROUTE_CONSTANTS.REPORTS.ALLOCATED_CONSUMED_REPORT,
  ROUTE_CONSTANTS.REPORTS.LEAVE_REPORT,
]);

const isKnownProtectedReactRoute = (pathname = "") =>
  PROTECTED_ROUTE_PATTERNS.some((pattern) =>
    Boolean(matchPath({ path: pattern, end: true }, pathname)),
  );

export default function ProtectedRoute({
  requireMenuAccess = true,
  allowedRoles = null,
}) {
  const {
    isAuthenticated,
    menuItems = [],
    menuAccessLoading,
    user,
  } = useAuth();
  const location = useLocation();

  if (!isAuthenticated)
    return (
      <Navigate
        to={ROUTE_CONSTANTS.AUTH.LOGIN}
        replace
        state={{ from: location }}
      />
    );

  /*
   * Some authenticated direct-URL screens have an additional role
   * restriction even though they are intentionally excluded from normal
   * menu-access checks.
   */
  if (Array.isArray(allowedRoles) && allowedRoles.length > 0) {
    const currentRole = String(
      user?.role ?? user?.UserRole ?? user?.userRole ?? "",
    )
      .trim()
      .toLowerCase();

    const hasAllowedRole = allowedRoles.some(
      (role) =>
        String(role || "")
          .trim()
          .toLowerCase() === currentRole,
    );

    if (!hasAllowedRole) {
      return (
        <AccessDenied
          message="You don't have permission to access this page."
          returnPath="/"
          returnLabel="Back to Home"
        />
      );
    }
  }

  /*
   * Direct authenticated URL screens, such as the Timesheet Approval/Rejection
   * email link, are not required to exist in the visible menu configuration.
   * All existing protected menu routes continue to use the default
   * requireMenuAccess=true behavior.
   */
  if (!requireMenuAccess) {
    return <Outlet />;
  }

  if (menuAccessLoading) return null;

  const hasMenuAccess = hasPathAccess(location.pathname, menuItems);

  if (!hasMenuAccess) {
    /*
     * Valid React route but no permission => 403.
     * Completely unknown URL => 404.
     *
     * Backend-controlled dynamic menu routes continue to work because
     * hasPathAccess() returns true when their RoutePath is present in the
     * authenticated user's menuItems.
     */
    if (!isKnownProtectedReactRoute(location.pathname)) {
      return <NotFound />;
    }

    return (
      <AccessDenied
        message="You don't have permission to access this page."
        returnPath="/"
        returnLabel="Back to Home"
      />
    );
  }

  return <Outlet />;
}
