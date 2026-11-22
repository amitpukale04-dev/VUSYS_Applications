import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import ConfirmModal from "../../components/common/ConfirmModal";
import SortableTable from "../../components/common/SortableTable";
import SvgIcon from "../../components/common/SvgIcon";
import { SearchableDropdown } from "../../components/forms/FormElements";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { useAuth } from "../../context/AuthContext";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { getDropdownItems } from "../../services/dropdownService";
import {
  approveOrRejectLeaveRequest,
  getPendingLeaveRequestList,
} from "../../services/leaveApprovalRejectionService";
import ToastService from "../../services/toastService";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import LeaveApproveRejectModal from "./LeaveApproveRejectModal";
import LeaveDetailsPopup from "./LeaveDetailsPopup";
import LeaveApprovedRejectedDetailsModal from "./LeaveApprovedRejectedDetailsModal";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateLeaveApprovalRejectionList =
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_APPROVAL_REJECTION_LIST;

const LEAVE_APPROVE_REJECT_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  approvalStatus: { apiKeys: ["ActionType"] },
  actionBy: { apiKeys: ["ActionBy"] },
  remarks: { apiKeys: ["Remarks"] },
};

// ============================================================
// LEAVE APPROVAL / REJECTION LIST
// ============================================================

export default function LeaveApprovalRejectionList() {
  const { hasPermission, isLoading: securityPermissionLoading } =
    useUserSecurityPermission();
  const canOpenScreen = hasPermission("Leave Approval/Rejection List", "Open");
  const canApproveLeave = hasPermission(
    "Leave Approval/Rejection List",
    "Approve",
  );
  const { closeTab } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const [employeeCode, setEmployeeCode] = useState("0");
  const [employeeOptions, setEmployeeOptions] = useState([]);
  const [approvalOptions, setApprovalOptions] = useState([]);
  const [gridRefreshTrigger, setGridRefreshTrigger] = useState(0);
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [approveRejectRow, setApproveRejectRow] = useState(null);
  const [apiFieldErrors, setApiFieldErrors] = useState({});
  const [approvalRequest, setApprovalRequest] = useState(null);
  const [leaveDetailsRow, setLeaveDetailsRow] = useState(null);
  const [approvedRejectedDetailsRow, setApprovedRejectedDetailsRow] =
    useState(null);
  const loadSequenceRef = useRef(0);

  const loadPendingLeaveRequestRows = useCallback(
    async (selectedEmployee = "0", showError = true) => {
      if (securityPermissionLoading || !canOpenScreen) return;
      setGridRefreshTrigger((current) => current + 1);
      const requestSequence = ++loadSequenceRef.current;
      const loaderId = showFullPageLoader(
        "Loading Leave Approval/Rejection List...",
      );
      setLoading(true);
      try {
        const result = await getPendingLeaveRequestList(selectedEmployee);
        if (requestSequence === loadSequenceRef.current) setRows(result);
      } catch (error) {
        if (requestSequence === loadSequenceRef.current) {
          setRows([]);
          // Initial automatic load must not display a "List Not Found" toast.
          if (showError)
            showApiError(
              error,
              "Unable to load Leave Approval/Rejection List.",
            );
        }
      } finally {
        if (requestSequence === loadSequenceRef.current) setLoading(false);
        hideFullPageLoader(loaderId);
      }
    },
    [
      hideFullPageLoader,
      showFullPageLoader,
      securityPermissionLoading,
      canOpenScreen,
    ],
  );

  useEffect(() => {
    if (securityPermissionLoading || !canOpenScreen) return undefined;
    let active = true;

    const initialize = async () => {
      try {
        const [employees, statuses] = await Promise.all([
          getDropdownItems({
            sourceTable: DROPDOWN_SOURCE_TABLE.EMPLOYEE_PROJECT_WITH_ALL,
          }),
          getDropdownItems({
            sourceTable: DROPDOWN_SOURCE_TABLE.LEAVE_APPROVE_REJECT_STATUS,
          }),
        ]);
        if (!active) return;
        setEmployeeOptions(employees.options);
        setApprovalOptions(statuses.options);
      } catch (error) {
        if (active)
          showApiError(
            error,
            "Unable to load Leave Approval/Rejection dropdowns.",
          );
      } finally {
        if (active) await loadPendingLeaveRequestRows("0", false);
      }
    };

    void initialize();
    return () => {
      active = false;
    };
  }, [loadPendingLeaveRequestRows]);

  const updateEmployee = (event) => {
    const selected = String(event.target.value);
    setEmployeeCode(selected);
    void loadPendingLeaveRequestRows(selected, true);
  };

  const confirmApproveReject = async () => {
    if (!canOpenScreen || !canApproveLeave || !approvalRequest || saving)
      return;
    const { row, form } = approvalRequest;
    const isApprove = form.approvalStatus === "2";
    const loaderId = showFullPageLoader(
      isApprove ? "Approving Leave Request..." : "Rejecting Leave Request...",
    );
    setSaving(true);
    try {
      const message = await approveOrRejectLeaveRequest({
        id: row.id,
        employeeCode: form.employeeCode,
        actionType: isApprove ? "APPROVE" : "REJECT",
        actionBy: loggedInUserId,
        remarks: form.remarks.trim(),
      });
      ToastService.success(message);
      setApprovalRequest(null);
      setApproveRejectRow(null);
      setApiFieldErrors({});
      await loadPendingLeaveRequestRows(employeeCode, false);
    } catch (error) {
      setApprovalRequest(null);
      if (error?.isApiValidationError) {
        setApiFieldErrors({});
        applyApiValidationErrors(
          error.validationErrors || [],
          LEAVE_APPROVE_REJECT_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setApiFieldErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to Approve/Reject Leave Request.");
      }
    } finally {
      setSaving(false);
      hideFullPageLoader(loaderId);
    }
  };

  const columns = useMemo(
    () => [
      { key: "slNo", label: "SL No", width: "115px" },
      { key: "employeeCode", label: "Employee Code", width: "180px" },
      { key: "employeeName", label: "Name", width: "180px" },
      { key: "leaveType", label: "Leave Type", width: "180px" },
      { key: "fromDate", label: "From Date", width: "150px" },
      { key: "toDate", label: "To Date", width: "150px" },
      { key: "totalLeaveDays", label: "Leave Days", width: "155px" },
      { key: "leaveStatusText", label: "Approval Status", width: "195px" },
      {
        key: "approvedOrRejectedReason",
        label: "Approved/Rejected Details",
        width: "245px",
        sortable: false,
        filterable: false,
        render: (value, row) =>
          String(value || "").trim() ? (
            <button
              type="button"
              className="tp-report-link-button"
              onClick={() => setApprovedRejectedDetailsRow(row)}
              title="View Details"
            >
              View Details
            </button>
          ) : null,
      },
    ],
    [],
  );

  const renderActions = useCallback(
    (row) =>
      canApproveLeave ? (
        <div className="tp-row-actions">
          <button
            type="button"
            className="tp-row-more"
            title="Approve / Reject"
            aria-label="Approve / Reject"
            onClick={() => {
              setApiFieldErrors({});
              setApproveRejectRow(row);
            }}
          >
            <SvgIcon name="ApproveRejectIcon" size={20} />
          </button>
        </div>
      ) : null,
    [canApproveLeave],
  );

  const isApprove = approvalRequest?.form?.approvalStatus === "2";

  if (securityPermissionLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  if (!canOpenScreen) return <AccessDenied />;

  return (
    <div className="tp-screen-form-page tp-leave-approval-rejection-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="approval" size={18} />
          <h1>Leave Approval/Rejection List</h1>
        </div>
        <div className="tp-form-page-heading-actions">
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => loadPendingLeaveRequestRows(employeeCode, true)}
            disabled={loading || saving}
            title="Refresh Grid"
            aria-label="Refresh Grid"
          >
            <SvgIcon name="refresh" size={18} />
          </button>
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => closeTab(NavigateLeaveApprovalRejectionList)}
            disabled={loading || saving}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>

      <div className="tp-card tp-form-card tp-form-card-attached tp-leave-approval-rejection-list-card">
        <div className="row g-3 tp-leave-approval-rejection-list-filters">
          <div className="col-md-3">
            <SearchableDropdown
              name="employeeCode"
              label="Employee"
              required
              value={employeeCode}
              onChange={updateEmployee}
              options={employeeOptions}
              columns={[{ key: "label", label: "Employee" }]}
              placeholder="Select employee"
              disabled={loading || saving}
            />
          </div>
        </div>
        <div className="tp-leave-approval-rejection-list-grid">
          <SortableTable
            refreshTrigger={gridRefreshTrigger}
            columns={columns}
            data={rows}
            keyField="id"
            emptyText="Leave Request List Not Found."
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={1}
            defaultPageSize={30}
            maxHeight={420}
            showActions={canApproveLeave}
            actionColumnWidth="90px"
            showRowMore={false}
            showViewAction={false}
            showEditAction={false}
            showDeleteAction={false}
            renderActions={renderActions}
            loading={loading}
            loadingText="Loading Leave Approval/Rejection List..."
          />
        </div>
      </div>

      <LeaveApproveRejectModal
        show={canApproveLeave && Boolean(approveRejectRow)}
        row={approveRejectRow}
        approvalOptions={approvalOptions}
        saving={saving}
        apiFieldErrors={apiFieldErrors}
        onViewLeaveDetails={setLeaveDetailsRow}
        onSave={(form) => {
          if (canApproveLeave && approveRejectRow)
            setApprovalRequest({ row: approveRejectRow, form });
        }}
        onClose={() => {
          setApproveRejectRow(null);
          setApiFieldErrors({});
        }}
      />

      <ConfirmModal
        show={canApproveLeave && Boolean(approvalRequest)}
        title={isApprove ? "Approve Leave Request" : "Reject Leave Request"}
        message={
          isApprove
            ? "Are You Sure You Want To Approve This Leave Request ?"
            : "Are You Sure You Want To Reject This Leave Request ?"
        }
        confirmText="Yes, Confirm"
        cancelText="No"
        loading={saving}
        onConfirm={confirmApproveReject}
        onCancel={() => {
          if (!saving) setApprovalRequest(null);
        }}
      />

      <LeaveDetailsPopup
        show={Boolean(leaveDetailsRow)}
        row={leaveDetailsRow}
        onClose={() => setLeaveDetailsRow(null)}
      />

      <LeaveApprovedRejectedDetailsModal
        show={Boolean(approvedRejectedDetailsRow)}
        row={approvedRejectedDetailsRow}
        onClose={() => setApprovedRejectedDetailsRow(null)}
      />
    </div>
  );
}
