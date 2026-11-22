// src/constants/authConstants.js

import { ROUTE_CONSTANTS } from "./RouteConstants";

// ============================================================
// PUBLIC AUTHENTICATION ROUTES
// ============================================================

export const PUBLIC_AUTH_ROUTES = Object.freeze([
  ROUTE_CONSTANTS.AUTH.LOGIN,
  ROUTE_CONSTANTS.AUTH.FORGOT_PASSWORD,
  ROUTE_CONSTANTS.AUTH.RESET_PASSWORD,
  ROUTE_CONSTANTS.AUTH.OTP_AUTHENTICATION,
]);

// ============================================================
// CHECK PUBLIC AUTHENTICATION ROUTE
// ============================================================

export const isPublicAuthRoutePath = (pathname = "") => {
  const path = String(pathname || "")
    .trim()
    .toLowerCase();

  return PUBLIC_AUTH_ROUTES.some(
    (route) => path === route || path.startsWith(`${route}/`),
  );
};
