import apiClient from "./apiClient";
import { project } from "../utils/url";
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

// ============================================================
// PROJECT ACTIVITY NORMALIZATION
// ============================================================

const normalizeProjectActivityRow = (row = {}) => ({
  projectActivityId: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  activityName: normalizeString(row?.ActivityName ?? row?.activityName),
  isDeleted: normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
  activeStatus: normalizeString(row?.ActiveStatus ?? row?.activeStatus),
  isActive: !normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
});

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

// ============================================================
// GET PROJECT ACTIVITY LIST
// ============================================================

export async function getProjectActivityList() {
  const response = await apiClient.post(project.get_project_activity_list, {
    id: 0,
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.PROJECT_ACTIVITY_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load Project Activity List."),
    );
    error.name = "ProjectActivityListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    projectActivities: rows
      .map(normalizeProjectActivityRow)
      .filter((item) => item.projectActivityId > 0),
  };
}

// ============================================================
// GET PROJECT ACTIVITY DETAILS
// ============================================================

export async function getProjectActivityDetails(projectActivityId) {
  const response = await apiClient.post(project.get_project_activity_details, {
    id: normalizeNumber(projectActivityId),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.PROJECT_ACTIVITY_DETAILS_RESPONSE,
  );

  if (
    status !== COMMON_CONSTANTS.SUCCESS ||
    !normalizeNumber(details?.Id ?? details?.ID ?? details?.id)
  ) {
    const error = new Error(
      getMessage(payload, details, "Project Activity Details Not Found."),
    );
    error.name = "ProjectActivityDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    projectActivity: normalizeProjectActivityRow(details),
  };
}

// ============================================================
// SAVE / UPDATE / DELETE PROJECT ACTIVITY
// ============================================================

export async function saveAndUpdateProjectActivityDetails({
  id = 0,
  activityName = "",
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(
    project.save_and_update_project_activity_details,
    {
      id: normalizeNumber(id),
      activityName: normalizeString(activityName),
      isDeleted: Boolean(isDeleted),
      createdBy: normalizeNumber(createdBy),
      updatedBy: normalizeNumber(updatedBy),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.PROJECT_ACTIVITY_DETAILS_RESPONSE,
  );
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.PROJECT_ACTIVITY_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Project Activity Details Saved Successfully."
      : "Project Activity Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "ProjectActivitySaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    details,
    projectActivityId: normalizeNumber(
      details?.ID ?? details?.Id ?? details?.id,
    ),
    message,
  };
}
