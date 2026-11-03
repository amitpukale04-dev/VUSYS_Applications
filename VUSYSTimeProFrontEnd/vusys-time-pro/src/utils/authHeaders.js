import { ContentType } from "./url";

export const USER_ACCESS_TOKEN_KEY = "auth_token";
export const USER_REFRESH_TOKEN_KEY = "refresh_token";

/** Reads the current user access token from localStorage. */
export const getAccessToken = () => {
  try {
    return localStorage.getItem(USER_ACCESS_TOKEN_KEY) || "";
  } catch {
    return "";
  }
};

/** Reads the current user refresh token from localStorage. */
export const getRefreshToken = () => {
  try {
    return localStorage.getItem(USER_REFRESH_TOKEN_KEY) || "";
  } catch {
    return "";
  }
};

/** Builds request headers and includes the Bearer token when available. */
export const buildAuthHeaders = (accessToken) => {
  const token = accessToken || getAccessToken();

  return {
    "Content-Type": ContentType,
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
};

/** Builds request headers without an Authorization token. */
export const buildAuthHeadersWithoutAccessToken = () => ({
  "Content-Type": ContentType,
});
