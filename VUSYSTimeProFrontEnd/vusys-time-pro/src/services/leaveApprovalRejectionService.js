import apiClient from "./apiClient";
import { leaveManagement } from "../utils/url";
import { API_RESPONSE_TABLE, COMMON_CONSTANTS } from "../constants/constants";
import {
  getApiResponseRows,
  getApiResponseFirstRow,
  getApiResponseStatus,
} from "../utils/apiResponseUtils";
import { normalizeNumber, normalizeString } from "../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../utils/userSession";

// ============================================================
// LEAVE APPROVAL / REJECTION - API RESPONSE HANDLING
// ============================================================

const responseMessage = (payload, row, fallback) =>
  row?.Message ||
  row?.message ||
  payload?.Message ||
  payload?.message ||
  fallback;

const normalizeRow = (row = {}, index = 0) => ({
  id: normalizeNumber(row.Id),
  slNo: normalizeNumber(row.SLNo) || index + 1,
  employeeCode: normalizeString(row.EmployeeCode),
  employeeName: normalizeString(row.EmployeeName),
  leaveType: normalizeString(row.LeaveType),
  fromDate: normalizeString(row.FromDate),
  toDate: normalizeString(row.ToDate),
  totalLeaveDays: normalizeNumber(row.TotalLeaveDays),
  reason: normalizeString(row.Reason),
  leaveStatus: normalizeNumber(row.LeaveStatus),
  leaveStatusText: normalizeString(row.LeaveStatusText),
  approvedOrRejectedByUser: normalizeString(row.ApprovedOrRejectedByUser),
  approvedOrRejectedDateTime: normalizeString(row.ApprovedOrRejectedDateTime),
  approvedOrRejectedReason: normalizeString(row.ApprovedOrRejectedReason),
});

// ============================================================
// GET PENDING LEAVE REQUEST LIST
// ============================================================

export async function getPendingLeaveRequestList(employeeCode = "") {
  const response = await apiClient.post(
    leaveManagement.get_pending_leave_request_list,
    {
      id: 0,
      employeeCode: employeeCode === "0" ? "" : normalizeString(employeeCode),
    },
  );
  const payload = response?.data || {};
  const rows = getApiResponseRows(
    payload,
    API_RESPONSE_TABLE.PENDING_LEAVE_REQUEST_LIST_RESPONSE,
  );
  if (getApiResponseStatus(payload) !== COMMON_CONSTANTS.SUCCESS) {
    throw new Error(
      responseMessage(payload, rows[0], "Leave Request List Not Found."),
    );
  }
  return rows.map(normalizeRow).filter((row) => row.id > 0);
}

// ============================================================
// APPROVE OR REJECT LEAVE REQUEST
// ============================================================

export async function approveOrRejectLeaveRequest({
  id,
  employeeCode,
  actionType,
  remarks,
}) {
  const response = await apiClient.post(
    leaveManagement.approve_reject_cancel_leave_request,
    {
      id: normalizeNumber(id),
      employeeCode: normalizeString(employeeCode),
      actionType: normalizeString(actionType),
      actionBy: normalizeNumber(getCreatedOrUpdatedBy()),
      remarks: normalizeString(remarks),
    },
  );
  const payload = response?.data || {};
  const row = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.LEAVE_REQUEST_APPROVE_REJECT_CANCEL_RESPONSE,
  );
  if (getApiResponseStatus(payload) !== COMMON_CONSTANTS.SUCCESS) {
    throw new Error(
      responseMessage(payload, row, "Unable to Approve/Reject Leave Request."),
    );
  }
  return responseMessage(payload, row, "Leave Request Updated Successfully.");
}

// ============================================================
// DIRECT EMAIL LINK - LEAVE REQUEST DETAILS / APPROVAL STATUS
// ============================================================

export async function getLeaveApprovalRequestDetails({ id, employeeCode }) {
  const response = await apiClient.post(
    leaveManagement.get_leave_request_details,
    {
      id: normalizeNumber(id),
      employeeCode: normalizeString(employeeCode),
    },
  );
  const payload = response?.data || {};
  const row = getApiResponseFirstRow(
    payload,
    API_RESPONSE_TABLE.LEAVE_REQUEST_DETAILS_RESPONSE,
  );
  if (
    getApiResponseStatus(payload) !== COMMON_CONSTANTS.SUCCESS ||
    !normalizeNumber(row?.Id)
  ) {
    throw new Error(
      responseMessage(payload, row, "Leave Request Details Not Found."),
    );
  }
  return normalizeRow(row);
}

export async function checkLeaveRequestApprovalStatus({ id, employeeCode }) {
  const response = await apiClient.post(
    leaveManagement.check_leave_request_approval_status,
    {
      id: normalizeNumber(id),
      employeeCode: normalizeString(employeeCode),
    },
  );
  const payload = response?.data || {};
  const row = getApiResponseFirstRow(
    payload,
    "LEAVE_REQUEST_APPROVAL_STATUS_RESPONSE",
  );
  if (getApiResponseStatus(payload) !== COMMON_CONSTANTS.SUCCESS) {
    const error = new Error(
      responseMessage(
        payload,
        row,
        "Leave Request Is Not Pending For Approval.",
      ),
    );
    error.isLeaveApprovalStatusError = true;
    throw error;
  }
  return responseMessage(payload, row, "Pending For Approval.");
}
