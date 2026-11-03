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
// GENDER NORMALIZATION
// ============================================================

const normalizeGenderRow = (row = {}) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  genderId: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  gender: normalizeString(row?.Gender ?? row?.gender),
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
// GET GENDER LIST
// ============================================================

export async function getGenderList() {
  const response = await apiClient.post(master.get_gender_list, { id: 0 });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.GENDER_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load Gender List."),
    );
    error.name = "GenderListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    genders: rows.map(normalizeGenderRow).filter((item) => item.genderId > 0),
  };
}

// ============================================================
// GET GENDER DETAILS
// ============================================================

export async function getGenderDetails(genderId) {
  const response = await apiClient.post(master.get_gender_details, {
    id: normalizeNumber(genderId),
  });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.GENDER_DETAILS_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details?.Id) {
    const error = new Error(
      getMessage(payload, details, "Gender Details Not Found"),
    );
    error.name = "GenderDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    gender: normalizeGenderRow(details),
  };
}

// ============================================================
// SAVE / UPDATE / DELETE GENDER
// ============================================================

export async function saveAndUpdateGenderDetails({
  id = 0,
  gender = "",
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(master.save_and_update_gender_details, {
    id: normalizeNumber(id),
    gender: normalizeString(gender),
    isDeleted: Boolean(isDeleted),
    createdBy: normalizeNumber(createdBy),
    updatedBy: normalizeNumber(updatedBy),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.GENDER_DETAILS_RESPONSE,
  );
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.GENDER_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Gender Details Saved Successfully."
      : "Gender Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "GenderSaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    details,
    genderId: normalizeNumber(details?.Id ?? details?.id),
    message,
  };
}
