import { getMenuIconPresentation } from "../../utils/menuIconPresentation";
import { useEffect, useState } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { HOME_MENU_ITEM } from "../../utils/menuUtils";
import SvgIcon from "../common/SvgIcon";

export default function Sidebar({ collapsed, mobileOpen, onMobileClose }) {
  const location = useLocation();
  const { menuItems = [], menuAccessLoading } = useAuth();
  const [openMenus, setOpenMenus] = useState({});
  const visibleRoutes = [HOME_MENU_ITEM, ...menuItems];
  const groups = [...new Set(visibleRoutes.map((route) => route.group))];

  useEffect(() => {
    const parent = visibleRoutes.find((route) =>
      route.children?.some((child) => child.path === location.pathname),
    );
    if (parent)
      setOpenMenus((current) => ({ ...current, [parent.label]: true }));
  }, [location.pathname]);

  const toggleMenu = (label) => {
    if (collapsed) return;
    setOpenMenus((current) => ({ ...current, [label]: !current[label] }));
  };

  const icon = (route, size = 20) => (
    <span
      className={`tp-sidebar-svg tp-sidebar-svg-${getMenuIconPresentation(route).tone}`}
    >
      <SvgIcon name={getMenuIconPresentation(route).svgIcon} size={size} />
    </span>
  );

  return (
    <>
      <aside className={`tp-sidebar ${mobileOpen ? "mobile-open" : ""}`}>
        <div className="tp-sidebar-brand">
          <div className="tp-brand-mark">
            <img src="/images/timepro-brand.png" alt="VUT Time Pro" />
          </div>
          {!collapsed && (
            <div>
              <strong>TimePro</strong>
              <small>Timesheet Management</small>
            </div>
          )}
          <button
            className="tp-icon-button d-lg-none ms-auto text-white"
            onClick={onMobileClose}
            aria-label="Close menu"
          >
            <i className="bi bi-x-lg" />
          </button>
        </div>

        <nav className="tp-sidebar-nav">
          {menuAccessLoading && !collapsed && (
            <div className="tp-nav-label">Loading menu...</div>
          )}
          {groups.map((group) => (
            <div key={group} className="tp-nav-group">
              {!collapsed && <div className="tp-nav-label">{group}</div>}
              {visibleRoutes
                .filter((route) => route.group === group)
                .map((route) => {
                  if (route.children?.length) {
                    const active = route.children.some(
                      (child) => child.path === location.pathname,
                    );
                    const opened = Boolean(openMenus[route.label]);
                    return (
                      <div
                        className={`tp-nav-parent ${active ? "active" : ""}`}
                        key={route.label}
                      >
                        <button
                          type="button"
                          className={`tp-nav-link tp-nav-parent-button ${active ? "active" : ""}`}
                          onClick={() => toggleMenu(route.label)}
                          title={collapsed ? route.label : undefined}
                        >
                          {icon(route)}
                          {!collapsed && (
                            <>
                              <span className="tp-nav-text">{route.label}</span>
                              <span
                                className={`tp-nav-chevron ${opened ? "open" : ""}`}
                              >
                                <SvgIcon name="chevron" size={16} />
                              </span>
                            </>
                          )}
                        </button>
                        {!collapsed && (
                          <div className={`tp-submenu ${opened ? "open" : ""}`}>
                            {route.children.map((child) => (
                              <NavLink
                                key={child.path}
                                to={child.path}
                                onClick={onMobileClose}
                                className={({ isActive }) =>
                                  `tp-submenu-link ${isActive ? "active" : ""}`
                                }
                              >
                                {icon(child, 17)}
                                <span>{child.label}</span>
                                {child.badge && (
                                  <span className="tp-nav-badge">
                                    {child.badge}
                                  </span>
                                )}
                              </NavLink>
                            ))}
                          </div>
                        )}
                      </div>
                    );
                  }

                  return (
                    <NavLink
                      key={route.path}
                      to={route.path}
                      end={route.path === "/"}
                      onClick={onMobileClose}
                      className={({ isActive }) =>
                        `tp-nav-link ${isActive ? "active" : ""}`
                      }
                      title={collapsed ? route.label : undefined}
                    >
                      {icon(route)}
                      {!collapsed && (
                        <span className="tp-nav-text">{route.label}</span>
                      )}
                      {!collapsed && route.badge && (
                        <span className="tp-nav-badge">{route.badge}</span>
                      )}
                    </NavLink>
                  );
                })}
            </div>
          ))}
        </nav>
      </aside>
      {mobileOpen && (
        <button
          className="tp-sidebar-backdrop d-lg-none"
          onClick={onMobileClose}
          aria-label="Close menu"
        />
      )}
    </>
  );
}
