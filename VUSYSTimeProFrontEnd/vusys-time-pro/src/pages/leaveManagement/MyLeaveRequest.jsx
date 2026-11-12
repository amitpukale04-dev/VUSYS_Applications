import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import SortableTable from "../../components/common/SortableTable";
import SvgIcon from "../../components/common/SvgIcon";
import ConfirmModal from "../../components/common/ConfirmModal";
import LeaveApprovedRejectedDetailsModal from "./LeaveApprovedRejectedDetailsModal";
import LeaveRequestCancelModal from "./LeaveRequestCancelModal";
import AccessDenied from "../../components/common/AccessDenied";
import { useAuth } from "../../context/AuthContext";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import ToastService from "../../services/toastService";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { getLoggedInEmployeeCode } from "../../utils/userSession";
import { encodeRouteId } from "../../utils/routeIdUtils";
import {
  cancelMyLeaveRequest,
  getMyLeaveRequestList,
} from "../../services/myLeaveRequestService";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const NavigateMyLeaveRequest =
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS;
const NavigateApplyLeave = ROUTE_CONSTANTS.LEAVE_MANAGEMENT.APPLY_LEAVE;

const CANCEL_LEAVE_REQUEST_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  actionType: { apiKeys: ["ActionType"] },
  actionBy: { apiKeys: ["ActionBy"] },
  remarks: { apiKeys: ["Remarks"] },
};

// ============================================================
// MY LEAVE REQUEST LIST
// ============================================================

export default function MyLeaveRequest() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { closeTab } = useTabs();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();
  const employeeCode = getLoggedInEmployeeCode(user);
  const canOpenMyLeaveRequestList = hasPermission("My Leave Requests", "Open");
  const canApplyLeaveRequest = hasPermission("My Leave Requests", "Apply");
  const canViewLeaveRequest = hasPermission("My Leave Requests", "View");

  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(false);
  const [refresh, setRefreshed] = useState(false);
  const [cancelRow, setCancelRow] = useState(null);
  const [cancelReason, setCancelReason] = useState("");
  const [reasonError, setReasonError] = useState("");
  const [gridRefreshTrigger, setGridRefreshTrigger] = useState(0);
  const [cancelApiFieldErrors, setCancelApiFieldErrors] = useState({});
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [detailsRow, setDetailsRow] = useState(null);
  const initialLoadRef = useRef(false);

  const loadLeaveRequests = useCallback(
    async (showErrors = false) => {
      if (!employeeCode) return;
      setLoading(true);
      try {
        setRows(await getMyLeaveRequestList(employeeCode));
      } catch (error) {
        setRows([]);
        if (showErrors) {
          showApiError(error, "Unable to load My Leave Requests.");
        }
      } finally {
        setLoading(false);
      }
    },
    [employeeCode],
  );

  useEffect(() => {
    if (
      securityAccessLoading ||
      !canOpenMyLeaveRequestList ||
      initialLoadRef.current
    )
      return;
    initialLoadRef.current = true;
    void loadLeaveRequests(false);
  }, [canOpenMyLeaveRequestList, loadLeaveRequests, securityAccessLoading]);

  const refreshGrid = async () => {
    setGridRefreshTrigger((current) => current + 1);
    setRefreshed(true);
    await loadLeaveRequests(true);
  };

  const openCancel = (row) => {
    setCancelRow(row);
    setCancelReason("");
    setReasonError("");
    setCancelApiFieldErrors({});
  };

  const closeCancel = () => {
    if (cancelling) return;
    setCancelRow(null);
    setConfirmOpen(false);
    setReasonError("");
    setCancelApiFieldErrors({});
  };

  const requestCancel = () => {
    if (!cancelReason.trim()) {
      setReasonError("Enter The Cancellation Reason.");
      return;
    }
    setReasonError("");
    setConfirmOpen(true);
  };

  const confirmCancel = async () => {
    if (!cancelRow || cancelling) return;
    setCancelling(true);
    const loaderRequestId = showFullPageLoader("Cancelling Leave Request...");
    try {
      const message = await cancelMyLeaveRequest({
        id: cancelRow.id,
        employeeCode,
        remarks: cancelReason.trim(),
      });
      ToastService.success(message);
      setConfirmOpen(false);
      setCancelRow(null);
      setCancelApiFieldErrors({});
      await loadLeaveRequests(false);
    } catch (error) {
      setConfirmOpen(false);
      if (error?.isApiValidationError) {
        setCancelApiFieldErrors({});
        applyApiValidationErrors(
          error.validationErrors || [],
          CANCEL_LEAVE_REQUEST_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setCancelApiFieldErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to cancel Leave Request.");
      }
    } finally {
      hideFullPageLoader(loaderRequestId);
      setCancelling(false);
    }
  };

  // ==========================================================
  // GRID COLUMNS / ACTIONS
  // ==========================================================

  const columns = useMemo(
    () => [
      { key: "slNo", label: "SL No", width: "115px" },
      { key: "leaveType", label: "Leave Type", width: "220px" },
      { key: "fromDate", label: "From Date", width: "160px" },
      { key: "toDate", label: "To Date", width: "160px" },
      {
        key: "totalLeaveDays",
        label: "Leave Days",
        width: "155px",
      },
      { key: "leaveStatusText", label: "Leave Status", width: "190px" },
      {
        key: "approvedOrRejectedReason",
        label: "Approved/Rejected Details",
        width: "260px",
        render: (value, row) =>
          value?.trim() ? (
            <button
              type="button"
              className="tp-report-link-button"
              onClick={() => setDetailsRow(row)}
            >
              View Details
            </button>
          ) : (
            ""
          ),
      },
    ],
    [],
  );

  const renderActions = useCallback(
    (row) => (
      <div className="tp-row-actions">
        {canViewLeaveRequest && (
          <button
            type="button"
            className="tp-row-edit"
            title="View Leave Request"
            onClick={() =>
              navigate(`${NavigateMyLeaveRequest}/${encodeRouteId(row.id)}`, {
                state: { mode: "view" },
              })
            }
          >
            <SvgIcon name="eye" size={19} />
          </button>
        )}
        <button
          type="button"
          className="tp-row-edit"
          title={
            row.isShowCancelLeaveRequest
              ? "Cancel Leave Request"
              : "Cancellation Not Available"
          }
          onClick={() => openCancel(row)}
          disabled={!row.isShowCancelLeaveRequest}
        >
          <SvgIcon name="CancelLeaveRequestIcon" size={18} />
        </button>
      </div>
    ),
    [canViewLeaveRequest, navigate],
  );

  if (securityAccessLoading)
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" /> Loading security
        permissions...
      </div>
    );
  if (!canOpenMyLeaveRequestList) return <AccessDenied />;

  return (
    <div className="tp-screen-form-page tp-my-leave-request-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="leaveType" size={18} />
          <h1>My Leave Requests</h1>
        </div>
        <div className="tp-form-page-heading-actions">
          <button
            type="button"
            className="tp-form-page-icon-button"
            title="Refresh Grid"
            onClick={refreshGrid}
            disabled={loading}
          >
            <SvgIcon name="refresh" size={18} />
          </button>
          {canApplyLeaveRequest && (
            <button
              type="button"
              className="tp-form-page-icon-button"
              title="Apply Leave"
              onClick={() => navigate(NavigateApplyLeave)}
            >
              <SvgIcon name="ApplyLeaveRequestIcon" size={18} />
            </button>
          )}
          <button
            type="button"
            className="tp-form-page-icon-button"
            title="Cancel"
            onClick={() => closeTab(NavigateMyLeaveRequest)}
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>
      <div className="tp-card tp-log-timesheet-card tp-form-card-attached">
        <SortableTable
          refreshTrigger={gridRefreshTrigger}
          columns={columns}
          data={rows}
          keyField="id"
          enablePagination
          enableSorting
          enableGlobalSearch
          enableColumnFilters
          stickyHeader
          freezeColumns={1}
          defaultPageSize={30}
          maxHeight={420}
          showActions
          actionColumnWidth="100px"
          showViewAction={false}
          showEditAction={false}
          showDeleteAction={false}
          showRowMore={false}
          renderActions={renderActions}
          loading={loading}
          loadingText="Loading leave requests..."
        />
      </div>

      <LeaveRequestCancelModal
        show={Boolean(cancelRow)}
        row={cancelRow}
        reason={cancelReason}
        reasonError={reasonError || cancelApiFieldErrors.remarks}
        apiFieldErrors={cancelApiFieldErrors}
        loading={cancelling}
        onReasonChange={(value) => {
          setCancelReason(value);
          setReasonError("");
          setCancelApiFieldErrors((current) => ({ ...current, remarks: "" }));
        }}
        onCancel={closeCancel}
        onSubmit={requestCancel}
      />

      <LeaveApprovedRejectedDetailsModal
        show={Boolean(detailsRow)}
        row={detailsRow}
        onClose={() => setDetailsRow(null)}
      />

      <ConfirmModal
        show={confirmOpen}
        title="Cancel Leave Request"
        message="Are You Sure You Want To Cancel This Leave Request ?"
        onConfirm={confirmCancel}
        onCancel={() => setConfirmOpen(false)}
        loading={cancelling}
      />
    </div>
  );
}
