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

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

const normalizeEmployeeProjectMappingRow = (row = {}) => {
  const activeStatus = normalizeString(row?.ActiveStatus ?? row?.activeStatus);
  const isDeletedValue = row?.IsDeleted ?? row?.isDeleted;
  const isDeleted =
    isDeletedValue === undefined || isDeletedValue === null
      ? activeStatus.trim().toLowerCase() === "inactive"
      : normalizeBoolean(isDeletedValue);

  return {
    employeeProjectMappingID: normalizeNumber(
      row?.EmployeeProjectMappingID ??
        row?.EmployeeProjectMappingId ??
        row?.employeeProjectMappingID ??
        row?.employeeProjectMappingId,
    ),
    projectID: normalizeNumber(
      row?.ProjectID ?? row?.ProjectId ?? row?.projectID ?? row?.projectId,
    ),
    projectCode: normalizeString(row?.ProjectCode ?? row?.projectCode),
    projectName: normalizeString(row?.ProjectName ?? row?.projectName),
    projectDescription: normalizeString(
      row?.ProjectDescription ?? row?.projectDescription,
    ),
    clientName: normalizeString(row?.ClientName ?? row?.clientName),
    startDate: normalizeString(row?.StartDate ?? row?.startDate),
    endDate: normalizeString(row?.EndDate ?? row?.endDate),
    employeeCode: normalizeString(row?.EmployeeCode ?? row?.employeeCode),
    employeeName: normalizeString(row?.EmployeeName ?? row?.employeeName),
    employeeEmailID: normalizeString(
      row?.EmployeeEmailID ?? row?.employeeEmailID,
    ),
    isNotifyToEmployee: normalizeBoolean(
      row?.IsNotifyToEmployee ?? row?.isNotifyToEmployee,
    ),
    isDeleted,
    activeStatus: activeStatus || (isDeleted ? "Inactive" : "Active"),
    createdBy: normalizeNumber(row?.CreatedBy ?? row?.createdBy),
    updatedBy: normalizeNumber(row?.UpdatedBy ?? row?.updatedBy),
  };
};

export async function getEmployeeProjectMappingList() {
  const response = await apiClient.post(
    project.get_employee_project_mapping_list,
    {
      id: 0,
      projectID: 0,
      employeeCode: "",
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_PROJECT_MAPPING_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(
        payload,
        details,
        "Unable to load Employee Project Mapping List.",
      ),
    );
    error.name = "EmployeeProjectMappingListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    mappings: rows
      .map(normalizeEmployeeProjectMappingRow)
      .filter((item) => item.employeeProjectMappingID > 0),
  };
}

export async function getEmployeeProjectMappingDetails(mappingId) {
  const response = await apiClient.post(
    project.get_employee_project_mapping_details,
    {
      id: normalizeNumber(mappingId),
      projectID: 0,
      employeeCode: "",
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_PROJECT_MAPPING_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(
      getMessage(
        payload,
        details,
        "Employee Project Mapping Details Not Found.",
      ),
    );
    error.name = "EmployeeProjectMappingDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  const normalized = normalizeEmployeeProjectMappingRow(details);
  if (!normalized.employeeProjectMappingID) {
    const error = new Error("Employee Project Mapping Details Not Found.");
    error.name = "EmployeeProjectMappingDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    mapping: normalized,
  };
}

export async function saveAndUpdateEmployeeProjectMappingDetails({
  id = 0,
  projectID = 0,
  employeeCode = "",
  isNotifyToEmployee = false,
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(
    project.save_and_update_employee_project_mapping_details,
    {
      id: normalizeNumber(id),
      projectID: normalizeNumber(projectID),
      employeeCode: normalizeString(employeeCode),
      isNotifyToEmployee: normalizeBoolean(isNotifyToEmployee),
      isDeleted: normalizeBoolean(isDeleted),
      createdBy: normalizeNumber(createdBy),
      updatedBy: normalizeNumber(updatedBy),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_PROJECT_MAPPING_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Employee Project Mapping Details Saved Successfully."
      : "Employee Project Mapping Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "EmployeeProjectMappingSaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    id: normalizeNumber(details?.Id ?? details?.ID ?? details?.id),
    message,
  };
}

export async function deleteEmployeeProjectMapping(mappingId, updatedBy = 0) {
  const { mapping } = await getEmployeeProjectMappingDetails(mappingId);

  return saveAndUpdateEmployeeProjectMappingDetails({
    id: mapping.employeeProjectMappingID,
    projectID: mapping.projectID,
    employeeCode: mapping.employeeCode,
    isNotifyToEmployee: false,
    isDeleted: true,
    createdBy: 0,
    updatedBy,
  });
}
