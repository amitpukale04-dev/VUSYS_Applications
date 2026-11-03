import apiClient from "./apiClient";
import { leaveManagement } from "../utils/url";
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
import { getCreatedOrUpdatedBy } from "../utils/userSession";

// ============================================================
// EMPLOYEE LEAVE BALANCE NORMALIZATION
// ============================================================

const normalizeEmployeeLeaveBalanceRow = (row = {}) => ({
  slNo: normalizeNumber(row?.SLNo ?? row?.slNo),
  employeeLeaveBalanceId: normalizeNumber(row?.Id ?? row?.ID ?? row?.id),
  employeeCode: normalizeString(row?.EmployeeCode ?? row?.employeeCode),
  employeeName: normalizeString(row?.EmployeeName ?? row?.employeeName),
  leaveTypeID: normalizeNumber(row?.LeaveTypeID ?? row?.leaveTypeID),
  leaveType: normalizeString(row?.LeaveType ?? row?.leaveType),
  leaveTypeCode: normalizeString(row?.LeaveTypeCode ?? row?.leaveTypeCode),
  leaveYear: normalizeNumber(row?.LeaveYear ?? row?.leaveYear),
  openingBalance: normalizeNumber(row?.OpeningBalance ?? row?.openingBalance),
  allocatedLeave: normalizeNumber(row?.AllocatedLeave ?? row?.allocatedLeave),
  usedLeave: normalizeNumber(row?.UsedLeave ?? row?.usedLeave),
  pendingLeave: normalizeNumber(row?.PendingLeave ?? row?.pendingLeave),
  availableBalance: normalizeNumber(row?.AvailableBalance ?? row?.availableBalance),
  isDeleted: normalizeBoolean(row?.IsDeleted ?? row?.isDeleted),
  activeStatus: normalizeString(row?.ActiveStatus ?? row?.activeStatus),
});

const getMessage = (payload, details, fallback) =>
  details?.Message ||
  details?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

const throwResponseError = (name, payload, details, fallback) => {
  const error = new Error(getMessage(payload, details, fallback));
  error.name = name;
  error.payload = payload;
  error.details = details;
  throw error;
};

// ============================================================
// GET EMPLOYEE LEAVE BALANCE LIST
// ============================================================

export async function getEmployeeLeaveBalanceList({
  id = 0,
  employeeCode = "",
  leaveYear = 0,
} = {}) {
  const response = await apiClient.post(
    leaveManagement.get_employee_leave_balance_list,
    {
      id: normalizeNumber(id),
      employeeCode: normalizeString(employeeCode),
      leaveYear: normalizeNumber(leaveYear),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_LEAVE_BALANCE_LIST_RESPONSE,
  );
  const details = rows?.[0] || null;

  if (status !== COMMON_CONSTANTS.SUCCESS) {
    throwResponseError(
      "EmployeeLeaveBalanceListError",
      payload,
      details,
      "Unable to load Employee Leave Balance List.",
    );
  }

  return {
    payload,
    status,
    rows,
    leaveBalances: rows
      .map(normalizeEmployeeLeaveBalanceRow)
      .filter((item) => item.employeeLeaveBalanceId > 0),
  };
}

// ============================================================
// GET EMPLOYEE LEAVE BALANCE DETAILS
// ============================================================

export async function getEmployeeLeaveBalanceDetails({
  id = 0,
  employeeCode = "",
  leaveYear = 0,
} = {}) {
  const response = await apiClient.post(
    leaveManagement.get_employee_leave_balance_details,
    {
      id: normalizeNumber(id),
      employeeCode: normalizeString(employeeCode),
      leaveYear: normalizeNumber(leaveYear),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_LEAVE_BALANCE_DETAILS_RESPONSE,
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details?.Id) {
    throwResponseError(
      "EmployeeLeaveBalanceDetailsError",
      payload,
      details,
      "Employee Leave Balance Details Not Found.",
    );
  }

  return {
    payload,
    status,
    details,
    leaveBalance: normalizeEmployeeLeaveBalanceRow(details),
  };
}

// ============================================================
// SAVE / UPDATE / DELETE EMPLOYEE LEAVE BALANCE
// ============================================================

export async function saveAndUpdateEmployeeLeaveBalanceDetails({
  id = 0,
  employeeCode = "",
  leaveTypeID = 0,
  leaveYear = 0,
  openingBalance = 0,
  allocatedLeave = 0,
  usedLeave = 0,
  pendingLeave = 0,
  availableBalance = 0,
  isDeleted = false,
  createdBy = 0,
  updatedBy = 0,
}) {
  const response = await apiClient.post(
    leaveManagement.save_and_update_employee_leave_balance_details,
    {
      id: normalizeNumber(id),
      employeeCode: normalizeString(employeeCode),
      leaveTypeID: normalizeNumber(leaveTypeID),
      leaveYear: normalizeNumber(leaveYear),
      openingBalance: normalizeNumber(openingBalance),
      allocatedLeave: normalizeNumber(allocatedLeave),
      usedLeave: normalizeNumber(usedLeave),
      pendingLeave: normalizeNumber(pendingLeave),
      availableBalance: normalizeNumber(availableBalance),
      isDeleted: normalizeBoolean(isDeleted),
      createdBy: normalizeNumber(createdBy),
      updatedBy: normalizeNumber(updatedBy),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_LEAVE_BALANCE_DETAILS_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Employee Leave Balance Details Saved Successfully."
      : "Employee Leave Balance Details Not Saved.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    throwResponseError("EmployeeLeaveBalanceSaveUpdateError", payload, details, message);
  }

  return {
    payload,
    status,
    details,
    id: normalizeNumber(details?.Id ?? details?.id),
    message,
  };
}

export async function deleteEmployeeLeaveBalance(id, updatedBy = 0) {
  const { leaveBalance } = await getEmployeeLeaveBalanceDetails({ id });

  return saveAndUpdateEmployeeLeaveBalanceDetails({
    ...leaveBalance,
    id: leaveBalance.employeeLeaveBalanceId,
    isDeleted: true,
    createdBy: 0,
    updatedBy,
  });
}

// ============================================================
// INITIALIZE EMPLOYEE LEAVE BALANCE
// ============================================================

export async function saveEmployeeLeaveBalanceInitialize({leaveYear = 0}) 
{
  const response = await apiClient.post(
    leaveManagement.save_employee_leave_balance_initialize,
    {
      leaveYear: normalizeNumber(leaveYear),
      createdBy: normalizeNumber(getCreatedOrUpdatedBy()),
    },
  );

  const payload = response?.data || {};
  const status = getApiResponseStatus(payload);
  const details = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.EMPLOYEE_LEAVE_BALANCE_INITIALIZE_RESPONSE,
  );
  const message = getMessage(
    payload,
    details,
    status === COMMON_CONSTANTS.SUCCESS
      ? "Employee Leave Balance Initialized Successfully."
      : "Unable to initialize Employee Leave Balance.",
  );

  if (status !== COMMON_CONSTANTS.SUCCESS || !details) {
    throwResponseError("EmployeeLeaveBalanceInitializeError", payload, details, message);
  }

  return {
    payload,
    status,
    details,
    id: normalizeNumber(details?.Id ?? details?.id),
    message,
  };
}
