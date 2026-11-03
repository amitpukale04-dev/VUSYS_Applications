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
// COUNTRY NORMALIZATION
// ============================================================

const normalizeCountryRow = (row = {}) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  countryId: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  countryCode: normalizeString(row?.CountryCode ?? row?.countryCode),
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
// GET COUNTRY LIST
// ============================================================

export async function getCountryList() {
  const response = await apiClient.post(master.get_country_list, { id: 0 });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.COUNTRY_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load Country List."),
    );
    error.name = "CountryListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    countries: rows
      .map(normalizeCountryRow)
      .filter((item) => item.countryId > 0),
  };
}

// ============================================================
// GET COUNTRY DETAILS
// ============================================================

export async function getCountryDetails(countryId) {
  const response = await apiClient.post(master.get_country_details, {
    id: normalizeNumber(countryId),
  });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.COUNTRY_DETAILS_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details?.Id) {
    const error = new Error(
      getMessage(payload, details, "Country Details Not Found"),
    );
    error.name = "CountryDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    country: normalizeCountryRow(details),
  };
}

// ============================================================
// SAVE / UPDATE / DELETE COUNTRY
// ============================================================

export async function saveAndUpdateCountryDetails({
  id = 0,
  countryCode = "",
  countryName = "",
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(
    master.save_and_update_country_details,
    {
      id: normalizeNumber(id),
      countryCode: normalizeString(countryCode),
      countryName: normalizeString(countryName),
      isDeleted: Boolean(isDeleted),
      createdBy: normalizeNumber(createdBy),
      updatedBy: normalizeNumber(updatedBy),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.COUNTRY_DETAILS_RESPONSE,
  );
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.COUNTRY_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Country Details Saved Successfully."
      : "Country Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "CountrySaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    details,
    countryId: normalizeNumber(details?.Id ?? details?.id),
    message,
  };
}
