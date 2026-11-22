import apiClient from "./apiClient";
import { timesheet } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseFirstRow,
  getApiResponseRows,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import { normalizeNumber, normalizeString } from "../utils/valueUtils";

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

const stripHtml = (value = "") => {
  const source = normalizeString(value);

  if (!source) return "";

  if (typeof document !== "undefined") {
    const element = document.createElement("div");
    element.innerHTML = source;

    const listItems = Array.from(element.querySelectorAll("li"));

    if (listItems.length) {
      return listItems
        .map((item, index) => {
          const itemText = normalizeString(item.textContent || item.innerText)
            .replace(/\u00a0/g, " ")
            .replace(/\s+/g, " ")
            .trim();

          return itemText ? `${index + 1}. ${itemText}` : "";
        })
        .filter(Boolean)
        .join("\n");
    }

    return normalizeString(element.textContent || element.innerText)
      .replace(/\u00a0/g, " ")
      .replace(/\s+/g, " ")
      .trim();
  }

  const listItemMatches = [...source.matchAll(/<li\b[^>]*>([\s\S]*?)<\/li>/gi)];

  if (listItemMatches.length) {
    return listItemMatches
      .map((match, index) => {
        const itemText = normalizeString(match[1])
          .replace(/<[^>]*>/g, " ")
          .replace(/&nbsp;/gi, " ")
          .replace(/\s+/g, " ")
          .trim();

        return itemText ? `${index + 1}. ${itemText}` : "";
      })
      .filter(Boolean)
      .join("\n");
  }

  return source
    .replace(/<[^>]*>/g, " ")
    .replace(/&nbsp;/gi, " ")
    .replace(/\s+/g, " ")
    .trim();
};

const normalizeTimesheetRow = (row = {}) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  employeeCode: normalizeString(row?.EmployeeCode ?? row?.employeeCode),
  name: normalizeString(row?.Name ?? row?.name),
  taskDate: normalizeString(row?.DisplayTaskDate ?? row?.displayTaskDate),
  clientName: normalizeString(row?.ClientName ?? row?.clientName),
  projectName: normalizeString(row?.ProjectName ?? row?.projectName),
  activityName: normalizeString(row?.ActivityName ?? row?.activityName),
  taskDescription: stripHtml(row?.TaskDescription ?? row?.taskDescription),
  totalHoursWorked: normalizeNumber(
    row?.TotalHoursWorked ?? row?.totalHoursWorked,
  ),
});

const EXPORT_RESPONSE_TABLES = Object.freeze({
  export: API_RESPONSE_TABLE.EMPLOYEE_TIMESHEET_EXPORTED_RESPONSE,
  exportdatewise:
    API_RESPONSE_TABLE.EMPLOYEE_TIMESHEET_DATE_WISE_EXPORTED_RESPONSE,
  exportprojectwise:
    API_RESPONSE_TABLE.EMPLOYEE_TIMESHEET_PROJECT_WISE_EXPORTED_RESPONSE,
});

export async function getOrExportEmployeeTimesheetList({
  employeeCode = "",
  timesheetMonth = "",
  timesheetYear = "",
  projectID = 0,
  projectTimeSheetID = 0,
  action = "View",
} = {}) {
  const response = await apiClient.post(
    timesheet.get_or_export_employee_timesheet_list,
    {
      employeeCode: normalizeString(employeeCode),
      timesheetMonth: normalizeString(timesheetMonth),
      timesheetYear: normalizeNumber(timesheetYear),
      projectID: normalizeNumber(projectID),
      projectTimeSheetID: normalizeNumber(projectTimeSheetID),
      action: normalizeString(action),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const normalizedAction = normalizeString(action).trim().toLowerCase();

  if (normalizedAction === "view") {
    const rows = getApiResponseRows(
      payload,
      API_RESPONSE_TABLE.EMPLOYEE_TIMESHEET_RESPONSE,
    );
    const details = rows?.[0] || null;

    if (status !== COMMON_CONSTANTS.SUCCESS) {
      const error = new Error(
        getMessage(payload, details, "Timesheet Data Not Found."),
      );
      error.name = "EmployeeTimesheetViewError";
      error.payload = payload;
      error.details = details;
      throw error;
    }

    return {
      payload,
      status,
      rows,
      timesheetRows: rows.map(normalizeTimesheetRow),
    };
  }

  const responseTable = EXPORT_RESPONSE_TABLES[normalizedAction];

  if (!responseTable) {
    throw new Error(`Unsupported Timesheet action: ${action}`);
  }

  const details = getApiResponseFirstRow(payload, responseTable);
  const fileBase64 = normalizeString(
    details?.FileBase64 ?? details?.fileBase64,
  ).trim();

  if (status !== COMMON_CONSTANTS.SUCCESS || !fileBase64) {
    const error = new Error(
      getMessage(payload, details, "Unable to download Timesheet."),
    );
    error.name = "EmployeeTimesheetExportError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    exportDetails: {
      fileName: normalizeString(details?.FileName ?? details?.fileName),
      contentType: normalizeString(
        details?.ContentType ?? details?.contentType,
      ),
      fileBase64,
    },
  };
}
