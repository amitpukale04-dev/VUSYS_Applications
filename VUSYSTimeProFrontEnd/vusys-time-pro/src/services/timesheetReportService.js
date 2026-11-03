import apiClient from "./apiClient";
import { report, timesheet } from "../utils/url";
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

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

const normalizeTimesheetReportRow = (row = {}) => ({
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

export async function getTimesheetReportList({
  employeeCode = "0",
  timesheetMonth = "",
  timesheetYear = 0,
}) {
  const response = await apiClient.post(report.get_timesheet_report_list, {
    employeeCode: normalizeString(employeeCode),
    timesheetMonth: normalizeString(timesheetMonth),
    timesheetYear: normalizeNumber(timesheetYear),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_REPORT_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Timesheet Report Not Found."),
    );
    error.name = "TimesheetReportError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    reportRows: rows
      .map(normalizeTimesheetReportRow)
      .filter((item) => item.employeeCode),
  };
}

export async function sendTimesheetSubmissionReminderNotification({
  employeeCode = "",
  timesheetMonth = "",
  timesheetYear = 0,
}) {
  const response = await apiClient.post(
    timesheet.send_timesheet_submission_reminder_notification_to_employee,
    {
      employeeCode: normalizeString(employeeCode),
      timesheetMonth: normalizeString(timesheetMonth),
      timesheetYear: normalizeNumber(timesheetYear),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_SUBMISSION_REMINDER_NOTIFICATION_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    "Unable to send Timesheet Submission Notification.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(message);
    error.name = "TimesheetSubmissionReminderNotificationError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return { payload, status, details, message };
}
