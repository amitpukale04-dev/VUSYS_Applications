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
  toUpperCase,
} from "../utils/valueUtils";

// ============================================================
// LEAVE TYPE NORMALIZATION
// ============================================================

const normalizeLeaveTypeRow = (row = {}) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  leaveTypeId: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  leaveTypeCode: normalizeString(row?.LeaveTypeCode ?? row?.leaveTypeCode),
  leaveType: normalizeString(row?.LeaveType ?? row?.leaveType),
  leaveTypeDescription: normalizeString(
    row?.LeaveTypeDescription ?? row?.leaveTypeDescription,
  ),
  defaultLeaveCount: normalizeNumber(
    row?.DefaultLeaveCount ?? row?.defaultLeaveCount,
  ),
  isPaidLeave: normalizeBoolean(row?.IsPaidLeave ?? row?.isPaidLeave),
  allowHalfDay: normalizeBoolean(row?.AllowHalfDay ?? row?.allowHalfDay),
  isDeleted: normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
  activeStatus: normalizeString(row?.ActiveStatus ?? row?.activeStatus),
});

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

// ============================================================
// GET LEAVE TYPE LIST
// ============================================================

export async function getLeaveTypeList() {
  const response = await apiClient.post(master.get_leave_type_list, { id: 0 });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.LEAVE_TYPE_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load Leave Type List."),
    );
    error.name = "LeaveTypeListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    leaveTypes: rows
      .map(normalizeLeaveTypeRow)
      .filter((item) => item.leaveTypeId > 0),
  };
}

// ============================================================
// GET LEAVE TYPE DETAILS
// ============================================================

export async function getLeaveTypeDetails(leaveTypeId) {
  const response = await apiClient.post(master.get_leave_type_details, {
    id: normalizeNumber(leaveTypeId),
  });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.LEAVE_TYPE_DETAILS_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details?.Id) {
    const error = new Error(
      getMessage(payload, details, "Leave Type Details Not Found"),
    );
    error.name = "LeaveTypeDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    leaveType: normalizeLeaveTypeRow(details),
  };
}

// ============================================================
// SAVE / UPDATE / DELETE LEAVE TYPE
// ============================================================

export async function saveAndUpdateLeaveTypeDetails({
  id = 0,
  leaveTypeCode = "",
  leaveType = "",
  leaveTypeDescription = "",
  defaultLeaveCount = 0,
  isPaidLeave = false,
  allowHalfDay = false,
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(
    master.save_and_update_leave_type_details,
    {
      id: normalizeNumber(id),
      leaveTypeCode: toUpperCase(normalizeString(leaveTypeCode)),
      leaveType: normalizeString(leaveType),
      leaveTypeDescription: normalizeString(leaveTypeDescription),
      defaultLeaveCount: normalizeNumber(defaultLeaveCount),
      isPaidLeave: normalizeBoolean(isPaidLeave),
      allowHalfDay: normalizeBoolean(allowHalfDay),
      isDeleted: normalizeBoolean(isDeleted),
      createdBy: normalizeNumber(createdBy),
      updatedBy: normalizeNumber(updatedBy),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.LEAVE_TYPE_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Leave Type Details Saved Successfully."
      : "Leave Type Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "LeaveTypeSaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    id: normalizeNumber(details?.Id ?? details?.id),
    message,
  };
}

export async function deleteLeaveTypeMaster(leaveTypeId, updatedBy = 0) {
  const { leaveType } = await getLeaveTypeDetails(leaveTypeId);

  return saveAndUpdateLeaveTypeDetails({
    ...leaveType,
    id: leaveType.leaveTypeId,
    isDeleted: true,
    createdBy: 0,
    updatedBy,
  });
}
