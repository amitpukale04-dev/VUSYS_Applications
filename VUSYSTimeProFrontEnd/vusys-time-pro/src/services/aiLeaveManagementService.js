import apiClient from "./apiClient";
import { aiLeaveManagement } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseFirstRow,
  getApiResponseRows,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";

const AI_RESPONSE = API_RESPONSE_TABLE.AI_LEAVE_MANAGEMENT_RESPONSE;
const AI_REQUEST_DETAILS_RESPONSE =
  API_RESPONSE_TABLE.AI_LEAVE_REQUEST_DETAILS_RESPONSE;
const AI_BALANCE_RESPONSE = API_RESPONSE_TABLE.AI_LEAVE_BALANCE_RESPONSE;
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

export async function processAILeaveRequest(message, clarificationToken = "") {
  const response = await apiClient.post(
    aiLeaveManagement.process,
    { message, ...(clarificationToken ? { clarificationToken } : {}) },
    { timeout: 300_000 },
  );
  const payload = response?.data || {};
  const details = getApiResponseFirstRow(payload, AI_RESPONSE);
  const matches = getApiResponseRows(payload, AI_REQUEST_DETAILS_RESPONSE);
  const balances = getApiResponseRows(payload, AI_BALANCE_RESPONSE);
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
