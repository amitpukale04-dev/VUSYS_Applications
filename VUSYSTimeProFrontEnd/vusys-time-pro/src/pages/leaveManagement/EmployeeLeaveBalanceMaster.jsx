import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import SortableTable from "../../components/common/SortableTable";
import ConfirmModal from "../../components/common/ConfirmModal";
import AccessDenied from "../../components/common/AccessDenied";
import SvgIcon from "../../components/common/SvgIcon";
import { SearchableDropdown } from "../../components/forms/FormElements";
import ToastService from "../../services/toastService";
import { getDropdownItems } from "../../services/dropdownService";
import {
  deleteEmployeeLeaveBalance,
  getEmployeeLeaveBalanceList,
  saveEmployeeLeaveBalanceInitialize,
} from "../../services/employeeLeaveBalanceService";
import EmployeeLeaveBalanceInitializeModal from "./EmployeeLeaveBalanceInitializeModal";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useTabs } from "../../context/TabsContext";
import { encodeRouteId } from "../../utils/routeIdUtils";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const INITIALIZE_LEAVE_BALANCE_API_FIELD_MAPPINGS = {
  leaveYear: { apiKeys: ["LeaveYear"] },
  createdBy: { apiKeys: ["CreatedBy"] },
};

const currentYear = String(new Date().getFullYear());
const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateEmployeeLeaveBalance =
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE;
const NavigateAddEmployeeLeaveBalance =
  ROUTE_CONSTANTS.LEAVE_MANAGEMENT.EMPLOYEE_LEAVE_BALANCE_ADD;

export default function EmployeeLeaveBalanceMaster() {
  const navigate = useNavigate();
  const { closeTab } = useTabs();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenLeaveBalanceList = hasPermission(
    "Employee Leave Balance",
    "Open",
  );
  const canCreateLeaveBalance = hasPermission(
    "Employee Leave Balance",
    "Create",
  );
  const canEditLeaveBalance = hasPermission("Employee Leave Balance", "Edit");
  const canViewLeaveBalance = hasPermission("Employee Leave Balance", "View");

  const [leaveYear, setLeaveYear] = useState(currentYear);
  const [years, setYears] = useState([]);
  const [leaveBalances, setLeaveBalances] = useState([]);
  const [loading, setLoading] = useState(false);
  const [deleteRow, setDeleteRow] = useState(null);
  const [gridRefreshTrigger, setGridRefreshTrigger] = useState(0);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [initializeOpen, setInitializeOpen] = useState(false);
  const [initializeLoading, setInitializeLoading] = useState(false);
  const initialLoadRef = useRef(false);

  // ==========================================================
  // LOAD EMPLOYEE LEAVE BALANCE LIST
  // ==========================================================

  const loadLeaveBalances = useCallback(
    async (selectedYear, showErrors = false) => {
      if (!selectedYear) return;
      setGridRefreshTrigger((current) => current + 1);
      setLoading(true);
      try {
        const result = await getEmployeeLeaveBalanceList({
          id: 0,
          employeeCode: "",
          leaveYear: Number(selectedYear),
        });
        setLeaveBalances(result.leaveBalances || []);
      } catch (error) {
        setLeaveBalances([]);
        if (showErrors) {
          showApiError(error, "Unable to load Employee Leave Balance List.");
        }
      } finally {
        setLoading(false);
      }
    },
    [],
  );

  useEffect(() => {
    if (securityAccessLoading || !canOpenLeaveBalanceList) return;
    if (initialLoadRef.current) return;

    /* Avoid duplicate initial API calls in React StrictMode. */
    initialLoadRef.current = true;

    getDropdownItems({ sourceTable: "Year" })
      .then((result) => setYears(result.options || []))
      .catch((error) => {
        showApiError(error, "Unable to load years.");
      });

    void loadLeaveBalances(currentYear);
  }, [canOpenLeaveBalanceList, loadLeaveBalances, securityAccessLoading]);

  // ==========================================================
  // INITIALIZE EMPLOYEE LEAVE BALANCE
  // ==========================================================

  const initializeLeaveBalance = async (selectedYear, setFieldError) => {
    if (initializeLoading) return;

    setInitializeLoading(true);

    try {
      const result = await saveEmployeeLeaveBalanceInitialize({
        leaveYear: Number(selectedYear),
      });

      ToastService.success(result.message);
      setInitializeOpen(false);
      setLeaveYear(String(selectedYear));
      await loadLeaveBalances(selectedYear);
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          INITIALIZE_LEAVE_BALANCE_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to initialize Employee Leave Balance.");
      }
    } finally {
      setInitializeLoading(false);
    }
  };

  // ==========================================================
  // DELETE EMPLOYEE LEAVE BALANCE
  // ==========================================================

  const confirmDelete = async () => {
    if (!deleteRow || deleteLoading) return;

    setDeleteLoading(true);

    try {
      await deleteEmployeeLeaveBalance(
        deleteRow.employeeLeaveBalanceId,
        loggedInUserId,
      );
      ToastService.success("Employee Leave Balance Deleted Successfully.");
      setDeleteRow(null);
      await loadLeaveBalances(leaveYear);
    } catch (error) {
      showApiError(error, "Unable to delete Employee Leave Balance.");
    } finally {
      setDeleteLoading(false);
    }
  };

  // ==========================================================
  // GRID COLUMNS
  // ==========================================================

  const columns = useMemo(
    () => [
      { key: "employeeCode", label: "Employee Code", width: "180px" },
      { key: "employeeName", label: "Employee Name", width: "190px" },
      { key: "leaveType", label: "Leave Type", width: "150px" },
      { key: "allocatedLeave", label: "Allocated Leave", width: "190px" },
      { key: "pendingLeave", label: "Pending Leave", width: "180px" },
      { key: "availableBalance", label: "Available Balance", width: "210px" },
      { key: "leaveYear", label: "Leave Year", width: "150px" },
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

  // ==========================================================
  // SECURITY ACCESS LOADING / DENIED
  // ==========================================================

  if (securityAccessLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  if (!canOpenLeaveBalanceList) {
    return <AccessDenied />;
  }

  // ==========================================================
  // UI
  // ==========================================================

  return (
    <div className="tp-employee-form-page tp-log-timesheet-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="leaveBalance" size={16} />
          <h1>Employee Leave Balance List</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => loadLeaveBalances(leaveYear, true)}
            disabled={loading}
            title="Refresh Grid"
            aria-label="Refresh Grid"
          >
            <SvgIcon name="refresh" size={18} />
          </button>

          {canCreateLeaveBalance && (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={() => navigate(NavigateAddEmployeeLeaveBalance)}
              disabled={loading}
              title="Add Employee Leave Balance"
              aria-label="Add Employee Leave Balance"
            >
              <SvgIcon name="add" size={18} />
            </button>
          )}

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => loadLeaveBalances(leaveYear, true)}
            disabled={loading}
            title="Search Leave Balance"
            aria-label="Search Leave Balance"
          >
            <SvgIcon name="reportSearch" size={18} />
          </button>

          {canCreateLeaveBalance && (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={() => setInitializeOpen(true)}
              disabled={loading}
              title="Initialize Leave Balance"
              aria-label="Initialize Leave Balance"
            >
              <SvgIcon name="leaveBalance" size={18} />
            </button>
          )}

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => closeTab(NavigateEmployeeLeaveBalance)}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>

      <div className="tp-card tp-grid-card tp-grid-card-modern tp-employee-form-card-attached">
        <div className="tp-grid-card-body">
          <div className="row g-3 mb-3">
            <div className="col-md-3">
              <SearchableDropdown
                name="leaveYear"
                label="Year"
                required
                value={leaveYear}
                onChange={(event) => setLeaveYear(event.target.value)}
                options={
                  years.some((item) => String(item.value) === String(leaveYear))
                    ? years
                    : [
                        { value: String(leaveYear), label: String(leaveYear) },
                        ...years,
                      ]
                }
                columns={[{ key: "label", label: "Year" }]}
                placeholder="Search and Select Year"
                disabled={loading}
              />
            </div>
          </div>

          <SortableTable
            refreshTrigger={gridRefreshTrigger}
            columns={columns}
            data={leaveBalances}
            keyField="employeeLeaveBalanceId"
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
            showViewAction={canViewLeaveBalance}
            showEditAction={canEditLeaveBalance}
            showDeleteAction={canEditLeaveBalance}
            loading={loading}
            loadingText="Loading employee leave balances..."
            onView={(row) =>
              navigate(
                `${NavigateEmployeeLeaveBalance}/${encodeRouteId(row.employeeLeaveBalanceId)}`,
                {
                  state: { mode: "view" },
                },
              )
            }
            onEdit={(row) =>
              navigate(
                `${NavigateEmployeeLeaveBalance}/${encodeRouteId(row.employeeLeaveBalanceId)}`,
                {
                  state: { mode: "edit" },
                },
              )
            }
            onDelete={(row) => setDeleteRow(row)}
          />
        </div>
      </div>

      <EmployeeLeaveBalanceInitializeModal
        show={initializeOpen}
        years={years}
        saving={initializeLoading}
        onInitialize={initializeLeaveBalance}
        onClose={() => {
          if (!initializeLoading) setInitializeOpen(false);
        }}
      />

      <ConfirmModal
        show={Boolean(deleteRow)}
        title="Delete Employee Leave Balance"
        message={`Are you sure you want to delete the leave balance for ${
          deleteRow?.employeeName || deleteRow?.employeeCode || "this employee"
        }?`}
        confirmText="Yes, Delete"
        loading={deleteLoading}
        onConfirm={confirmDelete}
        onCancel={() => setDeleteRow(null)}
      />
    </div>
  );
}
