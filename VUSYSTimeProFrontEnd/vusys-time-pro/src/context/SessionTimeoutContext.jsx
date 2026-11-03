import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "./AuthContext";
import { isPublicAuthRoutePath } from "../constants/authConstants";
import { ROUTE_CONSTANTS } from "../constants/RouteConstants";

const SessionTimeoutContext = createContext(null);

const DEFAULT_TIMEOUT_MS = 30 * 60 * 1000;

export function SessionTimeoutProvider({
  children,
  timeoutMs = DEFAULT_TIMEOUT_MS,
}) {
  const { isAuthenticated, logout } = useAuth();

  const navigate = useNavigate();

  const location = useLocation();

  const timerRef = useRef(null);

  const [expired, setExpired] = useState(false);

  const isPublicRoute = isPublicAuthRoutePath(location.pathname);

  // ==========================================================
  // CLEAR TIMER
  // ==========================================================

  const clearTimer = useCallback(() => {
    if (timerRef.current) {
      window.clearTimeout(timerRef.current);
    }

    timerRef.current = null;
  }, []);

  // ==========================================================
  // EXPIRE SESSION
  // ==========================================================

  const expireSession = useCallback(() => {
    /*
     * Session Expired popup is valid only while the user is
     * authenticated and currently on a protected application
     * route.
     *
     * This prevents an already-raised session-expired event
     * from remaining visible after React navigates to /login.
     */
    if (!isAuthenticated || isPublicRoute) {
      return;
    }

    clearTimer();

    setExpired(true);
  }, [clearTimer, isAuthenticated, isPublicRoute]);

  // ==========================================================
  // RESET TIMER
  // ==========================================================

  const resetTimer = useCallback(() => {
    if (!isAuthenticated || expired || isPublicRoute) {
      return;
    }

    clearTimer();

    timerRef.current = window.setTimeout(expireSession, timeoutMs);
  }, [
    clearTimer,
    expireSession,
    expired,
    isAuthenticated,
    isPublicRoute,
    timeoutMs,
  ]);

  // ==========================================================
  // SESSION RESTORED
  // ==========================================================

  useEffect(() => {
    /*
     * IMPORTANT:
     *
     * axiosSetup.js dispatches "sessionRestored" after:
     *
     * 1. Successful Login
     * 2. Successful Refresh Token
     *
     * This listener must remain active even when the user is
     * currently unauthenticated.
     *
     * During Login, storeAuthenticationTokens() can dispatch
     * sessionRestored BEFORE AuthContext changes
     * isAuthenticated to true.
     *
     * Therefore this must NOT be placed inside the
     * authenticated-only activity listener effect.
     */
    const handleSessionRestored = () => {
      clearTimer();

      setExpired(false);
    };

    window.addEventListener("sessionRestored", handleSessionRestored);

    return () => {
      window.removeEventListener("sessionRestored", handleSessionRestored);
    };
  }, [clearTimer]);

  // ==========================================================
  // PUBLIC ROUTE / LOGOUT CLEANUP
  // ==========================================================

  useEffect(() => {
    /*
     * IMPORTANT:
     *
     * The Session Expired modal must never be displayed on:
     *
     * - Login
     * - Forgot Password
     * - Reset Password
     * - OTP Authentication
     *
     * Also clear an old expired state when the authenticated
     * user has been removed during Logout.
     */
    if (!isAuthenticated || isPublicRoute) {
      clearTimer();

      setExpired(false);
    }
  }, [clearTimer, isAuthenticated, isPublicRoute]);

  // ==========================================================
  // SESSION ACTIVITY LISTENERS
  // ==========================================================

  useEffect(() => {
    /*
     * Activity tracking and Session Expired listeners are
     * required only while the user is authenticated and on a
     * protected application route.
     */
    if (!isAuthenticated || isPublicRoute) {
      clearTimer();

      return undefined;
    }

    const activityEvents = ["mousedown", "keydown", "touchstart", "scroll"];

    const handleActivity = () => {
      resetTimer();
    };

    activityEvents.forEach((eventName) =>
      window.addEventListener(eventName, handleActivity, {
        passive: true,
      }),
    );

    // --------------------------------------------------------
    // APPLICATION SESSION EXPIRED EVENT
    // --------------------------------------------------------

    window.addEventListener("timepro:session-expired", expireSession);

    // --------------------------------------------------------
    // AXIOS SESSION EXPIRED EVENT
    // --------------------------------------------------------

    window.addEventListener("sessionExpired", expireSession);

    // --------------------------------------------------------
    // START INACTIVITY TIMER
    // --------------------------------------------------------

    resetTimer();

    return () => {
      clearTimer();

      activityEvents.forEach((eventName) =>
        window.removeEventListener(eventName, handleActivity),
      );

      window.removeEventListener("timepro:session-expired", expireSession);

      window.removeEventListener("sessionExpired", expireSession);
    };
  }, [clearTimer, expireSession, isAuthenticated, isPublicRoute, resetTimer]);

  // ==========================================================
  // SIGN IN AGAIN
  // ==========================================================

  const signInAgain = useCallback(async () => {
    clearTimer();

    setExpired(false);

    try {
      await logout();
    } catch {
      /*
       * A session may already be expired, therefore the
       * Logout API can fail.
       *
       * Navigation to Login must still continue.
       */
    }

    navigate(ROUTE_CONSTANTS.AUTH.LOGIN, {
      replace: true,

      state: {
        sessionExpired: true,
      },
    });
  }, [clearTimer, logout, navigate]);

  // ==========================================================
  // CONTEXT VALUE
  // ==========================================================

  const value = useMemo(
    () => ({
      expired,

      resetTimer,

      expireSession,

      signInAgain,
    }),
    [expired, expireSession, resetTimer, signInAgain],
  );

  return (
    <SessionTimeoutContext.Provider value={value}>
      {children}
    </SessionTimeoutContext.Provider>
  );
}

export const useSessionTimeout = () => useContext(SessionTimeoutContext);
