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
import { toISODateOnly } from "../utils/dateUtils";

const getMessage = (payload, details, fallback) => {
  const message =
    details?.Message ||
    details?.message ||
    payload?.Message ||
    payload?.message ||
    "";
  const normalized = normalizeString(message);
  return normalized && normalized.toLowerCase() !== "ok" ? normalized : fallback;
};

const getValidationErrors = (payload) =>
  Array.isArray(payload?.data?.ValidationErrors)
    ? payload.data.ValidationErrors
    : Array.isArray(payload?.ValidationErrors)
      ? payload.ValidationErrors
      : [];

const throwValidation = (payload) => {
  const validationErrors = getValidationErrors(payload);
  if (!validationErrors.length) return;
  const error = new Error("API validation failed.");
  error.name = "ApiValidationError";
  error.isApiValidationError = true;
  error.validationErrors = validationErrors;
  error.payload = payload;
  throw error;
};

const normalizeLogRow = (row = {}, index = 0) => {
  const slNo = normalizeNumber(row?.SLNo ?? row?.slNo) || index + 1;
  const projectTimeSheetID = normalizeNumber(
    row?.ProjectTimeSheetID ?? row?.projectTimeSheetID,
  );
  const taskDate = toISODateOnly(row?.TaskDate ?? row?.taskDate);

  return {
  listKey: `${projectTimeSheetID}-${taskDate}-${slNo}-${index}`,
  slNo,
  projectTimeSheetID,
  employeeCode: normalizeString(row?.EmployeeCode ?? row?.employeeCode),
  timeSheetMonth: normalizeString(row?.TimeSheetMonth ?? row?.timeSheetMonth),
  timeSheetYear: normalizeNumber(row?.TimeSheetYear ?? row?.timeSheetYear),
  isApproved: normalizeBoolean(row?.IsApproved ?? row?.isApproved),
  isSentForApproval: normalizeBoolean(
    row?.IsSentForApproval ?? row?.isSentForApproval,
  ),
  cannotEditAfterApproveMessage: normalizeString(
    row?.CannotEditAfterApproveMessage ?? row?.cannotEditAfterApproveMessage,
  ),
  cannotEditAfterSentForApprovalMessage: normalizeString(
    row?.CannotEditAfterSentForApprovalMessage ??
      row?.cannotEditAfterSentForApprovalMessage,
  ),
  fullName: normalizeString(row?.FullName ?? row?.fullName),
  emailID: normalizeString(row?.EmailID ?? row?.emailID),
  taskDate,
  displayTaskDate: normalizeString(
    row?.DisplayTaskDate ?? row?.displayTaskDate ?? row?.TaskDate ?? "",
  ),
  projectStatus: normalizeString(row?.ProjectStatus ?? row?.projectStatus),
  clientName: normalizeString(row?.ClientName ?? row?.clientName),
  projectName: normalizeString(row?.ProjectName ?? row?.projectName),
  taskDescription: normalizeString(
    row?.TaskDescription ?? row?.taskDescription,
  ),
  activityName: normalizeString(row?.ActivityName ?? row?.activityName),
  totalHoursWorked: normalizeNumber(
    row?.TotalHoursWorked ?? row?.totalHoursWorked,
  ),
  };
};

const normalizeDetailRow = (row = {}, index = 0) => ({
  localKey:
    normalizeNumber(row?.ProjectTimeSheetDetailID ?? row?.projectTimeSheetDetailID) ||
    -(Date.now() + index),
  projectTimeSheetID: normalizeNumber(
    row?.ProjectTimeSheetID ?? row?.projectTimeSheetID,
  ),
  employeeCode: normalizeString(row?.EmployeeCode ?? row?.employeeCode),
  projectTimeSheetDetailID: normalizeNumber(
    row?.ProjectTimeSheetDetailID ?? row?.projectTimeSheetDetailID,
  ),
  projectID: normalizeNumber(row?.ProjectID ?? row?.projectID),
  projectName: normalizeString(row?.ProjectName ?? row?.projectName),
  projectActivityID: normalizeNumber(
    row?.ProjectActivityID ?? row?.projectActivityID,
  ),
  activityName: normalizeString(row?.ActivityName ?? row?.activityName),
  taskDate: toISODateOnly(row?.TaskDate ?? row?.taskDate),
  taskDescription: normalizeString(
    row?.TaskDescription ?? row?.taskDescription,
  ),
  taskDescriptionRaw: normalizeString(
    row?.TaskDescriptionRaw ?? row?.taskDescriptionRaw,
  ),
  noofHoursWorked: normalizeString(
    row?.NoofHoursWorked ?? row?.noofHoursWorked,
  ),
  isOnLeave: normalizeBoolean(row?.IsOnLeave ?? row?.isOnLeave),
  isSentForApproval: normalizeBoolean(
    row?.IsSentForApproval ?? row?.isSentForApproval,
  ),
  isDeleted: false,
});

export async function getLogTimesheetList({
  projectTimeSheetID = 0,
  employeeCode = "",
  fromDate = "",
  toDate = "",
} = {}) {
  const response = await apiClient.post(timesheet.get_log_timesheet_list, {
    projectTimeSheetID: normalizeNumber(projectTimeSheetID),
    employeeCode: normalizeString(employeeCode),
    fromDate: toISODateOnly(fromDate),
    toDate: toISODateOnly(toDate),
  });

  const payload = response?.data || {};
  throwValidation(payload);

  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.LOG_TIMESHEET_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;
  const allowAddRows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.ALLOW_ADD_TIMESHEET_RESPONSE,
  );
  const allowAddDetails = allowAddRows?.[0] || null;
  const allowToAddTimeSheet =
    normalizeNumber(
      allowAddDetails?.AllowToAddTimeSheet ??
        allowAddDetails?.allowToAddTimeSheet ??
        1,
    ) !== 0;
  const allowToAddTimeSheetMessage = normalizeString(
    allowAddDetails?.AllowToAddTimeSheetMessage ??
      allowAddDetails?.allowToAddTimeSheetMessage ??
      "",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(
        payload,
        details,
        "Timesheet Not Found For The Selected From Date and To Date.",
      ),
    );
    error.name = "LogTimesheetListError";
    error.payload = payload;
    throw error;
  }

  return {
    payload,
    rows,
    logRows: rows
      .map(normalizeLogRow)
      .filter((item) => item.projectTimeSheetID > 0),
    allowToAddTimeSheet,
    allowToAddTimeSheetMessage,
  };
}

export async function getTimesheetDetails({
  projectTimeSheetID = 0,
  employeeCode = "",
  taskDate = "",
  timesheetMonth = "",
  timesheetYear = 0,
} = {}) {
  const response = await apiClient.post(timesheet.get_timesheet_details, {
    projectTimeSheetID: normalizeNumber(projectTimeSheetID),
    employeeCode: normalizeString(employeeCode),
    taskDate: toISODateOnly(taskDate),
    timesheetMonth: normalizeString(timesheetMonth),
    timesheetYear: normalizeNumber(timesheetYear),
  });

  const payload = response?.data || {};
  throwValidation(payload);

  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_DETAILS_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Timesheet Details Not Found."),
    );
    error.name = "TimesheetDetailsError";
    error.payload = payload;
    throw error;
  }

  return {
    payload,
    rows,
    detailRows: rows
      .map(normalizeDetailRow)
      .filter((item) => item.projectTimeSheetDetailID > 0),
  };
}

export async function saveAndUpdateTimesheetDetails(rows = []) {
  const request = (Array.isArray(rows) ? rows : []).map((row) => ({
    projectTimeSheetID: normalizeNumber(row?.projectTimeSheetID),
    projectTimeSheetDetailID: normalizeNumber(row?.projectTimeSheetDetailID),
    employeeCode: normalizeString(row?.employeeCode),
    projectID: normalizeNumber(row?.projectID),
    projectActivityID: normalizeNumber(row?.projectActivityID),
    taskDate: toISODateOnly(row?.taskDate),
    taskDescription: normalizeString(row?.taskDescription),
    taskDescriptionRaw: normalizeString(row?.taskDescriptionRaw),
    noofHoursWorked: normalizeString(row?.noofHoursWorked),
    isOnLeave: normalizeBoolean(row?.isOnLeave),
    isDeleted: normalizeBoolean(row?.isDeleted),
    createdBy: normalizeNumber(row?.createdBy),
    updatedBy: normalizeNumber(row?.updatedBy),
  }));

  const response = await apiClient.post(
    timesheet.save_and_update_timesheet_details,
    request,
  );

  const payload = response?.data || {};
  throwValidation(payload);

  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_SAVE_RESPONSE,
  );
  const message = getMessage(payload, details, "Unable To Save Your Timesheet.");

  if (
    status !== COMMON_CONSTANTS.SUCCESS ||
    normalizeString(message).toLowerCase().startsWith("failed")
  ) {
    const error = new Error(message);
    error.name = "TimesheetSaveError";
    error.payload = payload;
    throw error;
  }

  return { payload, details, message };
}
