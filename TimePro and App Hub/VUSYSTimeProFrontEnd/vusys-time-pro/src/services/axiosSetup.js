// src/services/axiosSetup.js

import axios from "axios";
import { detectNetworkOrCorsError } from "../utils/networkUtils";
import { auth, ContentType, ensureBaseUrl, baseUrl } from "../utils/url";
import ToastService from "./toastService";
import { handleApiValidationErrors } from "../utils/CustomAPIValidation";
import { COMMON_CONSTANTS } from "../constants/constants";
import { isPublicAuthRoutePath } from "../constants/authConstants";
import { isNullOrWhiteSpace } from "../utils/valueUtils";
import { STORAGE_KEYS } from "../utils/storageKeys";

const AXIOS_GUARD_KEY = "__AXIOS_INTERCEPTORS_INSTALLED__";

// ============================================================
// UNIFIED USER TOKEN STORAGE KEYS
// ============================================================

const USER_ACCESS_TOKEN_KEY = STORAGE_KEYS.token;

const USER_REFRESH_TOKEN_KEY = STORAGE_KEYS.refreshToken;

const USER_ACCESS_TOKEN_EXPIRY_KEY = STORAGE_KEYS.tokenExpiryDateTime;

const USER_REFRESH_TOKEN_EXPIRY_KEY = STORAGE_KEYS.refreshTokenExpiryDateTime;

// ============================================================
// SESSION STATE KEYS
// ============================================================

/*
 * Persistent state for the current browser tab.
 *
 * This prevents sessionExpired from being lost when AxiosSetup
 * detects expiry before React / SessionTimeOutModal is mounted.
 */
const SESSION_EXPIRED_STATE_KEY = STORAGE_KEYS.sessionExpired;

/*
 * Cross-tab synchronization keys.
 */
const SESSION_EXPIRED_BROADCAST_KEY = STORAGE_KEYS.sessionExpiredBroadcast;

const SESSION_REFRESH_BROADCAST_KEY = STORAGE_KEYS.sessionRefreshedBroadcast;

// ============================================================
// PUBLIC AUTHENTICATION ROUTES
// ============================================================

/*
 * Missing authentication storage is expected on these screens.
 *
 * Therefore Session Timeout must NOT be shown merely because
 * auth_token / refresh_token are missing on these routes.
 */

// ============================================================
// PUBLIC AUTHENTICATION API ENDPOINTS
// ============================================================

/*
 * These APIs do NOT require an Access Token.
 *
 * IMPORTANT:
 * Public API endpoint handling is separate from
 * PUBLIC_AUTH_ROUTES.
 */
const PUBLIC_AUTH_API_ENDPOINTS = [
  "/auth/login",
  "/auth/refreshtoken",
  "/auth/refresh",
  "/auth/forgotpasswordrequest",
  "/auth/forgotpassword",
  "/auth/resetpassword",
  "/auth/resetpasswordrequest",
  "/auth/checkforpasswordlinkavailable",
  "/auth/verifyotp",
  "/auth/verify-otp",
  "/otp/generateorresendotp",
];

/**
 * Checks whether current browser route is a public
 * authentication route.
 */
const isPublicAuthRoute = () => {
  try {
    return isPublicAuthRoutePath(window.location.pathname);
  } catch {
    return false;
  }
};

/**
 * Checks whether an API URL belongs to one of our public
 * authentication endpoints.
 */
const isPublicAuthApi = (url = "") => {
  try {
    const normalizedUrl = String(url || "")
      .trim()
      .toLowerCase();

    if (!normalizedUrl) {
      return false;
    }

    return PUBLIC_AUTH_API_ENDPOINTS.some((endpoint) =>
      normalizedUrl.includes(endpoint),
    );
  } catch {
    return false;
  }
};

/**
 * Returns true when current screen should have
 * an authenticated user session.
 */
const isProtectedRoute = () => {
  return !isPublicAuthRoute();
};

// ============================================================
// ACCESS TOKEN AUTO REFRESH TIMER
// ============================================================

let accessTokenRefreshTimer = null;

const clearAccessTokenRefreshTimer = () => {
  if (accessTokenRefreshTimer) {
    window.clearTimeout(accessTokenRefreshTimer);

    accessTokenRefreshTimer = null;
  }
};

// ============================================================
// EXPLICIT LOGOUT STATE
// ============================================================

/*
 * Explicit user Logout is different from Session Expiration.
 *
 * While Logout is in progress, another protected request or token check
 * may briefly observe authentication values being cleared. That must NOT
 * raise the Session Expired popup.
 *
 * IMPORTANT:
 * This state is intentionally kept only in memory. Do not persist it in
 * Local Storage or Session Storage.
 */
let explicitLogoutInProgress = false;

/**
 * Call immediately before the Logout API request.
 */
export const beginExplicitLogout = () => {
  explicitLogoutInProgress = true;

  /*
   * Prevent an already scheduled access-token refresh from running while
   * the explicit Logout flow is in progress.
   */
  clearAccessTokenRefreshTimer();
};

/**
 * Call only when the Logout API fails and the authenticated session should
 * continue normally.
 */
export const cancelExplicitLogout = () => {
  explicitLogoutInProgress = false;

  /*
   * Logout failed, so restore the normal auto-refresh schedule when the
   * current authentication state is still valid.
   */
  try {
    if (!isPublicAuthRoute()) {
      const authState = getAuthenticationStorageState();

      if (authState.hasCompleteAuthentication) {
        scheduleAccessTokenRefresh();
      }
    }
  } catch (error) {
    void error;
  }
};

const isExplicitLogoutInProgress = () => explicitLogoutInProgress;

// ============================================================
// SESSION EXPIRED / RESTORED
// ============================================================

const dispatchSessionExpiredEvent = () => {
  try {
    window.dispatchEvent(new Event("sessionExpired"));
  } catch (error) {
    void error;
  }
};

/**
 * Trigger Session Timeout.
 *
 * IMPORTANT:
 *
 * AxiosSetup may execute before React is completely mounted.
 *
 * Therefore we:
 *
 * 1. Persist expiry in sessionStorage.
 * 2. Store expiry in window.__SESSION_EXPIRED__.
 * 3. Dispatch sessionExpired immediately.
 * 4. Re-dispatch shortly afterward.
 * 5. Dispatch after DOMContentLoaded when needed.
 * 6. Broadcast expiry to other TimePro tabs.
 */
const triggerSessionExpired = () => {
  /*
   * A user-initiated Logout must never be converted into a Session Expired
   * state while the application is clearing authentication data.
   */
  if (isExplicitLogoutInProgress()) {
    return;
  }

  clearAccessTokenRefreshTimer();

  let newlyExpired = false;

  // --------------------------------------------------------
  // Persist Session Expiry
  // --------------------------------------------------------

  try {
    sessionStorage.setItem(SESSION_EXPIRED_STATE_KEY, "true");
  } catch (error) {
    void error;
  }

  // --------------------------------------------------------
  // In-memory Session Expiry Flag
  // --------------------------------------------------------

  try {
    if (!window.__SESSION_EXPIRED__) {
      window.__SESSION_EXPIRED__ = true;

      newlyExpired = true;
    }
  } catch (error) {
    void error;
  }

  // --------------------------------------------------------
  // Dispatch Immediately
  // --------------------------------------------------------

  dispatchSessionExpiredEvent();

  // --------------------------------------------------------
  // Re-dispatch after React startup cycle
  // --------------------------------------------------------

  try {
    window.setTimeout(() => {
      let isStillExpired = false;

      try {
        isStillExpired =
          sessionStorage.getItem(SESSION_EXPIRED_STATE_KEY) === "true";
      } catch {
        isStillExpired = window.__SESSION_EXPIRED__ === true;
      }

      if (isStillExpired) {
        dispatchSessionExpiredEvent();
      }
    }, 150);
  } catch (error) {
    void error;
  }

  // --------------------------------------------------------
  // Dispatch after DOMContentLoaded
  // --------------------------------------------------------

  try {
    if (document.readyState === "loading") {
      document.addEventListener(
        "DOMContentLoaded",
        () => {
          let isStillExpired = false;

          try {
            isStillExpired =
              sessionStorage.getItem(SESSION_EXPIRED_STATE_KEY) === "true";
          } catch {
            isStillExpired = window.__SESSION_EXPIRED__ === true;
          }

          if (isStillExpired) {
            dispatchSessionExpiredEvent();
          }
        },
        {
          once: true,
        },
      );
    }
  } catch (error) {
    void error;
  }

  // --------------------------------------------------------
  // Cross-tab Broadcast
  // --------------------------------------------------------

  if (newlyExpired) {
    try {
      localStorage.setItem(SESSION_EXPIRED_BROADCAST_KEY, String(Date.now()));
    } catch (error) {
      void error;
    }
  }
};

/**
 * Session becomes valid again after:
 *
 * Login Success
 * Refresh Token Success
 */
const markSessionRestored = () => {
  /*
   * Successful Login / RefreshToken means any previous explicit Logout flow
   * is complete and normal session-expiry handling can resume.
   */
  explicitLogoutInProgress = false;

  try {
    window.__SESSION_EXPIRED__ = false;
  } catch (error) {
    void error;
  }

  try {
    sessionStorage.removeItem(SESSION_EXPIRED_STATE_KEY);
  } catch (error) {
    void error;
  }

  try {
    window.dispatchEvent(new Event("sessionRestored"));
  } catch (error) {
    void error;
  }

  try {
    localStorage.setItem(SESSION_REFRESH_BROADCAST_KEY, String(Date.now()));
  } catch (error) {
    void error;
  }
};

/**
 * React components can use this method when they mount.
 *
 * This is important because sessionExpired may have been
 * triggered before React finished mounting.
 */
export const isSessionExpired = () => {
  try {
    return (
      window.__SESSION_EXPIRED__ === true ||
      sessionStorage.getItem(SESSION_EXPIRED_STATE_KEY) === "true"
    );
  } catch {
    return window.__SESSION_EXPIRED__ === true;
  }
};

// ============================================================
// SESSION EXPIRED ERROR
// ============================================================

/**
 * Creates a special Session Expired error.
 *
 * IMPORTANT:
 *
 * Session expiry is already displayed through the
 * SessionTimeoutModal.
 *
 * Therefore this error must NOT produce another Toast
 * notification from:
 *
 * - Axios global error handling
 * - Service catch blocks
 * - Page/component catch blocks
 */
const createSessionExpiredError = () => {
  const error = new Error("Session expired");

  error.name = "SessionExpiredError";

  error.isSessionExpired = true;

  /*
   * This is the standard flag already used throughout
   * TimePro to prevent duplicate/global Toast handling.
   */
  error.__globalToastHandled = true;

  error.customMessage = "Session expired";

  return error;
};

// ============================================================
// AUTHENTICATION STORAGE STATE
// ============================================================

const getAuthenticationStorageState = () => {
  let accessToken = null;

  let refreshToken = null;

  let accessExpiryRaw = null;

  let refreshExpiryRaw = null;

  try {
    accessToken = localStorage.getItem(USER_ACCESS_TOKEN_KEY);

    refreshToken = localStorage.getItem(USER_REFRESH_TOKEN_KEY);

    accessExpiryRaw = localStorage.getItem(USER_ACCESS_TOKEN_EXPIRY_KEY);

    refreshExpiryRaw = localStorage.getItem(USER_REFRESH_TOKEN_EXPIRY_KEY);
  } catch {
    /*
     * If Local Storage cannot be read on a protected screen,
     * treat it as a missing session.
     */
    return {
      accessToken: null,

      refreshToken: null,

      accessExpiryRaw: null,

      refreshExpiryRaw: null,

      hasAccessToken: false,

      hasRefreshToken: false,

      hasAccessExpiry: false,

      hasRefreshExpiry: false,

      accessExpiry: NaN,

      refreshExpiry: NaN,

      isAccessExpiryValid: false,

      isRefreshExpiryValid: false,

      hasCompleteAuthentication: false,
    };
  }

  const hasAccessToken = !isNullOrWhiteSpace(accessToken);

  const hasRefreshToken = !isNullOrWhiteSpace(refreshToken);

  const hasAccessExpiry = !isNullOrWhiteSpace(accessExpiryRaw);

  const hasRefreshExpiry = !isNullOrWhiteSpace(refreshExpiryRaw);

  const accessExpiry = hasAccessExpiry ? Date.parse(accessExpiryRaw) : NaN;

  const refreshExpiry = hasRefreshExpiry ? Date.parse(refreshExpiryRaw) : NaN;

  const isAccessExpiryValid = !Number.isNaN(accessExpiry);

  const isRefreshExpiryValid = !Number.isNaN(refreshExpiry);

  const hasCompleteAuthentication =
    hasAccessToken &&
    hasRefreshToken &&
    hasAccessExpiry &&
    hasRefreshExpiry &&
    isAccessExpiryValid &&
    isRefreshExpiryValid;

  return {
    accessToken,

    refreshToken,

    accessExpiryRaw,

    refreshExpiryRaw,

    hasAccessToken,

    hasRefreshToken,

    hasAccessExpiry,

    hasRefreshExpiry,

    accessExpiry,

    refreshExpiry,

    isAccessExpiryValid,

    isRefreshExpiryValid,

    hasCompleteAuthentication,
  };
};

/**
 * Missing authentication values should cause Session Timeout
 * only on protected routes.
 */
const hasMissingAuthenticationSession = () => {
  if (!isProtectedRoute()) {
    return false;
  }

  const state = getAuthenticationStorageState();

  return (
    !state.hasAccessToken ||
    !state.hasRefreshToken ||
    !state.hasAccessExpiry ||
    !state.hasRefreshExpiry ||
    !state.isAccessExpiryValid ||
    !state.isRefreshExpiryValid
  );
};

// ============================================================
// STORE AUTHENTICATION TOKENS
// ============================================================

export const storeAuthenticationTokens = (details = {}) => {
  try {
    const accessToken = details.accessToken || details.AccessToken || "";

    const refreshToken = details.refreshToken || details.RefreshToken || "";

    const tokenMinutes = Number(details.TokenExpirationInMinutes || 0);

    const refreshDays = Number(details.RefreshTokenExpiresAt || 0);

    // --------------------------------------------------------
    // Store Access Token
    // --------------------------------------------------------

    if (!isNullOrWhiteSpace(accessToken)) {
      localStorage.setItem(USER_ACCESS_TOKEN_KEY, String(accessToken));
    }

    // --------------------------------------------------------
    // Store Refresh Token
    // --------------------------------------------------------

    if (!isNullOrWhiteSpace(refreshToken)) {
      localStorage.setItem(USER_REFRESH_TOKEN_KEY, String(refreshToken));
    }

    // --------------------------------------------------------
    // Store Token Expiry Configuration
    // --------------------------------------------------------

    if (tokenMinutes > 0) {
      localStorage.setItem(
        STORAGE_KEYS.tokenExpirationInMinutes,
        String(tokenMinutes),
      );
    }

    if (refreshDays > 0) {
      localStorage.setItem(
        STORAGE_KEYS.refreshTokenExpiresAt,
        String(refreshDays),
      );
    }

    // ========================================================
    // ACCESS TOKEN EXPIRY
    // ========================================================

    let accessExpiry = null;

    if (details.token_expiry_date_time) {
      const rawExpiry = String(details.token_expiry_date_time).trim();

      const parsed = new Date(rawExpiry);

      if (!Number.isNaN(parsed.getTime())) {
        accessExpiry = parsed;
      } else {
        /*
         * Support API date format:
         *
         * 08-Aug-2026 13:30:00
         */
        const match = rawExpiry.match(
          /^(\d{1,2})-([A-Za-z]{3})-(\d{4})\s+(\d{1,2}):(\d{2}):(\d{2})$/,
        );

        if (match) {
          const months = {
            jan: 0,
            feb: 1,
            mar: 2,
            apr: 3,
            may: 4,
            jun: 5,
            jul: 6,
            aug: 7,
            sep: 8,
            oct: 9,
            nov: 10,
            dec: 11,
          };

          const month = months[match[2].toLowerCase()];

          if (month !== undefined) {
            accessExpiry = new Date(
              Number(match[3]),
              month,
              Number(match[1]),
              Number(match[4]),
              Number(match[5]),
              Number(match[6]),
            );
          }
        }
      }
    }

    /*
     * RefreshToken API returns TokenExpirationInMinutes.
     *
     * If explicit access-token expiry is unavailable,
     * calculate expiry from configured minutes.
     */
    if (!accessExpiry && tokenMinutes > 0) {
      accessExpiry = new Date(Date.now() + tokenMinutes * 60_000);
    }

    if (accessExpiry) {
      localStorage.setItem(
        USER_ACCESS_TOKEN_EXPIRY_KEY,
        accessExpiry.toISOString(),
      );
    }

    // ========================================================
    // REFRESH TOKEN EXPIRY
    // ========================================================

    if (refreshDays > 0) {
      const refreshExpiry = new Date(
        Date.now() + refreshDays * 24 * 60 * 60_000,
      );

      localStorage.setItem(
        USER_REFRESH_TOKEN_EXPIRY_KEY,
        refreshExpiry.toISOString(),
      );
    }

    /*
     * Successful Login / RefreshToken restores session.
     */
    markSessionRestored();

    /*
     * Schedule next access-token refresh.
     */
    scheduleAccessTokenRefresh();
  } catch (error) {
    console.error("Unable to store authentication tokens", error);
  }
};

// ============================================================
// API BASE URL
// ============================================================

let API_BASE_URL = "";

try {
  API_BASE_URL = baseUrl || "";
} catch (error) {
  API_BASE_URL = "";

  void error;
}

// ============================================================
// TOAST HELPERS
// ============================================================

const safeShowToast = (message, type = COMMON_CONSTANTS.ERROR) => {
  const toastMethod =
    type === COMMON_CONSTANTS.ERROR
      ? ToastService.error
      : ToastService.information;

  toastMethod(message);
};

// ============================================================
// STRONG GLOBAL ERROR DE-DUPLICATION
// GUARANTEED SINGLE TOAST
// ============================================================

const ERROR_SUPPRESSION_WINDOW = 6000;

const suppressedErrors = new Map();

const canonicalizeErrorMessage = (msg) =>
  (msg || "").toString().replace(/\s+/g, " ").trim().toLowerCase();

const showOnceGlobalErrorToast = (message, keyOverride = null) => {
  if (!message) {
    return;
  }

  const key = keyOverride
    ? String(keyOverride).toLowerCase()
    : canonicalizeErrorMessage(message);

  if (suppressedErrors.has(key)) {
    return;
  }

  suppressedErrors.set(key, Date.now());

  setTimeout(() => {
    suppressedErrors.delete(key);
  }, ERROR_SUPPRESSION_WINDOW);

  safeShowToast(message, COMMON_CONSTANTS.ERROR);
};

// ============================================================
// API SERVICE DOWN / AUTH FAILED
// ============================================================

const API_DOWN_FLAG = "__API_SERVICE_DOWN_ACTIVE__";

const API_DOWN_AT = "__API_SERVICE_DOWN_AT__";

const AUTH_FAIL_FLAG = "__AUTH_FAILED_ACTIVE__";

const AUTH_FAIL_AT = "__AUTH_FAILED_AT__";

const API_DOWN_SUPPRESSION_WINDOW = 15000;

const AUTH_FAIL_SUPPRESSION_WINDOW = 15000;

const isApiServiceDownMessage = (msg) => {
  const m = canonicalizeErrorMessage(msg);

  return (
    m.includes("api service is not running") ||
    m.includes("unable to connect") ||
    m.includes("failed to fetch") ||
    m.includes("net::err_connection_refused") ||
    m.includes("connection refused") ||
    m.includes("service unavailable") ||
    m.includes("network error")
  );
};

const isAuthFailedTokenInvalidMessage = (msg) => {
  const m = canonicalizeErrorMessage(msg);

  return (
    m.includes("authentication failed") &&
    (m.includes("missing") || m.includes("expired") || m.includes("invalid"))
  );
};

const isApiDownActive = () => {
  try {
    if (!window[API_DOWN_FLAG]) {
      return false;
    }

    const at = Number(window[API_DOWN_AT] || 0);

    if (!at) {
      return true;
    }

    return Date.now() - at <= API_DOWN_SUPPRESSION_WINDOW;
  } catch (error) {
    void error;

    return false;
  }
};

const markApiDownActive = () => {
  try {
    window[API_DOWN_FLAG] = true;

    window[API_DOWN_AT] = Date.now();
  } catch (error) {
    void error;
  }
};

const clearApiDownActive = () => {
  try {
    window[API_DOWN_FLAG] = false;

    window[API_DOWN_AT] = 0;
  } catch (error) {
    void error;
  }
};

const isAuthFailActive = () => {
  try {
    if (!window[AUTH_FAIL_FLAG]) {
      return false;
    }

    const at = Number(window[AUTH_FAIL_AT] || 0);

    if (!at) {
      return true;
    }

    return Date.now() - at <= AUTH_FAIL_SUPPRESSION_WINDOW;
  } catch (error) {
    void error;

    return false;
  }
};

const markAuthFailActive = () => {
  try {
    window[AUTH_FAIL_FLAG] = true;

    window[AUTH_FAIL_AT] = Date.now();
  } catch (error) {
    void error;
  }
};

const clearAuthFailActive = () => {
  try {
    window[AUTH_FAIL_FLAG] = false;

    window[AUTH_FAIL_AT] = 0;
  } catch (error) {
    void error;
  }
};

// ============================================================
// REFRESH TOKEN SUPPORT
// ============================================================

let isRefreshing = false;

let refreshQueue = [];

const REFRESH_ENDPOINT_PATH = auth.refresh_token;

// ============================================================
// GET CURRENT TOKENS
// ============================================================

const getCurrentAccessToken = () => {
  try {
    const token = localStorage.getItem(USER_ACCESS_TOKEN_KEY);

    return isNullOrWhiteSpace(token) ? null : token;
  } catch {
    return null;
  }
};

const getCurrentRefreshToken = () => {
  try {
    const token = localStorage.getItem(USER_REFRESH_TOKEN_KEY);

    return isNullOrWhiteSpace(token) ? null : token;
  } catch {
    return null;
  }
};

// ============================================================
// CLEAR TOKENS
// ============================================================

const clearUserTokens = () => {
  clearAccessTokenRefreshTimer();

  try {
    localStorage.removeItem(USER_ACCESS_TOKEN_KEY);

    localStorage.removeItem(USER_REFRESH_TOKEN_KEY);

    localStorage.removeItem(USER_ACCESS_TOKEN_EXPIRY_KEY);

    localStorage.removeItem(USER_REFRESH_TOKEN_EXPIRY_KEY);

    localStorage.removeItem(STORAGE_KEYS.tokenExpirationInMinutes);

    localStorage.removeItem(STORAGE_KEYS.refreshTokenExpiresAt);
  } catch (error) {
    void error;
  }
};

/**
 * Clears only the in-memory/session runtime state used by
 * Axios token refresh.
 *
 * Call this immediately before a successful explicit Logout
 * clears the persisted login session.
 */
export const clearAuthenticationRuntimeState = () => {
  clearAccessTokenRefreshTimer();

  try {
    window.__SESSION_EXPIRED__ = false;
  } catch (error) {
    void error;
  }

  try {
    sessionStorage.removeItem(SESSION_EXPIRED_STATE_KEY);
  } catch (error) {
    void error;
  }
};

// ============================================================
// REFRESH TOKEN API
// ============================================================

const doRefreshTokenRequest = async () => {
  const refreshToken = getCurrentRefreshToken();

  if (!refreshToken) {
    throw createSessionExpiredError();
  }

  let refreshUrl = REFRESH_ENDPOINT_PATH;

  if (API_BASE_URL) {
    refreshUrl = API_BASE_URL.replace(/\/+$/, "") + REFRESH_ENDPOINT_PATH;
  }

  let response;

  try {
    response = await fetch(refreshUrl, {
      method: "POST",

      headers: {
        "Content-Type": ContentType,
      },

      body: JSON.stringify({
        refreshToken,
      }),
    });
  } catch (networkError) {
    let finalMessage = "API Service is not running or unable to connect.";

    try {
      const result = await detectNetworkOrCorsError(refreshUrl);

      finalMessage = result?.message || finalMessage;
    } catch (error) {
      void error;
    }

    if (isApiServiceDownMessage(finalMessage)) {
      markApiDownActive();

      showOnceGlobalErrorToast(
        "API Service is not running or unable to connect.",
        "API_DOWN",
      );

      finalMessage = "API Service is not running or unable to connect.";
    } else {
      showOnceGlobalErrorToast(finalMessage);
    }

    networkError.customMessage = finalMessage;

    networkError.message = finalMessage;

    networkError.__globalToastHandled = true;

    throw networkError;
  }

  let data = null;

  try {
    data = await response.json();
  } catch {
    data = null;
  }

  // ========================================================
  // COMMON API VALIDATIONS
  // ========================================================

  const validationErrors =
    data?.data?.ValidationErrors || data?.ValidationErrors || [];

  if (Array.isArray(validationErrors) && validationErrors.length > 0) {
    handleApiValidationErrors(validationErrors);

    const validationError = new Error("API validation failed.");

    validationError.isApiValidationError = true;

    validationError.validationErrors = validationErrors;

    validationError.__globalToastHandled = true;

    throw validationError;
  }

  const status = String(data?.Status || data?.status || "")
    .trim()
    .toLowerCase();

  // ========================================================
  // REFRESH TOKEN API ERROR
  // ========================================================

  if (!response.ok || (status && status !== COMMON_CONSTANTS.SUCCESS)) {
    const globalError =
      data?.data?.GLOBAL_ERROR_RESPONSE || data?.GLOBAL_ERROR_RESPONSE;

    const globalDetail =
      Array.isArray(globalError) && globalError.length > 0
        ? globalError[0]?.ErrorDetail || globalError[0]?.Title
        : null;

    const message =
      globalDetail ||
      data?.Message ||
      data?.message ||
      `Failed to refresh token${
        response.status ? ` (${response.status})` : ""
      }.`;

    const refreshError = new Error(message);

    refreshError.status = response.status;

    throw refreshError;
  }

  // ========================================================
  // REFRESH TOKEN SUCCESS RESPONSE
  // ========================================================

  const rows = data?.data?.REFRESH_TOKEN_RESPONSE;

  const tokenContainer =
    Array.isArray(rows) && rows.length > 0 ? rows[0] : null;

  if (!tokenContainer) {
    throw new Error("Refresh token response is empty.");
  }

  const newAccessToken =
    tokenContainer.accessToken || tokenContainer.AccessToken || null;

  if (isNullOrWhiteSpace(newAccessToken)) {
    throw new Error("No access token in refresh response.");
  }

  /*
   * Store rotated access token,
   * rotated refresh token,
   * access expiry,
   * refresh expiry.
   */
  storeAuthenticationTokens(tokenContainer);

  clearApiDownActive();

  clearAuthFailActive();

  return newAccessToken;
};

// ============================================================
// SINGLE REFRESH REQUEST + QUEUE
// ============================================================

const getOrCreateRefreshPromise = () => {
  if (isRefreshing) {
    return new Promise((resolve, reject) => {
      refreshQueue.push({
        resolve,
        reject,
      });
    });
  }

  isRefreshing = true;

  const refreshPromise = doRefreshTokenRequest()
    .then((newAccessToken) => {
      refreshQueue.forEach((item) => item.resolve(newAccessToken));

      refreshQueue = [];

      isRefreshing = false;

      return newAccessToken;
    })
    .catch((error) => {
      refreshQueue.forEach((item) => item.reject(error));

      refreshQueue = [];

      isRefreshing = false;

      throw error;
    });

  return refreshPromise;
};

// ============================================================
// ACCESS TOKEN AUTO REFRESH SCHEDULER
// ============================================================

function scheduleAccessTokenRefresh() {
  clearAccessTokenRefreshTimer();

  /*
   * Do not schedule token refresh while an explicit Logout is in progress.
   */
  if (isExplicitLogoutInProgress()) {
    return;
  }

  /*
   * Never display Session Timeout on Login/Forgot Password/
   * Reset Password/OTP simply because no auth storage exists.
   */
  if (isPublicAuthRoute()) {
    return;
  }

  const authState = getAuthenticationStorageState();

  // ========================================================
  // NULL / EMPTY / INVALID LOCAL STORAGE
  // ========================================================

  if (!authState.hasCompleteAuthentication) {
    triggerSessionExpired();

    clearUserTokens();

    return;
  }

  const { accessExpiry, refreshExpiry } = authState;

  const now = Date.now();

  // ========================================================
  // REFRESH TOKEN ALREADY EXPIRED
  // ========================================================

  if (now >= refreshExpiry) {
    triggerSessionExpired();

    clearUserTokens();

    return;
  }

  // ========================================================
  // ACCESS TOKEN EXPIRY TIMER
  // ========================================================

  const delay = Math.max(0, accessExpiry - now + 100);

  accessTokenRefreshTimer = window.setTimeout(async () => {
    try {
      const currentState = getAuthenticationStorageState();

      // ================================================
      // STORAGE REMOVED / INVALID
      // ================================================

      if (!currentState.hasCompleteAuthentication) {
        /*
         * Explicit logout may have cleared tokens while
         * this timer was waiting.
         *
         * Do not display Session Timeout on a public
         * authentication route.
         */
        if (!isPublicAuthRoute()) {
          triggerSessionExpired();
        }

        clearUserTokens();

        return;
      }

      // ================================================
      // REFRESH TOKEN EXPIRED
      // ================================================

      if (Date.now() >= currentState.refreshExpiry) {
        if (!isPublicAuthRoute()) {
          triggerSessionExpired();
        }

        clearUserTokens();

        return;
      }

      // ================================================
      // ACCESS TOKEN EXPIRED
      // REFRESH TOKEN VALID
      // ================================================

      await getOrCreateRefreshPromise();
    } catch (error) {
      console.error("Automatic refresh token request failed:", error);

      /*
       * Network/API errors may already have displayed
       * their own message.
       *
       * Session timeout is shown only while the user
       * remains on a protected route.
       */
      if (!isPublicAuthRoute()) {
        triggerSessionExpired();
      }

      clearUserTokens();
    }
  }, delay);
}

// ============================================================
// INITIAL AUTHENTICATION SESSION VALIDATION
// ============================================================

const initializeAuthenticationSession = () => {
  /*
   * Login/Forgot Password/Reset Password/OTP
   * are allowed without authenticated session.
   */
  if (isPublicAuthRoute()) {
    return;
  }

  // --------------------------------------------------------
  // Already expired before React mounted
  // --------------------------------------------------------

  try {
    const persistedSessionExpired = sessionStorage.getItem(
      SESSION_EXPIRED_STATE_KEY,
    );

    if (
      String(persistedSessionExpired || "")
        .trim()
        .toLowerCase() === "true"
    ) {
      window.__SESSION_EXPIRED__ = true;

      triggerSessionExpired();

      return;
    }
  } catch (error) {
    void error;
  }

  // --------------------------------------------------------
  // Check Local Storage Session
  // --------------------------------------------------------

  if (hasMissingAuthenticationSession()) {
    triggerSessionExpired();

    clearUserTokens();

    return;
  }

  const state = getAuthenticationStorageState();

  const now = Date.now();

  // --------------------------------------------------------
  // Refresh token already expired
  // --------------------------------------------------------

  if (state.isRefreshExpiryValid && now >= state.refreshExpiry) {
    triggerSessionExpired();

    clearUserTokens();

    return;
  }

  /*
   * Valid stored authentication exists.
   */
  scheduleAccessTokenRefresh();
};

// ============================================================
// AXIOS INTERCEPTORS
// ============================================================

if (!window[AXIOS_GUARD_KEY]) {
  window[AXIOS_GUARD_KEY] = true;

  // ========================================================
  // TOKEN EXPIRY STATE
  // ========================================================

  const getTokenExpiryState = () => {
    const state = getAuthenticationStorageState();

    const now = Date.now();

    /*
     * Missing/invalid expiry information is considered
     * expired for protected API requests.
     */
    const isAccessExpired =
      !state.hasAccessExpiry ||
      !state.isAccessExpiryValid ||
      now >= state.accessExpiry;

    const isRefreshExpired =
      !state.hasRefreshExpiry ||
      !state.isRefreshExpiryValid ||
      now >= state.refreshExpiry;

    return {
      ...state,

      isAccessExpired,

      isRefreshExpired,
    };
  };

  // ========================================================
  // SESSION EXPIRY HANDLER
  // ========================================================

  const handle401SessionExpired = () => {
    /*
     * Explicit Logout is NOT Session Expiration.
     *
     * While Logout is running, protected requests may briefly observe
     * authentication values being cleared. Suppress Session Timeout for
     * that short transition.
     */
    if (isExplicitLogoutInProgress()) {
      return;
    }

    /*
     * Session Timeout belongs only to an authenticated /
     * protected screen.
     */
    if (isPublicAuthRoute()) {
      return;
    }

    triggerSessionExpired();
  };

  // ========================================================
  // ENSURE ACCESS TOKEN IS FRESH
  // ========================================================

  const ensureFreshAccessToken = async () => {
    const state = getTokenExpiryState();

    // ======================================================
    // MISSING / INVALID AUTHENTICATION STORAGE
    // ======================================================

    if (
      !state.hasAccessToken ||
      !state.hasRefreshToken ||
      !state.hasAccessExpiry ||
      !state.hasRefreshExpiry ||
      !state.isAccessExpiryValid ||
      !state.isRefreshExpiryValid
    ) {
      handle401SessionExpired();

      clearUserTokens();

      /*
       * IMPORTANT:
       *
       * Previously:
       *
       * throw new Error("Session expired1");
       *
       * That normal Error was reaching service/page catch
       * blocks and generating duplicate Toast notifications.
       *
       * Session Timeout Modal already communicates the issue,
       * so return our globally-handled session error instead.
       */
      throw createSessionExpiredError();
    }

    // ======================================================
    // ACCESS TOKEN STILL VALID
    // ======================================================

    if (!state.isAccessExpired) {
      return state.accessToken || getCurrentAccessToken();
    }

    // ======================================================
    // ACCESS + REFRESH TOKEN EXPIRED
    // ======================================================

    if (state.isRefreshExpired) {
      handle401SessionExpired();

      clearUserTokens();

      /*
       * Previously:
       *
       * throw new Error("Session expired2");
       *
       * Do not generate a Toast for Session Timeout.
       */
      throw createSessionExpiredError();
    }

    // ======================================================
    // ACCESS TOKEN EXPIRED / REFRESH TOKEN VALID
    // ======================================================

    try {
      const newAccessToken = await getOrCreateRefreshPromise();

      return newAccessToken;
    } catch (error) {
      handle401SessionExpired();

      clearUserTokens();

      /*
       * Preserve errors that have already been intentionally
       * handled globally, such as:
       *
       * - API Service Down
       * - Network/CORS
       * - API validation
       */
      if (error?.__globalToastHandled) {
        throw error;
      }

      /*
       * Any remaining refresh failure means the session can
       * no longer continue.
       *
       * Show only Session Timeout Modal — no extra Toast.
       */
      throw createSessionExpiredError();
    }
  };

  // ========================================================
  // ATTACH AUTH HEADER
  // ========================================================

  const attachAuthHeader = async (config) => {
    ensureBaseUrl();

    const urlString = String(config.url || "");

    const publicAuthApi = isPublicAuthApi(urlString);

    /*
     * Public authentication APIs do NOT require:
     *
     * - Access Token
     * - Refresh Token
     * - Access Token expiry validation
     * - Refresh Token expiry validation
     */
    if (!publicAuthApi) {
      /*
       * IMPORTANT:
       *
       * During explicit Logout, do NOT call
       * ensureFreshAccessToken().
       *
       * Logout itself is a protected API call and must be allowed
       * to reach the backend using the currently available token.
       *
       * Calling ensureFreshAccessToken() here during Logout can
       * reject the request with SessionExpiredError before the
       * Logout API is actually sent.
       */
      if (!isExplicitLogoutInProgress()) {
        await ensureFreshAccessToken();
      }
    }

    /*
     * Authorization header should be attached only
     * to protected APIs.
     */
    if (!publicAuthApi) {
      const token = getCurrentAccessToken();

      if (token) {
        config.headers = config.headers || {};

        config.headers.Authorization = `Bearer ${token}`;
      }
    }

    return config;
  };

  // ========================================================
  // EXTRACT VALIDATION ERRORS
  // ========================================================

  const extractValidationErrors = (payload) => {
    const container = payload?.data || payload;

    const validationErrors = container?.ValidationErrors;

    return Array.isArray(validationErrors) ? validationErrors : [];
  };

  // ========================================================
  // HANDLE SUCCESS RESPONSE
  // ========================================================

  const handleSuccessfulResponse = (response) => {
    const validationErrors = extractValidationErrors(response?.data);
    if (response?.config?.silentUserTabs) return response;
    if (validationErrors.length > 0) {
      handleApiValidationErrors(validationErrors);
      const validationError = new Error("API validation failed.");
      validationError.name = "ApiValidationError";
      validationError.isApiValidationError = true;
      validationError.validationErrors = validationErrors;
      validationError.__globalToastHandled = true;
      validationError.response = response;
      validationError.config = response?.config;
      return Promise.reject(validationError);
    }
    return response;
  };

  // ========================================================
  // GLOBAL ERROR RESPONSE
  // ========================================================

  const extractGlobalErrorDetail = (payload) => {
    const container = payload?.data || payload;

    const globalErr = container?.GLOBAL_ERROR_RESPONSE;

    if (Array.isArray(globalErr) && globalErr.length > 0) {
      const first = globalErr[0] || {};

      return first.ErrorDetail || first.Title || null;
    }

    return null;
  };

  // ========================================================
  // NORMALIZE BACKEND ERROR
  // ========================================================

  const normalizeBackendErrorMessage = (error, detail) => {
    try {
      if (!detail) {
        return;
      }

      error.customMessage = detail;

      error.message = detail;

      const response = error?.response;

      if (response && response.data && typeof response.data === "object") {
        const data = response.data;

        data.message = detail;

        data.Message = detail;

        data.errorMessage = detail;

        data.ErrorMessage = detail;

        data.userMessage = detail;

        if ("Status" in data) {
          data.Status = COMMON_CONSTANTS.ERROR1;
        }

        if ("status" in data && typeof data.status === "string") {
          data.status = COMMON_CONSTANTS.ERROR1;
        }
      }
    } catch (normalizationError) {
      void normalizationError;
    }
  };

  // ========================================================
  // GLOBAL ERROR HANDLER
  // ========================================================

  const handleGlobalError = async (error, instance) => {
    // ====================================================
    // SESSION EXPIRED
    // ====================================================

    /*
     * A SessionExpiredError is intentionally generated by
     * ensureFreshAccessToken().
     *
     * SessionTimeoutModal already handles this condition.
     *
     * Do NOT:
     *
     * - Display Toast
     * - Run network detection
     * - Run generic API error handling
     */
    if (error?.isSessionExpired) {
      error.__globalToastHandled = true;

      return Promise.reject(error);
    }

    const status = error?.response?.status;

    // User-tabs persistence must never produce a toast. Preserve the
    // existing 401/403 refresh/session handling below.
    if (error?.config?.silentUserTabs && status !== 401 && status !== 403) {
      return Promise.reject(error);
    }

    const originalRequest = error?.config;

    const originalRequestUrl = String(originalRequest?.url || "");

    const publicAuthRequest = isPublicAuthApi(originalRequestUrl);

    // ====================================================
    // API VALIDATIONS
    // ====================================================

    const validationErrors = extractValidationErrors(error?.response?.data);

    if (validationErrors.length > 0) {
      if (!error?.__globalToastHandled) {
        handleApiValidationErrors(validationErrors);
      }

      error.isApiValidationError = true;

      error.validationErrors = validationErrors;

      error.__globalToastHandled = true;

      error.customMessage = "API validation failed.";

      return Promise.reject(error);
    }

    // ====================================================
    // NETWORK / CORS / SSL
    // ====================================================

    if (!error.response && error.config) {
      try {
        const requestBaseURL = error.config.baseURL || "";

        const requestUrl = error.config.url || "";

        let apiUrl;

        if (/^https?:\/\//i.test(requestUrl)) {
          apiUrl = requestUrl;
        } else if (requestBaseURL) {
          apiUrl =
            requestBaseURL.replace(/\/+$/, "") +
            "/" +
            requestUrl.replace(/^\/+/, "");
        } else {
          apiUrl = requestUrl || window.location.origin;
        }

        const result = await detectNetworkOrCorsError(apiUrl);

        const finalMessage =
          result?.message || "Network Error. Please try again.";

        if (isApiServiceDownMessage(finalMessage)) {
          if (!isApiDownActive()) {
            markApiDownActive();

            showOnceGlobalErrorToast(
              "API Service is not running or unable to connect.",
              "API_DOWN",
            );

            /*
             * Public authentication APIs must NOT trigger
             * protected-session timeout flow.
             */
            if (!publicAuthRequest) {
              handle401SessionExpired();
            }
          }

          error.customMessage =
            "API Service is not running or unable to connect.";

          error.message = "API Service is not running or unable to connect.";

          error.__globalToastHandled = true;

          return Promise.reject(error);
        }

        showOnceGlobalErrorToast(finalMessage);

        error.customMessage = finalMessage;

        error.message = finalMessage;

        error.__globalToastHandled = true;
      } catch (networkHandlerError) {
        void networkHandlerError;
      }

      return Promise.reject(error);
    }

    // ====================================================
    // EXTRACT GLOBAL ERROR DETAIL
    // ====================================================

    let extractedDetail = null;

    try {
      extractedDetail = extractGlobalErrorDetail(error?.response?.data);

      if (extractedDetail) {
        normalizeBackendErrorMessage(error, extractedDetail);
      }
    } catch (extractionError) {
      void extractionError;
    }

    // ====================================================
    // AUTHENTICATION FAILED
    // ====================================================

    if (extractedDetail && isAuthFailedTokenInvalidMessage(extractedDetail)) {
      /*
       * Public auth APIs must not trigger protected-session
       * timeout flow.
       */
      if (publicAuthRequest) {
        if (!isAuthFailActive()) {
          markAuthFailActive();

          showOnceGlobalErrorToast(extractedDetail, "AUTH_FAILED_PUBLIC");

          error.__globalToastHandled = true;
        }

        return Promise.reject(error);
      }

      if (!isAuthFailActive()) {
        markAuthFailActive();

        showOnceGlobalErrorToast(extractedDetail, "AUTH_FAILED_TOKEN");

        error.__globalToastHandled = true;

        handle401SessionExpired();

        clearUserTokens();
      }

      return Promise.reject(error);
    }

    // ====================================================
    // 401 / 403 FALLBACK REFRESH
    // ====================================================

    if ((status === 401 || status === 403) && originalRequest) {
      /*
       * Public Authentication API:
       *
       * Do NOT:
       * - Refresh Access Token
       * - Trigger Session Timeout
       */
      if (publicAuthRequest) {
        return Promise.reject(error);
      }

      // --------------------------------------------------
      // ALREADY RETRIED
      // --------------------------------------------------

      if (originalRequest._retry) {
        handle401SessionExpired();

        clearUserTokens();

        /*
         * The request has already failed after refresh.
         *
         * Session Timeout modal is sufficient.
         */
        return Promise.reject(createSessionExpiredError());
      }

      const state = getAuthenticationStorageState();

      // --------------------------------------------------
      // NO VALID REFRESH TOKEN
      // --------------------------------------------------

      if (
        !state.hasRefreshToken ||
        !state.hasRefreshExpiry ||
        !state.isRefreshExpiryValid ||
        Date.now() >= state.refreshExpiry
      ) {
        /*
         * Do not show an additional backend token-expired
         * Toast here. Session Timeout modal owns this flow.
         */
        handle401SessionExpired();

        clearUserTokens();

        return Promise.reject(createSessionExpiredError());
      }

      originalRequest._retry = true;

      // --------------------------------------------------
      // REFRESH + RETRY ORIGINAL REQUEST
      // --------------------------------------------------

      try {
        const newAccessToken = await getOrCreateRefreshPromise();

        originalRequest.headers = originalRequest.headers || {};

        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;

        return instance(originalRequest);
      } catch (refreshError) {
        handle401SessionExpired();

        clearUserTokens();

        /*
         * Preserve intentional globally handled errors such
         * as API down/network errors.
         */
        if (refreshError?.__globalToastHandled) {
          return Promise.reject(refreshError);
        }

        return Promise.reject(createSessionExpiredError());
      }
    }

    if (originalRequest?.silentUserTabs) {
      return Promise.reject(error);
    }

    // ====================================================
    // GLOBAL API ERROR RESPONSE
    // ====================================================

    if ([400, 403, 404, 408, 500, 503].includes(status)) {
      let detail = extractedDetail;

      if (!detail) {
        const payload = error?.response?.data;

        const container = payload?.data || payload;

        const globalErr = container?.GLOBAL_ERROR_RESPONSE;

        if (Array.isArray(globalErr) && globalErr.length > 0) {
          const first = globalErr[0] || {};

          detail = first.ErrorDetail || first.Title || "Something went wrong.";
        }

        if (!detail && status === 404) {
          if (payload && typeof payload === "object") {
            detail = payload.detail || payload.title || null;
          }

          if (!detail) {
            detail = "The Requested Resource Was Not Found.";
          }
        }

        if (!detail) {
          detail = "Something went wrong.";
        }
      }

      if (detail && !isApiDownActive() && !isAuthFailActive()) {
        showOnceGlobalErrorToast(detail);

        normalizeBackendErrorMessage(error, detail);

        error.__globalToastHandled = true;
      }
    }

    return Promise.reject(error);
  };

  // ========================================================
  // ATTACH INTERCEPTORS
  // ========================================================

  const attachInterceptors = (instance) => {
    instance.interceptors.request.use(
      (config) => attachAuthHeader(config),

      (error) => Promise.reject(error),
    );

    instance.interceptors.response.use(
      (response) => handleSuccessfulResponse(response),

      (error) => handleGlobalError(error, instance),
    );
  };

  // ========================================================
  // DEFAULT AXIOS
  // ========================================================

  attachInterceptors(axios);

  // ========================================================
  // AXIOS.CREATE()
  // ========================================================

  const originalCreate = axios.create.bind(axios);

  axios.create = (config) => {
    const instance = originalCreate(config);

    attachInterceptors(instance);

    return instance;
  };

  // ========================================================
  // INITIALIZE SESSION
  // ========================================================

  initializeAuthenticationSession();

  // ========================================================
  // CROSS-TAB SESSION SYNCHRONIZATION
  // ========================================================

  window.addEventListener("storage", (event) => {
    /*
     * Another TimePro tab expired.
     */
    if (event.key === SESSION_EXPIRED_BROADCAST_KEY && event.newValue) {
      /*
       * Do not display Session Timeout over Login/Forgot/
       * Reset/OTP screens.
       */
      if (!isPublicAuthRoute()) {
        triggerSessionExpired();
      }

      return;
    }

    /*
     * Another tab logged in or refreshed token.
     */
    if (
      event.key === SESSION_REFRESH_BROADCAST_KEY ||
      event.key === USER_ACCESS_TOKEN_KEY ||
      event.key === USER_REFRESH_TOKEN_KEY ||
      event.key === USER_ACCESS_TOKEN_EXPIRY_KEY ||
      event.key === USER_REFRESH_TOKEN_EXPIRY_KEY
    ) {
      if (isPublicAuthRoute()) {
        return;
      }

      /*
       * If another tab removed authentication completely,
       * expire this tab too.
       */
      if (hasMissingAuthenticationSession()) {
        triggerSessionExpired();

        clearUserTokens();

        return;
      }

      scheduleAccessTokenRefresh();
    }
  });
}

// ============================================================
// SERVICE API ERROR HELPER
// ============================================================

/**
 * Services can call this inside catch and rethrow
 * so UI layers can perform screen-specific error handling.
 */
export const handleApiError = (error) => {
  console.error("Service API error:", error);

  throw error;
};
