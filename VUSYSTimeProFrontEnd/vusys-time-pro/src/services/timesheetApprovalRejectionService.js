import apiClient from "./apiClient";
import { timesheet } from "../utils/url";
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

const getMessage = (payload, details, fallback) => {
  const message =
    details?.Message ||
    details?.message ||
    payload?.Message ||
    payload?.message ||
    "";

  const normalizedMessage = normalizeString(message).trim();

  return normalizedMessage && normalizedMessage.toLowerCase() !== "ok"
    ? normalizedMessage
    : fallback;
};

const createInlineValidationError = (payload, validationErrors) => {
  const error = new Error("API validation failed.");
  error.name = "ApiValidationError";
  error.isApiValidationError = true;
  error.validationErrors = validationErrors;
  error.payload = payload;
  return error;
};

const normalizeApprovalRejectionRow = (row = {}, index = 0) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo) || index + 1,
  projectTimeSheetID: normalizeNumber(
    row?.ProjectTimeSheetID ?? row?.projectTimeSheetID,
  ),
  employeeCode: normalizeString(row?.EmployeeCode ?? row?.employeeCode),
  fullName: normalizeString(row?.FullName ?? row?.fullName),
  timeSheetMonth: normalizeString(row?.TimeSheetMonth ?? row?.timeSheetMonth),
  timeSheetYear: normalizeNumber(row?.TimeSheetYear ?? row?.timeSheetYear),
  totalHoursWorked: normalizeNumber(
    row?.TotalHoursWorked ?? row?.totalHoursWorked,
  ),
  timesheetApprovalStatus: normalizeString(
    row?.TimesheetApprovalStatus ?? row?.timesheetApprovalStatus,
  ),
  approvedOrRejectedReason: normalizeString(
    row?.ApprovedOrRejectedReason ?? row?.approvedOrRejectedReason,
  ),
  approvedOrRejectedByUser: normalizeString(
    row?.ApprovedOrRejectedByUser ?? row?.approvedOrRejectedByUser,
  ),
  approvedOrRejectedDateTime: normalizeString(
    row?.ApprovedOrRejectedDateTime ?? row?.approvedOrRejectedDateTime,
  ),
  isShowSendForApprovalButton: normalizeBoolean(
    row?.IsShowSendForApprovalButton ?? row?.isShowSendForApprovalButton,
  ),
  isShowSendNotificationButton: normalizeBoolean(
    row?.IsShowSendNotificationButton ?? row?.isShowSendNotificationButton,
  ),
  isSentForApproval: normalizeBoolean(
    row?.IsSentForApproval ?? row?.isSentForApproval,
  ),
  isAvailableToResendForApproval: normalizeBoolean(
    row?.IsAvailableToResendForApproval ?? row?.isAvailableToResendForApproval,
  ),
  isShowViewDetailsButton: normalizeBoolean(
    row?.IsShowViewDetailsButton ?? row?.isShowViewDetailsButton,
  ),
  isShowApproveRejectButton: normalizeBoolean(
    row?.IsShowApproveRejectButton ?? row?.isShowApproveRejectButton,
  ),
});

const getInlineValidationRows = (payload) => {
  const approvalRows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_APPROVAL_REJECTION_LIST_RESPONSE,
  );
  const submissionRows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_SUBMISSION_RESPONSE,
  );

  return [...approvalRows, ...submissionRows].filter(
    (row) =>
      normalizeString(row?.Key ?? row?.key).trim() &&
      normalizeString(row?.Message ?? row?.message).trim(),
  );
};

export async function getTimesheetApprovalRejectionList({
  employeeCode = "0",
  timesheetMonth = "",
  timesheetYear = 0,
  paramType = "GetAllEmployeeProjectTimesheetForApproval",
} = {}) {
  const response = await apiClient.post(timesheet.get_timesheet_submission_list, {
    employeeCode: normalizeString(employeeCode),
    timesheetMonth: normalizeString(timesheetMonth),
    timesheetYear: normalizeNumber(timesheetYear),
    paramType: normalizeString(paramType),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_APPROVAL_REJECTION_LIST_RESPONSE,
  );

  const inlineValidationErrors = getInlineValidationRows(payload);

  if (inlineValidationErrors.length) {
    throw createInlineValidationError(payload, inlineValidationErrors);
  }

  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Timesheet Approval List Not Found."),
    );
    error.name = "TimesheetApprovalRejectionListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    approvalRows: rows
      .map(normalizeApprovalRejectionRow)
      .filter((item) => item.projectTimeSheetID > 0),
  };
}

export async function checkTimesheetApprovalStatus({
  employeeCode = "",
  timesheetMonth = "",
  timesheetYear = 0,
} = {}) {
  const response = await apiClient.post(
    timesheet.check_timesheet_approval_status,
    {
      employeeCode: normalizeString(employeeCode),
      timesheetMonth: normalizeString(timesheetMonth),
      timesheetYear: normalizeNumber(timesheetYear),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const validationErrors = Array.isArray(payload?.data?.ValidationErrors)
    ? payload.data.ValidationErrors
    : Array.isArray(payload?.ValidationErrors)
      ? payload.ValidationErrors
      : [];

  if (validationErrors.length) {
    throw createInlineValidationError(payload, validationErrors);
  }

  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_APPROVAL_STATUS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    "Unable to verify Timesheet Approval Status.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(message);
    error.name = "TimesheetApprovalStatusError";
    error.isTimesheetApprovalStatusError = true;
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    message,
  };
}

export async function approveOrRejectTimesheet({
  employeeCode = "",
  timesheetMonth = "",
  timesheetYear = 0,
  projectTimeSheetID = 0,
  remarks = "",
  userToken = "",
  source = "",
  approveStatus = "",
} = {}) {
  const response = await apiClient.post(timesheet.approve_or_reject_timesheet, {
    employeeCode: normalizeString(employeeCode),
    timesheetMonth: normalizeString(timesheetMonth),
    timesheetYear: normalizeNumber(timesheetYear),
    projectTimeSheetID: normalizeNumber(projectTimeSheetID),
    remarks: normalizeString(remarks),
    userToken: normalizeString(userToken),
    source: normalizeString(source),
    approveStatus: normalizeString(approveStatus),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_APPROVE_REJECT_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    "Unable to Approve/Reject Timesheet.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(message);
    error.name = "TimesheetApproveRejectError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    message,
  };
}
