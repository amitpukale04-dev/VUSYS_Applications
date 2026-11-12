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
// MY LEAVE REQUEST NORMALIZATION
// ============================================================

const normalizeLeaveRequest = (row = {}) => ({
  id: normalizeNumber(row.Id ?? row.id),
  slNo: normalizeNumber(row.SLNo ?? row.slNo),
  employeeCode: normalizeString(row.EmployeeCode ?? row.employeeCode),
  employeeName: normalizeString(row.EmployeeName ?? row.employeeName),
  leaveTypeID: normalizeNumber(row.LeaveTypeID ?? row.leaveTypeID),
  leaveType: normalizeString(row.LeaveType ?? row.leaveType),
  fromDate: normalizeString(row.FromDate ?? row.fromDate),
  toDate: normalizeString(row.ToDate ?? row.toDate),
  totalLeaveDays: normalizeNumber(row.TotalLeaveDays ?? row.totalLeaveDays),
  isHalfDay: normalizeBoolean(row.IsHalfDay ?? row.isHalfDay),
  reason: normalizeString(row.Reason ?? row.reason),
  leaveStatusText: normalizeString(row.LeaveStatusText ?? row.leaveStatusText),
  isShowCancelLeaveRequest: normalizeBoolean(
    row.IsShowCancelLeaveRequest ?? row.isShowCancelLeaveRequest,
  ),
  approvedOrRejectedByUser: normalizeString(
    row.ApprovedOrRejectedByUser ?? row.approvedOrRejectedByUser,
  ),
  approvedOrRejectedDateTime: normalizeString(
    row.ApprovedOrRejectedDateTime ?? row.approvedOrRejectedDateTime,
  ),
  approvedOrRejectedReason: normalizeString(
    row.ApprovedOrRejectedReason ?? row.approvedOrRejectedReason,
  ),
});

const responseMessage = (payload, row, fallback) =>
  row?.Message ||
  row?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

const readResponse = (payload, table, fallback, single = false) => {
  const status = getApiResponseStatus(payload);
  const rows = getApiResponseRows(payload, table);
  const details = getApiResponseFirstRow(payload, table);
  if (status !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(responseMessage(payload, details, fallback));
    error.name = "MyLeaveRequestApiError";
    error.payload = payload;
    error.details = details;
    throw error;
  }
  return single ? details : rows;
};

// ============================================================
// GET MY LEAVE REQUEST LIST / DETAILS
// ============================================================

export async function getMyLeaveRequestList(employeeCode) {
  const response = await apiClient.post(
    leaveManagement.get_leave_request_list,
    {
      id: 0,
      employeeCode: normalizeString(employeeCode),
    },
  );
  const rows = readResponse(
    response?.data || {},
    API_RESPONSE_TABLE.MY_LEAVE_REQUEST_LIST_RESPONSE,
    "Unable to load My Leave Requests.",
  );
  return rows.map(normalizeLeaveRequest).filter((row) => row.id > 0);
}

export async function getMyLeaveRequestDetails(id) {
  const response = await apiClient.post(
    leaveManagement.get_leave_request_details,
    {
      id: normalizeNumber(id),
      employeeCode: "",
    },
  );
  const details = readResponse(
    response?.data || {},
    API_RESPONSE_TABLE.LEAVE_REQUEST_DETAILS_RESPONSE,
    "Leave Request Details Not Found.",
    true,
  );
  if (!details?.Id) throw new Error("Leave Request Details Not Found.");
  return normalizeLeaveRequest(details);
}

// ============================================================
// GET CURRENT YEAR LEAVE BALANCE
// ============================================================

export async function getCurrentYearLeaveBalance(employeeCode, leaveTypeID) {
  const response = await apiClient.post(
    leaveManagement.get_current_year_leave_balance,
    {
      employeeCode: normalizeString(employeeCode),
      leaveTypeID: normalizeNumber(leaveTypeID),
      leaveYear: new Date().getFullYear(),
    },
  );
  const details = readResponse(
    response?.data || {},
    API_RESPONSE_TABLE.EMPLOYEE_LEAVE_BALANCE_AVAILABLE,
    "Unable to load Current Leave Balance.",
    true,
  );
  return normalizeNumber(details?.AvailableBalance ?? 0);
}

// ============================================================
// CALCULATE TOTAL NO OF LEAVE DAYS
// ============================================================

export async function calculateAndGetTotalNoofLeaveDays(values) {
  const response = await apiClient.post(
    leaveManagement.calculate_and_get_total_no_of_leave_days,
    {
      employeeCode: normalizeString(values.employeeCode),
      leaveTypeID: normalizeNumber(values.leaveTypeID),
      fromDate: values.fromDate,
      toDate: values.toDate,
      isHalfDay: normalizeBoolean(values.isHalfDay),
    },
  );
  const details = readResponse(
    response?.data || {},
    API_RESPONSE_TABLE.TOTAL_NO_OF_LEAVE_DAYS_RESPONSE,
    "Unable to calculate Total No of Leave Days.",
    true,
  );
  if (!details) throw new Error("Total No of Leave Days data not found.");
  return {
    totalLeaveDays: normalizeNumber(details.TotalNoofLeaveDays),
    currentLeaveBalance: normalizeNumber(details.CurrentLeaveBalance),
  };
}

// ============================================================
// APPLY / CANCEL LEAVE REQUEST
// ============================================================

export async function applyLeaveRequest(values) {
  const response = await apiClient.post(
    leaveManagement.save_and_update_leave_request_details,
    {
      id: 0,
      employeeCode: normalizeString(values.employeeCode),
      leaveTypeID: normalizeNumber(values.leaveTypeID),
      fromDate: values.fromDate,
      toDate: values.toDate,
      isHalfDay: normalizeBoolean(values.isHalfDay),
      reason: normalizeString(values.reason),
      isDeleted: false,
      createdBy: normalizeNumber(values.createdBy),
      updatedBy: 0,
    },
  );
  const payload = response?.data || {};
  const details = readResponse(
    payload,
    API_RESPONSE_TABLE.LEAVE_REQUEST_RESPONSE,
    "Unable to submit Leave Request.",
    true,
  );
  return responseMessage(
    payload,
    details,
    "Leave Request Submitted Successfully.",
  );
}

export async function cancelMyLeaveRequest({ id, employeeCode, remarks }) {
  const response = await apiClient.post(
    leaveManagement.approve_reject_cancel_leave_request,
    {
      id: normalizeNumber(id),
      employeeCode: normalizeString(employeeCode),
      actionType: "CANCEL",
      actionBy: normalizeNumber(getCreatedOrUpdatedBy()),
      remarks: normalizeString(remarks),
    },
  );
  const payload = response?.data || {};
  const details = readResponse(
    payload,
    API_RESPONSE_TABLE.LEAVE_REQUEST_APPROVE_REJECT_CANCEL_RESPONSE,
    "Unable to cancel Leave Request.",
    true,
  );
  return responseMessage(
    payload,
    details,
    "Leave Request Cancelled Successfully.",
  );
}
