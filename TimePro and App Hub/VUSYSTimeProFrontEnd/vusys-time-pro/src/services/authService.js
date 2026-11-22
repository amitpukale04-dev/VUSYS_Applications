import apiClient from "./apiClient";
import { auth } from "../utils/url";
import { buildAuthHeadersWithoutAccessToken } from "../utils/authHeaders";
import { normalizeString } from "../utils/valueUtils";
import {
  getApiResponseFirstRow,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import { COMMON_CONSTANTS, API_RESPONSE_TABLE } from "../constants/constants";

/**
 * Sends the Login request.
 *
 * API ValidationErrors are handled globally by axiosSetup.js through
 * handleApiValidationErrors(). Business Success/Error responses are handled
 * here because the API can return HTTP 200 with Status = "Error".
 */
export async function loginUser({ email, password }) {
  const response = await apiClient.post(
    auth.login,
    {
      emailID: normalizeString(email),
      password: normalizeString(password),
    },
    { headers: buildAuthHeadersWithoutAccessToken() },
  );

  const payload = response?.data || {};
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.USER_LOGIN,
  );
  const status = getApiResponseStatus(payload);

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const message =
      details?.Message ||
      details?.message ||
      payload?.Message ||
      payload?.message ||
      "Unable to sign in. Please verify your Email ID and Password.";
    throw new Error(message);
  }

  return { payload, details };
}

/**
 * Sends the Forgot Password request.
 *
 * API ValidationErrors are handled globally by axiosSetup.js through
 * handleApiValidationErrors(). Business Success/Error responses are handled
 * here because the API can return HTTP 200 with Status = "Error".
 */
export async function forgotPasswordRequest({ email }) {
  const response = await apiClient.post(
    auth.forgot_password,
    {
      emailID: normalizeString(email),
    },
    { headers: buildAuthHeadersWithoutAccessToken() },
  );

  const payload = response?.data || {};
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.FORGOT_PASSWORD_RESPONSE,
  );
  const status = getApiResponseStatus(payload);

  const message =
    details?.Message ||
    details?.message ||
    payload?.Message ||
    payload?.message ||
    (status === COMMON_CONSTANTS.SUCCESS
      ? "Reset Password Link Has Been Sent To Your Email ID !!!"
      : "Unable to process forgot password request.");

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "ForgotPasswordError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return { payload, details, message };
}

/**
 * Verifies that the Reset Password link is still available before the
 * public Reset Password screen is rendered.
 */
export async function checkForPasswordLinkAvailable({ email }) {
  const response = await apiClient.post(
    auth.check_for_password_link_available,
    {
      emailID: normalizeString(email),
    },
    { headers: buildAuthHeadersWithoutAccessToken() },
  );

  const payload = response?.data || {};
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.PASSWORD_LINK_AVAILABLE_RESPONSE,
  );
  const status = getApiResponseStatus(payload);

  const message =
    details?.Message ||
    details?.message ||
    payload?.Message ||
    payload?.message ||
    (status === COMMON_CONSTANTS.SUCCESS
      ? "Password Link Available !!!"
      : "Reset Password Link is not available.");

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "PasswordLinkAvailabilityError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    details,
    message,
  };
}

/**
 * Resets the user's password.
 *
 * Common API ValidationErrors are handled globally by axiosSetup.js through
 * handleApiValidationErrors(). Business Success/Error responses are handled
 * here because the API can return HTTP 200 with Status = "Error".
 */
export async function resetPasswordRequest({
  email,
  newPassword,
  confirmPassword,
}) {
  const response = await apiClient.post(
    auth.reset_password,
    {
      emailID: normalizeString(email),
      newPassword: normalizeString(newPassword),
      confirmPassword: normalizeString(confirmPassword),
    },
    { headers: buildAuthHeadersWithoutAccessToken() },
  );

  const payload = response?.data || {};
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.RESET_PASSWORD_RESPONSE,
  );
  const status = getApiResponseStatus(payload);

  const message =
    details?.Message ||
    details?.message ||
    payload?.Message ||
    payload?.message ||
    (status === COMMON_CONSTANTS.SUCCESS
      ? "Password Changed Successfully !!!"
      : "Unable to reset the password.");

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "ResetPasswordError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    details,
    message,
  };
}

/**
 * Changes the password for the currently authenticated TimePro user.
 *
 * This is a protected API call, so the shared Axios interceptor attaches
 * the current access token and refreshes it automatically when required.
 * Common API ValidationErrors are handled globally by axiosSetup.js through
 * handleApiValidationErrors(). Business Success/Error responses are handled
 * here because the API can return HTTP 200 with Status = "Error".
 */
export async function changePasswordRequest({
  email,
  oldPassword,
  newPassword,
  confirmPassword,
}) {
  const response = await apiClient.post(auth.change_password, {
    emailID: normalizeString(email),
    oldPassword: normalizeString(oldPassword),
    newPassword: normalizeString(newPassword),
    confirmPassword: normalizeString(confirmPassword),
  });

  const payload = response?.data || {};
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.CHANGE_PASSWORD_RESPONSE,
  );
  const status = getApiResponseStatus(payload);

  const message =
    details?.Message ||
    details?.message ||
    payload?.Message ||
    payload?.message ||
    (status === COMMON_CONSTANTS.SUCCESS
      ? "Password Changed Successfully !!!"
      : "Unable to change the password.");

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "ChangePasswordError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    details,
    message,
  };
}

/**
 * Logs out the currently authenticated TimePro user.
 *
 * This is a protected API call, so the shared Axios interceptor attaches
 * the current access token and refreshes it automatically when required.
 * Common API ValidationErrors are handled globally by axiosSetup.js through
 * handleApiValidationErrors(). Business Success/Error responses are handled
 * here because the API can return HTTP 200 with Status = "Error".
 */
export async function logoutUser({ email }) {
  const response = await apiClient.post(auth.logout, {
    emailID: normalizeString(email),
  });

  const payload = response?.data || {};
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.USER_LOGOUT,
  );
  const status = getApiResponseStatus(payload);

  const message =
    details?.Message ||
    details?.message ||
    payload?.Message ||
    payload?.message ||
    (status === COMMON_CONSTANTS.SUCCESS
      ? "You Have Successfully Logged Out !!!"
      : "Unable to logout.");

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "LogoutError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    details,
    message,
  };
}
