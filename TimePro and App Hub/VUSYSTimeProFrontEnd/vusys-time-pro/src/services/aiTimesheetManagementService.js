import apiClient from "./apiClient";
import { aiTimesheetManagement } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseFirstRow,
  getApiResponseRows,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import { normalizeTimesheetRow } from "./downloadTimesheetService";
import { CONTENT_TYPE_CONSTANTS } from "../constants/ContentTypeConstants";

const AI_RESPONSE = API_RESPONSE_TABLE.AI_TIMESHEET_MANAGEMENT_RESPONSE;

// Business errors are returned to the conversation with any available details.
export function readAITimesheetResult(payload) {
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(payload, AI_RESPONSE);
  const submitted = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_SUBMITTED_RESPONSE,
  );
  const reviewed = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.TIMESHEET_APPROVE_REJECT_RESPONSE,
  );
  const file = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_TIMESHEET_EXPORTED_RESPONSE,
  );
  const view = Array.isArray(
    payload?.data?.[API_RESPONSE_TABLE.EMPLOYEE_TIMESHEET_RESPONSE],
  );
  const reviewAttachment =
    ["ApproveTimesheet", "RejectTimesheet"].includes(details?.Intent) &&
    ["clarificationrequired", "confirmationrequired"].includes(status);
  const row = reviewAttachment
    ? details
    : submitted || reviewed || file || details;
  const rowStatus = String(row?.Status ?? "")
    .trim()
    .toLowerCase();
  const successful =
    status === COMMON_CONSTANTS.SUCCESS &&
    (!rowStatus || rowStatus === COMMON_CONSTANTS.SUCCESS);
  const message =
    row?.Message ||
    details?.Message ||
    (String(payload?.message || "").toLowerCase() !== "ok"
      ? payload?.message
      : "");
  const result = {
    status: successful ? "success" : status === "success" ? "error" : status,
    details: details || {},
    message,
  };
  if (reviewAttachment || (successful && file)) {
    const base64 = String(file?.FileBase64 || "")
      .trim()
      .replace(/^data:[^,]*;base64,/i, "")
      .replace(/\s/g, "");
    if (
      !String(file?.FileName || "").trim() ||
      String(file?.ContentType || "")
        .split(";")[0]
        .trim()
        .toLowerCase() !== CONTENT_TYPE_CONSTANTS.EXCEL_XLSX ||
      !base64 ||
      !/^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/.test(
        base64,
      )
    ) {
      if (reviewAttachment) {
        return {
          ...result,
          reviewFileError: "Timesheet Excel file is unavailable or invalid.",
        };
      }
      return {
        ...result,
        status: "error",
        message: "The timesheet download response is incomplete or invalid.",
      };
    }
    const validatedFile = { ...file, FileBase64: base64 };
    return reviewAttachment
      ? { ...result, reviewFile: validatedFile }
      : { ...result, file: validatedFile };
  }
  if (successful && view) {
    return {
      ...result,
      rows: getApiResponseRows(
        payload,
        API_RESPONSE_TABLE.EMPLOYEE_TIMESHEET_RESPONSE,
      ).map(normalizeTimesheetRow),
    };
  }
  if (
    !row ||
    ![
      "success",
      "error",
      "clarificationrequired",
      "confirmationrequired",
    ].includes(result.status)
  ) {
    return {
      ...result,
      status: "error",
      message: message || "Unexpected timesheet response. Please try again.",
    };
  }
  return result;
}

export async function processAITimesheetRequest(
  message,
  clarificationToken = "",
) {
  const response = await apiClient.post(
    aiTimesheetManagement.process,
    { message, clarificationToken },
    { timeout: 300_000 },
  );
  return readAITimesheetResult(response?.data || {});
}

export async function confirmAITimesheetAction(confirmationToken) {
  const response = await apiClient.post(
    aiTimesheetManagement.confirm,
    { confirmationToken },
    { timeout: 300_000 },
  );
  return readAITimesheetResult(response?.data || {});
}
