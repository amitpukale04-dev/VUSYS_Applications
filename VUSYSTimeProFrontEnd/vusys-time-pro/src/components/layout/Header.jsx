import { useEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { useTheme } from "../../context/ThemeContext";
import { HOME_MENU_ITEM, flattenMenuItems } from "../../utils/menuUtils";
import ColorIcon from "../common/ColorIcon";
import SvgIcon from "../common/SvgIcon";
import ConfirmModal from "../common/ConfirmModal";
import ToastService from "../../services/toastService";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const normalize = (value) =>
  String(value || "")
    .trim()
    .toLowerCase();

export default function Header({ onSidebarToggle, onMobileMenu }) {
  const { user, logout, menuItems = [] } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();
  const searchRef = useRef(null);
  const searchContainerRef = useRef(null);
  const [query, setQuery] = useState("");
  const [searchOpen, setSearchOpen] = useState(false);
  const [mobileSearchOpen, setMobileSearchOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false);
  const [logoutLoading, setLogoutLoading] = useState(false);
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 60_000);
    return () => window.clearInterval(timer);
  }, []);

  const firstName = (user?.name || "User").trim().split(/\s+/)[0];
  const hour = now.getHours();
  const greeting =
    hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
  const formattedDate = new Intl.DateTimeFormat("en-GB", {
    weekday: "long",
    day: "2-digit",
    month: "long",
  })
    .format(now)
    .toUpperCase();

  const searchableRoutes = useMemo(
    () => flattenMenuItems([HOME_MENU_ITEM, ...menuItems]),
    [menuItems],
  );

  const results = useMemo(() => {
    const term = normalize(query);
    if (!term) return searchableRoutes.slice(0, 6);
    return searchableRoutes
      .filter((route) => {
        const haystack = [
          route.label,
          route.parentLabel,
          route.displayLabel,
          route.group,
          ...(route.keywords || []),
        ]
          .join(" ")
          .toLowerCase();
        return haystack.includes(term);
      })
      .slice(0, 12);
  }, [query, searchableRoutes]);

  useEffect(() => setActiveIndex(0), [query]);

  useEffect(() => {
    const handleOutsideClick = (event) => {
      if (!searchOpen) return;
      if (mobileSearchOpen) {
        const mobileOverlay = document.querySelector(
          ".tp-mobile-search-overlay",
        );
        if (mobileOverlay && !mobileOverlay.contains(event.target)) {
          setSearchOpen(false);
          setMobileSearchOpen(false);
        }
        return;
      }
      if (
        searchContainerRef.current &&
        !searchContainerRef.current.contains(event.target)
      ) {
        setSearchOpen(false);
      }
    };

    document.addEventListener("mousedown", handleOutsideClick);
    document.addEventListener("touchstart", handleOutsideClick);
    return () => {
      document.removeEventListener("mousedown", handleOutsideClick);
      document.removeEventListener("touchstart", handleOutsideClick);
    };
  }, [searchOpen, mobileSearchOpen]);

  useEffect(() => {
    const onShortcut = (event) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setMobileSearchOpen(window.innerWidth < 768);
        setSearchOpen(true);
        requestAnimationFrame(() => searchRef.current?.focus());
      }
      if (event.key === "Escape") {
        setSearchOpen(false);
        setMobileSearchOpen(false);
      }
    };
    window.addEventListener("keydown", onShortcut);
    return () => window.removeEventListener("keydown", onShortcut);
  }, []);

  const openRoute = (path) => {
    setQuery("");
    setSearchOpen(false);
    setMobileSearchOpen(false);
    navigate(path);
  };

  const onSearchKeyDown = (event) => {
    if (!results.length) return;
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActiveIndex((index) => (index + 1) % results.length);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex((index) => (index - 1 + results.length) % results.length);
    } else if (event.key === "Enter") {
      event.preventDefault();
      openRoute(results[activeIndex].path);
    }
  };

  const handleLogout = async () => {
    if (logoutLoading) {
      return;
    }

    setLogoutLoading(true);

    try {
      await logout();

      setShowLogoutConfirm(false);

      navigate(ROUTE_CONSTANTS.AUTH.LOGIN, {
        replace: true,
      });
    } catch (error) {
      if (!error?.isApiValidationError && !error?.__globalToastHandled) {
        ToastService.error(error?.message || "Unable to logout.");
      }
    } finally {
      setLogoutLoading(false);
    }
  };

  const initials = (user?.name || "TimePro User")
    .split(" ")
    .map((part) => part[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
  const profileImage =
    user?.profileImage ||
    (String(user?.role).toLowerCase() === "employee"
      ? "/images/profile-employee.svg"
      : "/images/profile-admin.svg");

  return (
    <header className="tp-header">
      <div className="d-flex align-items-center gap-2 flex-grow-1 min-width-0">
        <button
          type="button"
          className="tp-icon-button d-lg-none"
          onClick={onMobileMenu}
          aria-label="Open menu"
        >
          <i className="bi bi-list" />
        </button>
        <button
          type="button"
          className="tp-icon-button d-none d-lg-inline-flex"
          onClick={onSidebarToggle}
          aria-label="Toggle sidebar"
        >
          <i className="bi bi-layout-sidebar-inset" />
        </button>

        <div
          ref={searchContainerRef}
          className={`tp-global-search d-none d-md-flex ${searchOpen ? "open" : ""}`}
        >
          <i className="bi bi-search" />
          <input
            ref={searchRef}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onFocus={() => setSearchOpen(true)}
            onKeyDown={onSearchKeyDown}
            placeholder="Search menus..."
            aria-label="Search menus"
          />
          {query && (
            <button
              type="button"
              className="tp-search-clear"
              onClick={() => setQuery("")}
              aria-label="Clear search"
            >
              <i className="bi bi-x" />
            </button>
          )}
          <kbd>Ctrl K</kbd>
          {searchOpen && (
            <div className="tp-search-results">
              <div className="tp-search-results-title">
                {query ? "Search results" : "Quick navigation"}
              </div>
              {results.length ? (
                results.map((item, index) => (
                  <button
                    type="button"
                    key={item.path}
                    className={`tp-search-result ${index === activeIndex ? "active" : ""}`}
                    onMouseEnter={() => setActiveIndex(index)}
                    onClick={() => openRoute(item.path)}
                  >
                    <span
                      className={`tp-search-svg tp-sidebar-svg-${item.tone || "indigo"}`}
                    >
                      <SvgIcon name={item.svgIcon} size={17} />
                    </span>
                    <span>
                      <strong>{item.displayLabel || item.label}</strong>
                      <small>{item.group}</small>
                    </span>
                    <i className="bi bi-arrow-return-left" />
                  </button>
                ))
              ) : (
                <div className="tp-search-empty">
                  <i className="bi bi-search" /> No matching menu found
                </div>
              )}
            </div>
          )}
        </div>

        <div
          className="tp-header-greeting"
          aria-label={`${formattedDate}. ${greeting}, ${firstName}`}
        >
          <span className="tp-header-greeting-date">{formattedDate}</span>
          <strong className="tp-header-greeting-text">
            <span className="tp-header-greeting-label">
              {greeting}, {firstName}
            </span>
            <span className="tp-header-greeting-wave" aria-hidden="true">
              👋
            </span>
          </strong>
        </div>
      </div>

      <div className="d-flex align-items-center gap-1 gap-sm-2 tp-header-actions">
        <button
          type="button"
          className="tp-icon-button d-md-none"
          onClick={() => {
            setMobileSearchOpen(true);
            setSearchOpen(true);
            requestAnimationFrame(() => searchRef.current?.focus());
          }}
          aria-label="Search menu"
        >
          <i className="bi bi-search" />
        </button>
        <button
          type="button"
          className="tp-icon-button position-relative"
          aria-label="Notifications"
        >
          <i className="bi bi-bell-fill" />
          <span className="tp-notification-dot" />
        </button>
        <button
          type="button"
          className="tp-icon-button tp-theme-toggle"
          onClick={toggleTheme}
          aria-label={`Switch to ${theme === "light" ? "dark" : "light"} mode`}
          title={`Switch to ${theme === "light" ? "dark" : "light"} mode`}
        >
          <i
            className={`bi ${theme === "light" ? "bi-moon-stars-fill" : "bi-sun-fill"}`}
          />
        </button>
        <div className="dropdown">
          <button
            type="button"
            className="tp-user-button dropdown-toggle"
            data-bs-toggle="dropdown"
            aria-expanded="false"
          >
            <span className="tp-avatar">
              {profileImage ? (
                <img
                  src={profileImage}
                  alt={`${user?.name || "User"} profile`}
                />
              ) : (
                initials
              )}
            </span>
            <span className="d-none d-md-block text-start">
              <strong>{user?.name || ""}</strong>
              <small>{user?.role || ""}</small>
            </span>
          </button>
          <ul className="dropdown-menu dropdown-menu-end tp-dropdown-menu">
            <li className="tp-dropdown-account border-bottom">
              <small>Signed in as</small>
              <div className="fw-semibold text-truncate">{user?.email}</div>
              <hr className="dropdown-divider" />
              <div className="fw-semibold text-truncate tp-last-logged">
                Last Logged at: {user?.LastLogoutDate}
              </div>
            </li>
            <li>
              <button
                type="button"
                className="dropdown-item"
                onClick={() => navigate("/user-profile")}
              >
                <ColorIcon icon="bi-person-badge-fill" tone="cyan" size="sm" />
                User Profile
              </button>
            </li>
            <li>
              <button
                type="button"
                className="dropdown-item"
                onClick={() => navigate("/change-password")}
              >
                <ColorIcon icon="bi-shield-lock-fill" tone="violet" size="sm" />
                Change Password
              </button>
            </li>
            <li>
              <hr className="dropdown-divider" />
            </li>
            <li>
              <button
                type="button"
                className="dropdown-item text-danger"
                onClick={() => setShowLogoutConfirm(true)}
              >
                <span className="tp-search-svg tp-sidebar-svg-rose">
                  <SvgIcon name="logout" size={17} />
                </span>
                Logout
              </button>
            </li>
          </ul>
        </div>
      </div>

      {mobileSearchOpen &&
        createPortal(
          <div className="tp-mobile-search-overlay d-md-none">
            <div className="tp-mobile-search-head">
              <i className="bi bi-search" />
              <input
                ref={searchRef}
                autoFocus
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                onKeyDown={onSearchKeyDown}
                placeholder="Search menus..."
              />
              <button
                type="button"
                onClick={() => {
                  setMobileSearchOpen(false);
                  setSearchOpen(false);
                }}
              >
                <i className="bi bi-x-lg" />
              </button>
            </div>
            <div className="tp-mobile-search-results">
              {results.length ? (
                results.map((item, index) => (
                  <button
                    type="button"
                    key={item.path}
                    className={index === activeIndex ? "active" : ""}
                    onClick={() => openRoute(item.path)}
                  >
                    <span
                      className={`tp-search-svg tp-sidebar-svg-${item.tone || "indigo"}`}
                    >
                      <SvgIcon name={item.svgIcon} size={18} />
                    </span>
                    <span>
                      <strong>{item.displayLabel || item.label}</strong>
                      <small>{item.group}</small>
                    </span>
                    <i className="bi bi-chevron-right" />
                  </button>
                ))
              ) : (
                <div className="tp-search-empty">No matching menu found</div>
              )}
            </div>
          </div>,
          document.body,
        )}
      <ConfirmModal
        show={showLogoutConfirm}
        title="Confirm Logout"
        message="Are you sure you want to logout from TimePro?"
        icon="logout"
        confirmText="Yes, Logout"
        onConfirm={handleLogout}
        onCancel={() => {
          if (!logoutLoading) {
            setShowLogoutConfirm(false);
          }
        }}
        loading={logoutLoading}
      />
    </header>
  );
}
