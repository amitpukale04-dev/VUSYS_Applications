import { useEffect, useRef, useState } from "react";
import { Outlet, useLocation } from "react-router-dom";
import Header from "./Header";
import Sidebar from "./Sidebar";
import Footer from "./Footer";
import RouteTabs from "./RouteTabs";
import { STORAGE_KEYS } from "../../utils/storageKeys";
import ScrollToTop from "../common/ScrollToTop";
import { useAuth } from "../../context/AuthContext";
import { APP_CONFIG } from "../../utils/environmentConfig";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

export default function AppLayout() {
  const pageContentRef = useRef(null);
  const [collapsed, setCollapsed] = useState(
    () => localStorage.getItem(STORAGE_KEYS.sidebarCollapsed) === "true",
  );
  const [mobileOpen, setMobileOpen] = useState(false);
  const location = useLocation();
  const { isAuthenticated } = useAuth();
  const isWidePage =
    location.pathname === ROUTE_CONSTANTS.ADMINISTRATION.SECURITY_MASTER ||
    location.pathname === ROUTE_CONSTANTS.ADMINISTRATION.SECURITY_ACCESS ||
    location.pathname.startsWith(ROUTE_CONSTANTS.ADMINISTRATION.USERS) ||
    location.pathname === ROUTE_CONSTANTS.ADMINISTRATION.ROLE_MENU_ACCESS ||
    location.pathname === ROUTE_CONSTANTS.AUTH.CHANGE_PASSWORD ||
    location.pathname === ROUTE_CONSTANTS.AUTH.USER_PROFILE ||
    location.pathname === ROUTE_CONSTANTS.ADMINISTRATION.ROLE_MASTER ||
    location.pathname === ROUTE_CONSTANTS.MASTER.GENDER_MASTER ||
    location.pathname === ROUTE_CONSTANTS.MASTER.COUNTRY_MASTER ||
    location.pathname === ROUTE_CONSTANTS.MASTER.STATE_MASTER ||
    location.pathname === ROUTE_CONSTANTS.MASTER.HOLIDAY_MASTER ||
    location.pathname.startsWith(
      ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION,
    ) ||
    location.pathname.startsWith(ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEES) ||
    location.pathname === ROUTE_CONSTANTS.MANAGE_EMPLOYEES.EMPLOYEE_TYPE ||
    location.pathname.startsWith(ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECTS) ||
    location.pathname.startsWith(
      ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING,
    ) ||
    location.pathname === ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECT_ACTIVITIES ||
    location.pathname.startsWith(
      ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET,
    ) ||
    location.pathname === ROUTE_CONSTANTS.MANAGE_TIMESHEET.DOWNLOAD_TIMESHEET ||
    location.pathname ===
      ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_SUBMISSION ||
    location.pathname ===
      ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_APPROVAL_REJECTION_LIST ||
    location.pathname ===
      ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_APPROVAL_REJECTION ||
    location.pathname.startsWith(
      ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPES,
    ) ||
    location.pathname.startsWith(
      ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS,
    ) ||
    location.pathname.startsWith(
      ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_APPROVAL_REJECTION_LIST,
    ) ||
    location.pathname.startsWith(
      ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_APPROVAL_REJECTION,
    ) ||
    location.pathname.startsWith(
      ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE,
    ) ||
    location.pathname === ROUTE_CONSTANTS.TRANSACTIONS.TRANSACTION_HISTORY ||
    location.pathname === ROUTE_CONSTANTS.REPORTS.TIMESHEET_REPORT ||
    location.pathname === ROUTE_CONSTANTS.REPORTS.ALLOCATED_CONSUMED_REPORT ||
    location.pathname === ROUTE_CONSTANTS.REPORTS.LEAVE_REPORT;

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    const currentVersion = APP_CONFIG.versionNo;
    const storedVersion = localStorage.getItem(STORAGE_KEYS.appVersion);

    if (!storedVersion) {
      localStorage.setItem(STORAGE_KEYS.appVersion, currentVersion);
      return;
    }

    if (storedVersion !== currentVersion) {
      // Store the new version before reloading to ensure this happens only once.
      localStorage.setItem(STORAGE_KEYS.appVersion, currentVersion);
      window.location.reload();
    }
  }, [isAuthenticated]);

  const toggleCollapsed = () => {
    setCollapsed((value) => {
      localStorage.setItem(STORAGE_KEYS.sidebarCollapsed, String(!value));
      return !value;
    });
  };

  return (
    <div className={`tp-app-shell ${collapsed ? "sidebar-collapsed" : ""}`}>
      <Sidebar
        collapsed={collapsed}
        mobileOpen={mobileOpen}
        onMobileClose={() => setMobileOpen(false)}
      />
      <div className="tp-main-shell">
        <Header
          onSidebarToggle={toggleCollapsed}
          onMobileMenu={() => setMobileOpen(true)}
        />
        <RouteTabs />
        <main
          ref={pageContentRef}
          className={`tp-page-content ${isWidePage ? "tp-page-content-wide" : ""}`}
        >
          <Outlet />
        </main>
        <Footer />
        <ScrollToTop
          scrollContainerRef={pageContentRef}
          visibilityThreshold={260}
        />
      </div>
    </div>
  );
}
