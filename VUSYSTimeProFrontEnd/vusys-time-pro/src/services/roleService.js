import apiClient from "./apiClient";
import { master } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseFirstRow,
  getApiResponseRows,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import {
  normalizeBoolean,
  normalizeNumber,
  normalizeString,
} from "../utils/valueUtils";

// ============================================================
// ROLE NORMALIZATION
// ============================================================

const normalizeRoleRow = (row = {}) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  roleId: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  roleName: normalizeString(row?.RoleName ?? row?.roleName),
  isDeleted: normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
  activeStatus: normalizeString(row?.ActiveStatus ?? row?.activeStatus),
  isActive: !normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
});

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

// ============================================================
// GET ROLE LIST
// ============================================================

export async function getRoleList() {
  const response = await apiClient.post(master.get_role_list, { id: 0 });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.ROLE_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load Role List."),
    );
    error.name = "RoleListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    roles: rows.map(normalizeRoleRow).filter((item) => item.roleId > 0),
  };
}

// ============================================================
// GET ROLE DETAILS
// ============================================================

export async function getRoleDetails(roleId) {
  const response = await apiClient.post(master.get_role_details, {
    id: normalizeNumber(roleId),
  });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.ROLE_DETAILS_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details?.Id) {
    const error = new Error(
      getMessage(payload, details, "Role Details Not Found"),
    );
    error.name = "RoleDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    role: normalizeRoleRow(details),
  };
}

// ============================================================
// SAVE / UPDATE / DELETE ROLE
// ============================================================

export async function saveAndUpdateRoleDetails({
  id = 0,
  roleName = "",
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(master.save_and_update_role_details, {
    id: normalizeNumber(id),
    roleName: normalizeString(roleName),
    isDeleted: Boolean(isDeleted),
    createdBy: normalizeNumber(createdBy),
    updatedBy: normalizeNumber(updatedBy),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.ROLE_DETAILS_RESPONSE,
  );
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.ROLE_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Role Details Saved Successfully."
      : "Role Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "RoleSaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    details,
    roleId: normalizeNumber(details?.Id ?? details?.id),
    message,
  };
}
