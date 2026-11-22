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
// STATE NORMALIZATION
// ============================================================

const normalizeStateRow = (row = {}) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  stateId: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  stateCode: normalizeString(row?.StateCode ?? row?.stateCode),
  stateName: normalizeString(row?.StateName ?? row?.stateName),
  countryId: String(
    normalizeNumber(
      row?.CountryID ?? row?.CountryId ?? row?.countryID ?? row?.countryId,
    ),
  ),
  countryName: normalizeString(row?.CountryName ?? row?.countryName),
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
// GET STATE LIST
// ============================================================

export async function getStateList() {
  const response = await apiClient.post(master.get_state_list, { id: 0 });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.STATE_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load State List."),
    );
    error.name = "StateListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    states: rows.map(normalizeStateRow).filter((item) => item.stateId > 0),
  };
}

// ============================================================
// GET STATE DETAILS
// ============================================================

export async function getStateDetails(stateId) {
  const response = await apiClient.post(master.get_state_details, {
    id: normalizeNumber(stateId),
  });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.STATE_DETAILS_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details?.Id) {
    const error = new Error(
      getMessage(payload, details, "State Details Not Found"),
    );
    error.name = "StateDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    state: normalizeStateRow(details),
  };
}

// ============================================================
// SAVE / UPDATE / DELETE STATE
// ============================================================

export async function saveAndUpdateStateDetails({
  id = 0,
  stateCode = "",
  stateName = "",
  countryID = 0,
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(master.save_and_update_state_details, {
    id: normalizeNumber(id),
    stateCode: normalizeString(stateCode),
    stateName: normalizeString(stateName),
    countryID: normalizeNumber(countryID),
    isDeleted: Boolean(isDeleted),
    createdBy: normalizeNumber(createdBy),
    updatedBy: normalizeNumber(updatedBy),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.STATE_DETAILS_RESPONSE,
  );
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.STATE_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "State Details Saved Successfully."
      : "State Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "StateSaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    details,
    stateId: normalizeNumber(details?.Id ?? details?.id),
    message,
  };
}
