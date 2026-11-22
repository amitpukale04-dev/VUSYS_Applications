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
// SECURITY MASTER NORMALIZATION
// ============================================================

const normalizeSecurityRow = (row = {}) => ({
  securityId: normalizeNumber(
    row?.SecurityId ?? row?.SecurityID ?? row?.securityId ?? row?.securityID,
  ),
  securityName: normalizeString(row?.SecurityName ?? row?.securityName),
  roleId: String(
    normalizeNumber(row?.RoleID ?? row?.RoleId ?? row?.roleID ?? row?.roleId),
  ),
  screenName: normalizeString(row?.ScreenName ?? row?.screenName),
  isDefault: normalizeBoolean(row?.IsDefault ?? row?.isDefault),
  isDeleted: normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
  isActive: !normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
  createdBy: normalizeNumber(row?.CreatedBy ?? row?.createdBy),
  createdDate: normalizeString(row?.CreatedDate ?? row?.createdDate),
  updatedBy: normalizeNumber(row?.UpdatedBy ?? row?.updatedBy),
  updatedDate: normalizeString(row?.UpdatedDate ?? row?.updatedDate),
});

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

// ============================================================
// GET SECURITY LIST
// ============================================================

export async function getSecurityList() {
  const response = await apiClient.post(master.get_security_list, {
    securityId: 0,
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.SECURITY_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load Security Master List."),
    );
    error.name = "SecurityMasterListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    securities: rows
      .map(normalizeSecurityRow)
      .filter((item) => item.securityId > 0),
  };
}

// ============================================================
// GET SECURITY DETAILS
// ============================================================

export async function getSecurityDetails(securityId) {
  const response = await apiClient.post(master.get_security_details, {
    securityId: normalizeNumber(securityId),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.SECURITY_DETAILS_RESPONSE,
  );

  const normalizedSecurityId = normalizeNumber(
    details?.SecurityId ?? details?.SecurityID ?? details?.securityId,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || normalizedSecurityId <= 0) {
    const error = new Error(
      getMessage(payload, details, "Security Master Details Not Found"),
    );
    error.name = "SecurityMasterDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    security: normalizeSecurityRow(details),
  };
}

// ============================================================
// SAVE / UPDATE / DELETE SECURITY
// ============================================================

export async function saveUpdateSecurityDetails({
  securityId = 0,
  securityName = "",
  roleID = 0,
  screenName = "",
  isDefault = false,
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(master.save_update_security_details, {
    securityId: normalizeNumber(securityId),
    securityName: normalizeString(securityName),
    roleID: normalizeNumber(roleID),
    screenName: normalizeString(screenName),
    isDefault: Boolean(isDefault),
    isDeleted: Boolean(isDeleted),
    createdBy: normalizeNumber(createdBy),
    updatedBy: normalizeNumber(updatedBy),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.SECURITY_MASTER_RESPONSE,
  );
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.SECURITY_MASTER_RESPONSE,
  );

  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Security Master Details Saved Successfully."
      : "Security Master Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "SecurityMasterSaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    details,
    securityId: normalizeNumber(details?.Id ?? details?.id),
    message,
  };
}
