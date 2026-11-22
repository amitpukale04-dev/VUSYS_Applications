import apiClient from "./apiClient";
import { aiLeaveManagement } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseFirstRow,
  getApiResponseRows,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import { CONTENT_TYPE_CONSTANTS } from "../constants/ContentTypeConstants";

const AI_RESPONSE = API_RESPONSE_TABLE.AI_LEAVE_MANAGEMENT_RESPONSE;
const AI_REQUEST_DETAILS_RESPONSE =
  API_RESPONSE_TABLE.AI_LEAVE_REQUEST_DETAILS_RESPONSE;
const AI_BALANCE_RESPONSE = API_RESPONSE_TABLE.AI_LEAVE_BALANCE_RESPONSE;
const AI_LEAVE_DYNAMIC_REPORT_RESPONSE =
  API_RESPONSE_TABLE.AI_LEAVE_DYNAMIC_REPORT_RESPONSE;

const READ_ONLY_INTENTS = new Set([
  "GetLeaveBalance",
  "GetLeaveStatus",
  "GetLeaveHistory",
  "GetPendingLeaves",
  "GetLeaveDetails",
]);
const LEAVE_RESPONSE = API_RESPONSE_TABLE.LEAVE_REQUEST_RESPONSE;
const LEAVE_ACTION_RESPONSE =
  API_RESPONSE_TABLE.LEAVE_REQUEST_APPROVE_REJECT_CANCEL_RESPONSE;

const readResult = (payload, table, fallback) => {
  const details = getApiResponseFirstRow(payload, table);
  const errorDetails = getApiResponseFirstRow(payload, AI_RESPONSE);
  if (getApiResponseStatus(payload) !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      errorDetails?.Message || details?.Message || payload?.message || fallback,
    );
    error.payload = payload;
    throw error;
  }
  if (!details) throw new Error(fallback);
  return details;
};

export async function processAILeaveRequest(
  message,
  clarificationToken = "",
  reportOptions = null,
) {
  const response = await apiClient.post(
    aiLeaveManagement.process,
    {
      message,
      ...(clarificationToken ? { clarificationToken } : {}),
      ...(reportOptions || {}),
    },
    { timeout: 300_000 },
  );
  const payload = response?.data || {};
  const details = getApiResponseFirstRow(payload, AI_RESPONSE);
  const matches = getApiResponseRows(payload, AI_REQUEST_DETAILS_RESPONSE);
  const balances = getApiResponseRows(payload, AI_BALANCE_RESPONSE);
  if (details?.Intent === "GenerateLeaveReport") {
    if (getApiResponseStatus(payload) !== COMMON_CONSTANTS.SUCCESS) {
      throw new Error(
        details.Message || "Unable to generate the leave report.",
      );
    }
    return {
      ...details,
      report: true,
      reportRows: getApiResponseRows(payload, AI_LEAVE_DYNAMIC_REPORT_RESPONSE),
    };
  }
  // Read-only results are never treated as action-selection or confirmation steps.
  if (details && READ_ONLY_INTENTS.has(details.Intent)) {
    const status = getApiResponseStatus(payload);
    if (details.ClarificationToken && !details.ConfirmationToken) {
      return { ...details, readOnly: true, records: [], balances: [] };
    }
    if (status !== COMMON_CONSTANTS.SUCCESS) {
      const error = new Error(
        details.Message ||
          payload?.message ||
          "Unable to retrieve leave information.",
      );
      error.payload = payload;
      throw error;
    }
    return { ...details, readOnly: true, records: matches, balances };
  }
  // A multiple-match result can have Status=Error, but is a selection step,
  // not a terminal API failure. Never choose a matching request automatically.
  if (matches.length > 1 && !details?.ConfirmationToken) {
    return { ...details, requiresSelection: true, matches };
  }
  // Clarification is a continuation step, even if the backend reports Status=Error.
  // The next message must include the token supplied by this response.
  if (details?.ClarificationToken && !details?.ConfirmationToken) {
    return details;
  }
  // An approval/rejection review response may have Status=Error because
  // required remarks are not yet supplied. Show the returned details without
  // treating it as a confirmation or a terminal API error.
  if (
    !details?.ConfirmationToken &&
    ["ApproveLeave", "RejectLeave", "CancelLeave"].includes(details?.Intent) &&
    /review the leave details/i.test(details?.Message || "")
  ) {
    return details;
  }
  return readResult(
    payload,
    AI_RESPONSE,
    "Unable to process the leave request.",
  );
}

export async function confirmAILeaveAction(confirmationToken) {
  const response = await apiClient.post(
    aiLeaveManagement.confirm,
    {
      confirmationToken,
    },
    { timeout: 300_000 },
  );
  const payload = response?.data || {};
  const responseTable = getApiResponseFirstRow(payload, LEAVE_ACTION_RESPONSE)
    ? LEAVE_ACTION_RESPONSE
    : LEAVE_RESPONSE;

  return readResult(
    payload,
    responseTable,
    "Unable to confirm the leave action.",
  );
}

/** Export a previously viewed AI leave report using its own secure report token. */
export async function exportAILeaveReport(message, reportToken, action) {
  if (!["Excel", "PDF"].includes(action) || !reportToken) {
    throw new Error("Invalid report export request.");
  }
  const response = await apiClient.post(
    aiLeaveManagement.process,
    { message, clarificationToken: "", action, reportToken },
    { timeout: 300_000 },
  );
  const payload = response?.data || {};
  const table =
    action === "Excel"
      ? API_RESPONSE_TABLE.AI_LEAVE_EXCEL_EXPORTED_RESPONSE
      : API_RESPONSE_TABLE.AI_LEAVE_PDF_EXPORTED_RESPONSE;
  const row = getApiResponseFirstRow(payload, table);
  // The export table's Status field is optional in some backend responses.
  // A successful envelope with valid file data must not be treated as an error
  // merely because the export row omits Status.
  const exportStatus = String(row?.Status || "")
    .trim()
    .toLowerCase();
  if (
    getApiResponseStatus(payload) !== COMMON_CONSTANTS.SUCCESS ||
    (exportStatus && exportStatus !== COMMON_CONSTANTS.SUCCESS)
  ) {
    const error = new Error(
      row?.Message ||
        getApiResponseFirstRow(payload, AI_RESPONSE)?.Message ||
        "Unable to export the report.",
    );
    error.isReportTokenError = /token|expired|invalid/i.test(error.message);
    throw error;
  }
  const expectedType =
    action === "Excel"
      ? CONTENT_TYPE_CONSTANTS.EXCEL_XLSX
      : CONTENT_TYPE_CONSTANTS.PDF;
  // Normalize backend Base64 (including wrapped lines and data URLs) before
  // validating and passing the content to the existing shared download utility.
  const rawBase64 = String(row?.FileBase64 ?? "").trim();
  const dataUrlMatch = rawBase64.match(/^data:([^;,]+)?;base64,(.*)$/is);
  const base64 = (dataUrlMatch ? dataUrlMatch[2] : rawBase64)
    .replace(/\s/g, "")
    .replace(/-/g, "+")
    .replace(/_/g, "/");
  const normalizedBase64 = base64.padEnd(
    base64.length + ((4 - (base64.length % 4)) % 4),
    "=",
  );
  const actualContentType = String(row?.ContentType || "")
    .split(";")[0]
    .trim()
    .toLowerCase();
  if (
    !String(row?.FileName || "").trim() ||
    actualContentType !== expectedType ||
    !normalizedBase64 ||
    !/^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/.test(
      normalizedBase64,
    )
  ) {
    throw new Error("The report download response is incomplete or invalid.");
  }
  return { ...row, FileBase64: normalizedBase64 };
}
