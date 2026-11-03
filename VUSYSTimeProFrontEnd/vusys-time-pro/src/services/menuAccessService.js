import apiClient from "./apiClient";
import { master } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseFirstRow,
  getApiResponseRows,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import { normalizeNumber } from "../utils/valueUtils";
import {
  normalizeUserMenuAccess,
  normalizeUserActionAccess,
  normalizeRoleMenuAccess,
  saveUserMenuAccess,
  saveUserActionAccess,
} from "../utils/menuUtils";

// ============================================================
// GET USER MENU ACCESS
// ============================================================

/**
 * Loads paramType=Menu access.
 *
 * This response is the source of truth only for:
 *
 * - Sidebar Menu
 * - Global Search
 * - ProtectedRoute
 */
export async function getUserMenuAccess({
  userId,
  roleID,
  securityAccessIds = [],
}) {
  const response = await apiClient.post(master.get_user_menu_access, {
    userId: normalizeNumber(userId),

    roleID: normalizeNumber(roleID),

    paramType: "Menu",
  });

  const payload = response?.data || {};

  const status = getApiResponseStatus(payload);

  const rows = getApiResponseRows(payload, API_RESPONSE_TABLE.USER_MENU_ACCESS);

  const menuItems =
    status === COMMON_CONSTANTS.SUCCESS
      ? normalizeUserMenuAccess(rows, securityAccessIds)
      : [];

  saveUserMenuAccess(menuItems);

  return {
    payload,

    rows,

    menuItems,

    status,
  };
}

// ============================================================
// GET ROLE MENU ACCESS
// ============================================================

/**
 * Loads paramType=RoleMenuAccess for the Role Menu Access
 * TreeView.
 *
 * This request does not modify authenticated user's stored
 * menu access.
 */
export async function getRoleMenuAccess({ userId, roleID }) {
  const response = await apiClient.post(master.get_user_menu_access, {
    userId: normalizeNumber(userId),

    roleID: normalizeNumber(roleID),

    paramType: "RoleMenuAccess",
  });

  const payload = response?.data || {};

  const status = getApiResponseStatus(payload);

  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.USER_ROLE_MENU_ACCESS,
  );

  const menuItems =
    status === COMMON_CONSTANTS.SUCCESS ? normalizeRoleMenuAccess(rows) : [];

  return {
    payload,

    rows,

    menuItems,

    status,
  };
}

// ============================================================
// SAVE / UPDATE ROLE MENU ACCESS
// ============================================================

export async function saveUpdateRoleMenuAccessDetails(items = []) {
  const requestItems = (Array.isArray(items) ? items : []).map((item) => ({
    isDeleted: Boolean(item?.isDeleted),

    createdBy: normalizeNumber(item?.createdBy),

    updatedBy: normalizeNumber(item?.updatedBy),

    roleMenuAccessFrontEndId: normalizeNumber(item?.roleMenuAccessFrontEndId),

    roleID: normalizeNumber(item?.roleID),

    securityID: normalizeNumber(item?.securityID),
  }));

  const response = await apiClient.post(
    master.save_update_role_menu_access_details,
    requestItems,
  );

  const payload = response?.data || {};

  const status = getApiResponseStatus(payload);

  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.ROLE_MENU_ACCESS_INSERT,
  );

  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.ROLE_MENU_ACCESS_INSERT,
  );

  const message =
    details?.Message ||
    details?.message ||
    payload?.Message ||
    payload?.message ||
    (status === COMMON_CONSTANTS.SUCCESS
      ? "Role Menu Access Details Saved Successfully."
      : "Unable to update Role Menu Access details.");

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);

    error.name = "RoleMenuAccessUpdateError";

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

// ============================================================
// GET USER ACTION ACCESS
// ============================================================

/**
 * Loads paramType=Action access.
 *
 * This response is the source of truth only for:
 *
 * - Create
 * - Edit
 * - View
 * - Other action permissions
 */
export async function getUserActionAccess({
  userId,
  roleID,
  securityAccessIds = [],
}) {
  const response = await apiClient.post(master.get_user_menu_access, {
    userId: normalizeNumber(userId),

    roleID: normalizeNumber(roleID),

    paramType: "Action",
  });

  const payload = response?.data || {};

  const status = getApiResponseStatus(payload);

  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.USER_ACTION_ACCESS,
  );

  const actionAccessItems =
    status === COMMON_CONSTANTS.SUCCESS
      ? normalizeUserActionAccess(rows, securityAccessIds)
      : [];

  saveUserActionAccess(actionAccessItems);

  return {
    payload,

    rows,

    actionAccessItems,

    status,
  };
}
