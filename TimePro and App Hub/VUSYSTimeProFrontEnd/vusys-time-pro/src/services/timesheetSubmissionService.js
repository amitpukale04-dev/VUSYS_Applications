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

const normalizeTimesheetSubmissionRow = (row = {}) => ({
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

export async function getTimesheetSubmissionList({
  employeeCode = "",
  timesheetMonth = "",
  timesheetYear = 0,
  paramType = "GetEmployeeProjectTimesheetForSubmission",
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
    API_RESPONSE_TABLE.TIMESHEET_SUBMISSION_RESPONSE,
  );

  const inlineValidationErrors = rows.filter(
    (row) =>
      normalizeString(row?.Key ?? row?.key).trim() &&
      normalizeString(row?.Message ?? row?.message).trim(),
  );

  if (inlineValidationErrors.length) {
    throw createInlineValidationError(payload, inlineValidationErrors);
  }

  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Timesheet Submission Not Found."),
    );
    error.name = "TimesheetSubmissionError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    submissionRows: rows
      .map(normalizeTimesheetSubmissionRow)
      .filter((item) => item.projectTimeSheetID > 0),
  };
}

export async function submitTimesheetDetailsForApproval({
  employeeCode = "",
  timesheetMonth = "",
  timesheetYear = 0,
  projectTimeSheetID = 0,
  approveStatus = "",
} = {}) {
  const response = await apiClient.post(
    timesheet.submit_timesheet_details_for_approval,
    {
      employeeCode: normalizeString(employeeCode),
      timesheetMonth: normalizeString(timesheetMonth),
      timesheetYear: normalizeNumber(timesheetYear),
      projectTimeSheetID: normalizeNumber(projectTimeSheetID),
      approveStatus: normalizeString(approveStatus),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_SUBMITTED_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    "Unable to submit Timesheet for approval.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(message);
    error.name = "TimesheetSubmitForApprovalError";
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
