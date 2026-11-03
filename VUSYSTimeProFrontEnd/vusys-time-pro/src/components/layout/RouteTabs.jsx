import { useEffect, useRef } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useTabs } from "../../context/TabsContext";
import SvgIcon from "../common/SvgIcon";

export default function RouteTabs() {
  const { tabs, closeTab } = useTabs();
  const location = useLocation();
  const scrollRef = useRef(null);

  useEffect(() => {
    const activeTab = scrollRef.current?.querySelector(".tp-route-tab.active");
    activeTab?.scrollIntoView({
      behavior: "smooth",
      block: "nearest",
      inline: "center",
    });
  }, [location.pathname, tabs]);

  const scrollTabs = (direction) =>
    scrollRef.current?.scrollBy({ left: direction * 280, behavior: "smooth" });

  const isTabActive = (tabPath) => {
    if (location.pathname === tabPath) {
      return true;
    }

    /*
     * List/Add/View/Edit routes share the same route tab.
     *
     * Examples:
     *   /users/1006                -> /users
     *   /users/new                 -> /users
     *   /employees/101             -> /employees
     *   /email-configuration/2     -> /email-configuration
     *
     * Home (/) must remain exact-only; otherwise every route
     * would incorrectly mark the Home tab as active.
     */
    return tabPath !== "/" && location.pathname.startsWith(`${tabPath}/`);
  };

  return (
    <div className="tp-route-tabs">
      <button
        type="button"
        className="tp-tabs-scroll-button left"
        onClick={() => scrollTabs(-1)}
        aria-label="Scroll tabs left"
      >
        <i className="bi bi-chevron-left" />
      </button>
      <div
        ref={scrollRef}
        className="tp-route-tabs-scroll"
        onWheel={(event) => {
          if (Math.abs(event.deltaY) > Math.abs(event.deltaX)) {
            scrollRef.current.scrollLeft += event.deltaY;
          }
        }}
      >
        {tabs.map((tab) => (
          <div
            key={tab.path}
            className={`tp-route-tab ${isTabActive(tab.path) ? "active" : ""}`}
          >
            <NavLink to={tab.path}>
              <SvgIcon name={tab.svgIcon || "home"} size={15} />
              <span>{tab.label}</span>
            </NavLink>
            {tab.closable && (
              <button
                type="button"
                onClick={() => closeTab(tab.path)}
                aria-label={`Close ${tab.label}`}
              >
                <i className="bi bi-x" />
              </button>
            )}
          </div>
        ))}
      </div>
      <button
        type="button"
        className="tp-tabs-scroll-button right"
        onClick={() => scrollTabs(1)}
        aria-label="Scroll tabs right"
      >
        <i className="bi bi-chevron-right" />
      </button>
    </div>
  );
}
