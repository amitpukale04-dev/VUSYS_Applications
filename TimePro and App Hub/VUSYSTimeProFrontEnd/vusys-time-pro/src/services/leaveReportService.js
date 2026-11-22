import apiClient from "./apiClient";
import { report } from "../utils/url";
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

const normalizeLeaveReportRow = (row = {}) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  fullName: normalizeString(row?.FullName ?? row?.fullName),
  emailID: normalizeString(row?.EmailID ?? row?.emailID),
  onLeave: normalizeNumber(row?.OnLeave ?? row?.onLeave),
  holiday: normalizeNumber(row?.Holiday ?? row?.holiday),
  compOff: normalizeNumber(row?.CompOff ?? row?.compOff),
});

export async function getEmployeeLeaveReportList({
  employeeCode = "0",
  leaveMonth = "",
  leaveYear = "",
  action = "Search",
} = {}) {
  const response = await apiClient.post(report.get_employee_leave_report_list, {
    employeeCode: normalizeString(employeeCode),
    leaveMonth: normalizeString(leaveMonth),
    leaveYear: normalizeNumber(leaveYear),
    action: normalizeString(action),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const normalizedAction = normalizeString(action).trim().toLowerCase();

  if (normalizedAction === "export") {
    const details = getApiResponseFirstRow(
      payload,
      API_RESPONSE_TABLE.EMPLOYEE_LEAVE_EXPORTED_RESPONSE,
    );

    if (
      status !== COMMON_CONSTANTS.SUCCESS ||
      !normalizeString(details?.FileBase64 ?? details?.fileBase64).trim()
    ) {
      const error = new Error(
        getMessage(payload, details, "Unable to export Leave Report."),
      );
      error.name = "LeaveReportExportError";
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
        fileBase64: normalizeString(details?.FileBase64 ?? details?.fileBase64),
      },
    };
  }

  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_LEAVE_REPORT_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Leave Report Not Found."),
    );
    error.name = "LeaveReportError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    reportRows: rows.map(normalizeLeaveReportRow),
  };
}
