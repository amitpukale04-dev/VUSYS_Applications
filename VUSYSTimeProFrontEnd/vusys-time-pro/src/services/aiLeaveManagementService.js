import apiClient from "./apiClient";
import { aiLeaveManagement } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseFirstRow,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";

const AI_RESPONSE = API_RESPONSE_TABLE.AI_LEAVE_MANAGEMENT_RESPONSE;
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

export async function processAILeaveRequest(message) {
  const response = await apiClient.post(
    aiLeaveManagement.process,
    { message },
    { timeout: 300_000 },
  );
  return readResult(
    response?.data || {},
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
