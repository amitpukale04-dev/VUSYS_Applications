import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import SortableTable from "../../components/common/SortableTable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import {
  deleteLeaveTypeMaster,
  getLeaveTypeList,
} from "../../services/leaveTypeService";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { encodeRouteId } from "../../utils/routeIdUtils";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const NavigateLeaveTypes = ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPES;
const NavigateAddLeaveType = ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPE_ADD;

export default function LeaveTypeMaster() {
  const navigate = useNavigate();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenLeaveTypeList = hasPermission("Leave Type Master", "Open");
  const canCreateLeaveType = hasPermission("Leave Type Master", "Create");
  const canEditLeaveType = hasPermission("Leave Type Master", "Edit");
  const canViewLeaveType = hasPermission("Leave Type Master", "View");

  const [leaveTypes, setLeaveTypes] = useState([]);
  const [loading, setLoading] = useState(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const initialLoadRef = useRef(false);

  const loadLeaveTypes = useCallback(async ({ showToast = false } = {}) => {
    setLoading(true);

    try {
      const result = await getLeaveTypeList();
      setLeaveTypes(result.leaveTypes || []);

      if (showToast) {
        ToastService.success("Leave Type list refreshed successfully.");
      }
    } catch (error) {
      setLeaveTypes([]);
      showApiError(error, "Unable to load Leave Type List.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (securityAccessLoading || !canOpenLeaveTypeList) return;
    if (initialLoadRef.current) return;

    initialLoadRef.current = true;
    loadLeaveTypes();
  }, [canOpenLeaveTypeList, loadLeaveTypes, securityAccessLoading]);

  const confirmDelete = async () => {
    if (!deleteRow || deleteLoading) return;

    setDeleteLoading(true);

    try {
      await deleteLeaveTypeMaster(deleteRow.leaveTypeId, loggedInUserId);
      ToastService.success("Leave Type Details Deleted Successfully.");
      setDeleteRow(null);
      await loadLeaveTypes();
    } catch (error) {
      showApiError(error, "Unable to delete Leave Type.");
    } finally {
      setDeleteLoading(false);
    }
  };

  // ==========================================================
  // GRID COLUMNS
  // ==========================================================

  const columns = useMemo(
    () => [
      { key: "leaveTypeCode", label: "Leave Type Code", width: "160px" },
      { key: "leaveType", label: "Leave Type", width: "200px" },
      {
        key: "defaultLeaveCount",
        label: "Default Leave Count",
        width: "180px",
      },
      {
        key: "isPaidLeave",
        label: "Is Paid Leave",
        width: "150px",
        render: (value) => (value ? "Yes" : "No"),
      },
      {
        key: "allowHalfDay",
        label: "Allow Half Day",
        width: "150px",
        render: (value) => (value ? "Yes" : "No"),
      },
      {
        key: "activeStatus",
        label: "Status",
        width: "130px",
        filterType: "select",
        filterOptions: ["Active", "Inactive"].map((value) => ({
          value,
          label: value,
        })),
        render: (value) => (
          <span
            className={`tp-status-pill ${
              String(value).toLowerCase() === "inactive" ? "inactive" : ""
            }`}
          >
            <i className="bi bi-circle-fill" />
            {value || "Inactive"}
          </span>
        ),
      },
    ],
    [],
  );

  if (securityAccessLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  if (!canOpenLeaveTypeList) {
    return <AccessDenied />;
  }

  return (
    <div className="tp-master-full-width">
      <div className="tp-card tp-grid-card tp-grid-card-modern">
        <div className="tp-grid-card-body">
          <SortableTable
            columns={columns}
            data={leaveTypes}
            keyField="leaveTypeId"
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={2}
            defaultPageSize={30}
            maxHeight={420}
            showActions
            actionColumnWidth="90px"
            showViewAction={canViewLeaveType}
            showEditAction={canEditLeaveType}
            showDeleteAction={canEditLeaveType}
            showAddAction={canCreateLeaveType}
            addActionLabel="Add Leave Type"
            addActionIconOnly
            onAdd={() => navigate(NavigateAddLeaveType)}
            showRefreshAction
            onRefresh={() => loadLeaveTypes({ showToast: false })}
            refreshing={loading}
            loading={loading}
            loadingText="Loading leave types..."
            onView={(row) =>
              navigate(
                `${NavigateLeaveTypes}/${encodeRouteId(row.leaveTypeId)}`,
                {
                  state: { mode: "view" },
                },
              )
            }
            onEdit={(row) =>
              navigate(
                `${NavigateLeaveTypes}/${encodeRouteId(row.leaveTypeId)}`,
                {
                  state: { mode: "edit" },
                },
              )
            }
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Leave Type"
        message={`Are you sure you want to delete ${
          deleteRow?.leaveType || deleteRow?.leaveTypeCode || "this leave type"
        }?`}
        confirmText="Yes, Delete"
        loading={deleteLoading}
        onConfirm={confirmDelete}
        onCancel={() => setDeleteRow(null)}
      />
    </div>
  );
}
