import { createPortal } from "react-dom";
import { useLocation } from "react-router-dom";
import { useSessionTimeout } from "../../context/SessionTimeoutContext";
import { useAuth } from "../../context/AuthContext";
import SvgIcon from "./SvgIcon";
import { PUBLIC_AUTH_ROUTES } from "../../constants/authConstants";

const isPublicAuthPath = (pathname = "") => {
  const path = String(pathname || "")
    .trim()
    .toLowerCase();

  return PUBLIC_AUTH_ROUTES.some(
    (route) => path === route || path.startsWith(`${route}/`),
  );
};

export default function SessionTimeoutModal() {
  const session = useSessionTimeout();
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  /*
   * Final UI guard:
   *
   * Even if a session-expired event was raised immediately before
   * navigation, never render this modal on a public authentication
   * screen or when there is no authenticated user.
   */
  if (
    !session?.expired ||
    !isAuthenticated ||
    isPublicAuthPath(location.pathname)
  ) {
    return null;
  }

  return createPortal(
    <div
      className="tp-session-layer"
      role="dialog"
      aria-modal="true"
    >
      <div className="tp-session-backdrop" />

      <div className="tp-session-card">
        <div className="tp-session-icon tp-session-clock-icon">
          <SvgIcon
            name="clock"
            size={38}
            strokeWidth={1.7}
          />
        </div>

        <h3>Session Expired</h3>

        <p>
          Your session has expired due to inactivity. Please sign in
          again to continue.
        </p>

        <button
          type="button"
          className="tp-primary-button"
          onClick={session.signInAgain}
        >
          <i className="bi bi-box-arrow-in-right" /> Sign in again
        </button>
      </div>
    </div>,
    document.body,
  );
}
