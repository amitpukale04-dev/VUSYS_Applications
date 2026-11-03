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
import { toISODateOnly } from "../utils/dateUtils";
import { clearDropdownItemsCache } from "./dropdownService";

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

const normalizeProjectRow = (row = {}) => {
  const activeStatus = normalizeString(row?.ActiveStatus ?? row?.activeStatus);
  const isDeletedValue = row?.IsDeleted ?? row?.isDeleted;
  const isDeleted =
    isDeletedValue === undefined || isDeletedValue === null
      ? activeStatus.trim().toLowerCase() === "inactive"
      : normalizeBoolean(isDeletedValue);

  return {
    slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
    projectID: normalizeNumber(
      row?.ProjectID ?? row?.ProjectId ?? row?.projectID ?? row?.projectId,
    ),
    projectCode: normalizeString(row?.ProjectCode ?? row?.projectCode),
    clientName: normalizeString(row?.ClientName ?? row?.clientName),
    projectName: normalizeString(row?.ProjectName ?? row?.projectName),
    projectDescription: normalizeString(
      row?.ProjectDescription ?? row?.projectDescription,
    ),
    startDate: toISODateOnly(row?.StartDate ?? row?.startDate),
    endDate: toISODateOnly(row?.EndDate ?? row?.endDate),
    displayStartDate: normalizeString(
      row?.DisplayStartDate ?? row?.displayStartDate,
    ),
    displayEndDate: normalizeString(
      row?.DisplayEndDate ?? row?.displayEndDate,
    ),
    allocatedHours: normalizeNumber(
      row?.AllocatedHours ?? row?.allocatedHours,
    ),
    consumedHours: normalizeNumber(row?.ConsumedHours ?? row?.consumedHours),
    projectStatus: normalizeString(row?.ProjectStatus ?? row?.projectStatus),
    isDeleted,
    activeStatus: activeStatus || (isDeleted ? "Inactive" : "Active"),
  };
};

export async function getProjectList() {
  const response = await apiClient.post(project.get_project_list, { id: 0 });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.PROJECT_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load Project List."),
    );
    error.name = "ProjectListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    projects: rows
      .map(normalizeProjectRow)
      .filter((item) => item.projectID > 0),
  };
}

export async function getProjectDetails(projectId) {
  const response = await apiClient.post(project.get_project_details, {
    id: normalizeNumber(projectId),
  });
  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.PROJECT_DETAILS_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(
      getMessage(payload, details, "Project Details Not Found"),
    );
    error.name = "ProjectDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  const normalized = normalizeProjectRow(details);
  if (!normalized.projectID) {
    const error = new Error("Project Details Not Found");
    error.name = "ProjectDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    project: normalized,
  };
}

export async function saveAndUpdateProjectDetails({
  projectID = 0,
  projectCode = "",
  clientName = "",
  projectName = "",
  projectDescription = "",
  startDate = "",
  endDate = "",
  allocatedHours = 0,
  consumedHours = 0,
  projectStatus = "",
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(
    project.save_and_update_project_details,
    {
      projectID: normalizeNumber(projectID),
      projectCode: normalizeString(projectCode),
      clientName: normalizeString(clientName),
      projectName: normalizeString(projectName),
      projectDescription: normalizeString(projectDescription),
      startDate: normalizeString(startDate),
      endDate: normalizeString(endDate),
      allocatedHours: normalizeNumber(allocatedHours),
      consumedHours: normalizeNumber(consumedHours),
      projectStatus: normalizeString(projectStatus),
      isDeleted: normalizeBoolean(isDeleted),
      createdBy: normalizeNumber(createdBy),
      updatedBy: normalizeNumber(updatedBy),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.PROJECT_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Project Details Saved Successfully."
      : "Project Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "ProjectSaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  // Project is expected to be a common dropdown source in project mapping
  // screens. Invalidate it after successful add/update/obsolete operations.
  clearDropdownItemsCache("Project");

  return {
    payload,
    status,
    details,
    id: normalizeNumber(details?.Id ?? details?.ID ?? details?.id),
    message,
  };
}

export async function deleteProjectMaster(projectId, updatedBy = 0) {
  const { project: projectDetails } = await getProjectDetails(projectId);

  return saveAndUpdateProjectDetails({
    ...projectDetails,
    projectID: projectDetails.projectID,
    isDeleted: true,
    createdBy: 0,
    updatedBy,
  });
}
