import apiClient from "./apiClient";
import { master } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseFirstRow,
  getApiResponseRows,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import { normalizeNumber } from "../utils/valueUtils";

// ============================================================
// GET USER SECURITY ACCESS LIST
// ============================================================

/**
 * Loads Security Access details for the selected User + Role.
 *
 * USER_SECURITY_ACCESS_SELECTED_LIST
 *   -> permissions currently applied to the selected User.
 *
 * USER_SECURITY_ACCESS_LIST
 *   -> complete list of permissions available for the Role.
 *
 * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are handled
 * globally by axiosSetup.js.
 */
export async function getUserSecurityAccessList({ userID, roleID }) {
  const response = await apiClient.post(master.get_user_security_access_list, {
    isDeleted: false,
    createdBy: 0,
    updatedBy: 0,
    securityAccessId: 0,
    securityID: 0,
    roleID: normalizeNumber(roleID),
    userID: normalizeNumber(userID),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);

  const selectedRows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.USER_SECURITY_ACCESS_SELECTED_LIST,
  );

  const securityRows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.USER_SECURITY_ACCESS_LIST,
  );

  return {
    payload,
    status,
    selectedRows:
      status === COMMON_CONSTANTS.SUCCESS && Array.isArray(selectedRows)
        ? selectedRows
        : [],
    securityRows:
      status === COMMON_CONSTANTS.SUCCESS && Array.isArray(securityRows)
        ? securityRows
        : [],
  };
}

// ============================================================
// SAVE / UPDATE USER SECURITY ACCESS
// ============================================================

/**
 * Saves the complete Rules Applied collection for the selected User + Role.
 *
 * API ValidationErrors, Network/CORS and GLOBAL_ERROR_RESPONSE are handled
 * globally by axiosSetup.js. HTTP-200 business errors are converted to a
 * rejected Promise here so the screen can display the returned message once.
 */
export async function saveUpdateUserSecurityAccessDetails(items = []) {
  const requestItems = (Array.isArray(items) ? items : []).map((item) => ({
    isDeleted: Boolean(item?.isDeleted),
    createdBy: normalizeNumber(item?.createdBy),
    updatedBy: normalizeNumber(item?.updatedBy),
    securityAccessId: normalizeNumber(item?.securityAccessId),
    securityID: normalizeNumber(item?.securityID),
    roleID: normalizeNumber(item?.roleID),
    userID: normalizeNumber(item?.userID),
  }));

  const response = await apiClient.post(
    master.save_update_user_security_access_details,
    requestItems,
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.USER_SECURITY_ACCESS_RESPONSE,
  );
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.USER_SECURITY_ACCESS_RESPONSE,
  );

  const message =
    details?.Message ||
    details?.message ||
    payload?.Message ||
    payload?.message ||
    (status === COMMON_CONSTANTS.SUCCESS
      ? "Security Access Details Saved Successfully."
      : "Security Access Details Not Saved.");

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "UserSecurityAccessUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    rows,
    details,
    status,
    message,
  };
}
