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
// HOLIDAY NORMALIZATION
// ============================================================

const normalizeHolidayRow = (row = {}) => ({
  id: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  holidayName: normalizeString(row?.HolidayName ?? row?.holidayName),
  holidayDate: normalizeString(row?.HolidayDate ?? row?.holidayDate).slice(
    0,
    10,
  ),
  holidayYear: normalizeString(row?.HolidayYear ?? row?.holidayYear),
  holidayDescription: normalizeString(
    row?.HolidayDescription ?? row?.holidayDescription,
  ),
  isOptionalHoliday: normalizeBoolean(
    row?.IsOptionalHoliday ?? row?.isOptionalHoliday,
  ),
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
// GET HOLIDAY LIST
// ============================================================

export async function getHolidayList(holidayYear) {
  const response = await apiClient.post(master.get_holiday_list, {
    id: 0,
    holidayYear: normalizeNumber(holidayYear),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.HOLIDAY_LIST_RESPONSE || "HOLIDAY_LIST_RESPONSE",
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load Holiday List."),
    );
    error.name = "HolidayListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  // Preserve the existing HolidayMaster.jsx contract: return an array.
  return rows.map(normalizeHolidayRow).filter((item) => item.id > 0);
}

// ============================================================
// GET HOLIDAY DETAILS
// ============================================================

export async function getHolidayDetails(id, holidayYear) {
  const response = await apiClient.post(master.get_holiday_details, {
    id: normalizeNumber(id),
    holidayYear: normalizeNumber(holidayYear),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.HOLIDAY_DETAILS_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details?.Id) {
    const error = new Error(
      getMessage(payload, details, "Holiday Details Not Found."),
    );
    error.name = "HolidayDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  // Preserve the existing HolidayMaster.jsx contract: return a holiday object.
  return normalizeHolidayRow(details);
}

// ============================================================
// SAVE / UPDATE / DELETE HOLIDAY
// ============================================================

export async function saveAndUpdateHolidayDetails({
  id = 0,
  holidayName = "",
  holidayDate = "",
  holidayYear = 0,
  holidayDescription = "",
  isOptionalHoliday = false,
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(
    master.save_and_update_holiday_details,
    {
      id: normalizeNumber(id) > 0 ? normalizeNumber(id) : 0,
      holidayName: normalizeString(holidayName).trim(),
      holidayDate: normalizeString(holidayDate).slice(0, 10),
      holidayYear: normalizeNumber(holidayYear),
      holidayDescription: normalizeString(holidayDescription).trim(),
      isOptionalHoliday: normalizeBoolean(isOptionalHoliday),
      isDeleted: normalizeBoolean(isDeleted),
      createdBy: normalizeNumber(createdBy),
      updatedBy: normalizeNumber(updatedBy),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.HOLIDAY_DETAILS_RESPONSE,
  );
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.HOLIDAY_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Holiday Details Saved Successfully."
      : "Holiday Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "HolidaySaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    details,
    id: normalizeNumber(details?.Id ?? details?.id),
    message,
  };
}
