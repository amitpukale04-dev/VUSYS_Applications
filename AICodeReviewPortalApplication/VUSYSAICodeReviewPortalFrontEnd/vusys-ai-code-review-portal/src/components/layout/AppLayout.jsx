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
  const isWidePage = true;

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
