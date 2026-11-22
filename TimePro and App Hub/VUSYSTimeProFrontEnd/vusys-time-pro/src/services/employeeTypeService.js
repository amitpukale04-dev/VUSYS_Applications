import apiClient from "./apiClient";
import { employee } from "../utils/url";
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
// EMPLOYEE TYPE NORMALIZATION
// ============================================================

const normalizeEmployeeTypeRow = (row = {}) => ({
  employeeTypeId: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  employeeType: normalizeString(row?.EmployeeType ?? row?.employeeType),
  isDeleted: normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
  activeStatus: normalizeString(row?.ActiveStatus ?? row?.activeStatus),
  isActive: !normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
  createdBy: normalizeNumber(row?.CreatedBy ?? row?.createdBy),
  updatedBy: normalizeNumber(row?.UpdatedBy ?? row?.updatedBy),
});

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

// ============================================================
// GET EMPLOYEE TYPE LIST
// ============================================================

export async function getEmployeeTypeList() {
  const response = await apiClient.post(employee.get_employee_type_list, {
    id: 0,
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_TYPE_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      getMessage(payload, details, "Unable to load Employee Type List."),
    );
    error.name = "EmployeeTypeListError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    employeeTypes: rows
      .map(normalizeEmployeeTypeRow)
      .filter((item) => item.employeeTypeId > 0),
  };
}

// ============================================================
// GET EMPLOYEE TYPE DETAILS
// ============================================================

export async function getEmployeeTypeDetails(employeeTypeId) {
  const response = await apiClient.post(employee.get_employee_type_details, {
    id: normalizeNumber(employeeTypeId),
  });

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_TYPE_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details?.Id) {
    const error = new Error(
      getMessage(payload, details, "Employee Type Details Not Found."),
    );
    error.name = "EmployeeTypeDetailsError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    details,
    employeeType: normalizeEmployeeTypeRow(details),
  };
}

// ============================================================
// SAVE / UPDATE / DELETE EMPLOYEE TYPE
// ============================================================

export async function saveAndUpdateEmployeeTypeDetails({
  id = 0,
  employeeType = "",
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(
    employee.save_and_update_employee_type_details,
    {
      id: normalizeNumber(id),
      employeeType: normalizeString(employeeType),
      isDeleted: Boolean(isDeleted),
      createdBy: normalizeNumber(createdBy),
      updatedBy: normalizeNumber(updatedBy),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_TYPE_DETAILS_RESPONSE,
  );
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_TYPE_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Employee Type Details Saved Successfully."
      : "Employee Type Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    const error = new Error(message);
    error.name = "EmployeeTypeSaveUpdateError";
    error.payload = payload;
    error.details = details;
    throw error;
  }

  return {
    payload,
    status,
    rows,
    details,
    employeeTypeId: normalizeNumber(
      details?.ID ?? details?.Id ?? details?.id,
    ),
    message,
  };
}
