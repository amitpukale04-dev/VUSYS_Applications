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

const normalizeAllocatedConsumedReportRow = (row = {}) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  projectID: normalizeNumber(row?.ProjectID ?? row?.projectID),
  projectCode: normalizeString(row?.ProjectCode ?? row?.projectCode),
  projectName: normalizeString(row?.ProjectName ?? row?.projectName),
  clientName: normalizeString(row?.ClientName ?? row?.clientName),
  allocatedHours: normalizeNumber(row?.AllocatedHours ?? row?.allocatedHours),
  consumedHours: normalizeNumber(row?.ConsumedHours ?? row?.consumedHours),
  requirementGatheringAndAnalysis: normalizeNumber(
    row?.RequirementGatheringAndAnalysis ?? row?.requirementGatheringAndAnalysis,
  ),
  systemDesign: normalizeNumber(row?.SystemDesign ?? row?.systemDesign),
  frontendDevelopment: normalizeNumber(
    row?.FrontendDevelopment ?? row?.frontendDevelopment,
  ),
  backendDevelopment: normalizeNumber(
    row?.BackendDevelopment ?? row?.backendDevelopment,
  ),
  codeReview: normalizeNumber(row?.CodeReview ?? row?.codeReview),
  systemTesting: normalizeNumber(row?.SystemTesting ?? row?.systemTesting),
  projectManagement: normalizeNumber(
    row?.ProjectManagement ?? row?.projectManagement,
  ),
  deployment: normalizeNumber(row?.Deployment ?? row?.deployment),
  postDeploymentMonitoring: normalizeNumber(
    row?.PostDeploymentMonitoring ?? row?.postDeploymentMonitoring,
  ),
  fixPostReleaseBugs: normalizeNumber(
    row?.FixPostReleaseBugs ?? row?.fixPostReleaseBugs,
  ),
  maintenance: normalizeNumber(row?.Maintenance ?? row?.maintenance),
});

export async function getProjectAllocatedConsumedReportList({
  projectID = 0,
  action = "Search",
} = {}) {
  const response = await apiClient.post(
    report.get_project_allocated_consumed_report_list,
    {
      projectID: normalizeNumber(projectID),
      action: normalizeString(action),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const normalizedAction = normalizeString(action).trim().toLowerCase();

  if (normalizedAction === "export") {
    const details = getApiResponseFirstRow(
      payload,
      API_RESPONSE_TABLE.PROJECT_ALLOCATED_CONSUMED_EXPORTED_RESPONSE,
    );

    if (
      status !== COMMON_CONSTANTS.SUCCESS ||
      !normalizeString(details?.FileBase64 ?? details?.fileBase64).trim()
    ) {
      const error = new Error(
        getMessage(
          payload,
          details,
          "Unable to export Allocated/Consumed Report.",
        ),
      );
      error.name = "AllocatedConsumedReportExportError";
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
        fileBase64: normalizeString(
          details?.FileBase64 ?? details?.fileBase64,
        ),
      },
    };
  }

  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.PROJECT_ALLOCATED_CONSUMED_REPORT_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Allocated/Consumed Report Not Found."),
    );
    error.name = "AllocatedConsumedReportError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    reportRows: rows.map(normalizeAllocatedConsumedReportRow),
  };
}
