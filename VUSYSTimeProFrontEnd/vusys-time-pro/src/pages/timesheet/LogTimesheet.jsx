import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import SortableTable from "../../components/common/SortableTable";
import AccessDenied from "../../components/common/AccessDenied";
import SvgIcon from "../../components/common/SvgIcon";
import { DatePicker } from "../../components/forms/FormElements";
import { useAuth } from "../../context/AuthContext";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import useFormValidation from "../../hooks/useFormValidation";
import ToastService from "../../services/toastService";
import { getLogTimesheetList } from "../../services/logTimesheetService";
import { getLoggedInEmployeeCode } from "../../utils/userSession";
import { encodeRouteId } from "../../utils/routeIdUtils";
import { logTimesheetListValidationRules } from "../../validation/logTimesheetValidationRules";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const getMonthBounds = () => {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth();
  const first = new Date(year, month, 1);
  const last = new Date(year, month + 1, 0);
  const format = (date) =>
    `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(
      date.getDate(),
    ).padStart(2, "0")}`;
  return { fromDate: format(first), toDate: format(last) };
};

const LOG_TIMESHEET_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  fromDate: { apiKeys: ["FromDate"] },
  toDate: { apiKeys: ["ToDate"] },
};

const NavigateLogTimesheet = ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET;
const NavigateAddLogTimesheet =
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET_ADD;

export default function LogTimesheet() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { closeTab } = useTabs();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const employeeCode = getLoggedInEmployeeCode(user);

  const canOpenLogTimesheetList = hasPermission("Log Timesheet", "Open");
  const canLogNewTimesheet = hasPermission("Log Timesheet", "Log");
  const canEditTimesheet = hasPermission("Log Timesheet", "Edit");

  const [filters, setFilters] = useState(getMonthBounds);
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searched, setSearched] = useState(false);
  const [gridRefreshTrigger, setGridRefreshTrigger] = useState(0);
  const [allowAddTimesheet, setAllowAddTimesheet] = useState({
    allowed: true,
    message: "",
  });
  const initialLoadRef = useRef(false);

  const { errors, validate, clearError, setFieldError, clearErrors } =
    useFormValidation(logTimesheetListValidationRules);

  const validateDates = useCallback(() => {
    clearErrors();
    return validate(filters);
  }, [clearErrors, filters, validate]);

  const handleApiValidation = useCallback(
    (error) => {
      const validationErrors = error?.validationErrors || [];
      applyApiValidationErrors(
        validationErrors,
        LOG_TIMESHEET_API_FIELD_MAPPINGS,
        setFieldError,
      );

      // EmployeeCode belongs to the signed-in user and has no form control.
      // Show its API validation message rather than silently discarding it.
      const employeeCodeMessages = validationErrors
        .filter((item) =>
          /(?:^|\.)EmployeeCode$/i.test(
            String(item?.Key || item?.key || "").replace(/\[\d+\]/g, ""),
          ),
        )
        .map((item) => item?.Message || item?.message)
        .filter(Boolean);

      if (
        employeeCodeMessages.length &&
        !error?._globalToastHandled &&
        !error?.__globalToastHandled
      ) {
        ToastService.error(employeeCodeMessages.join("\n"));
      }
    },
    [setFieldError],
  );

  const loadRows = useCallback(
    async (
      resolvedFilters,
      { showErrors = true, loaderText = "Loading Timesheet..." } = {},
    ) => {
      const loaderId = showFullPageLoader(loaderText);
      setLoading(true);

      try {
        if (!employeeCode) {
          throw new Error("LoggedIn Employee Code Not Found.");
        }

        const result = await getLogTimesheetList({
          projectTimeSheetID: 0,
          employeeCode,
          fromDate: resolvedFilters.fromDate,
          toDate: resolvedFilters.toDate,
        });

        setRows(result.logRows);
        setAllowAddTimesheet({
          allowed: result.allowToAddTimeSheet !== false,
          message: result.allowToAddTimeSheetMessage || "",
        });
        setSearched(showErrors);
      } catch (error) {
        setRows([]);
        setAllowAddTimesheet({ allowed: true, message: "" });
        setSearched(showErrors);

        if (showErrors) {
          if (error?.isApiValidationError) {
            handleApiValidation(error);
          } else {
            showApiError(error, "Unable to load Log Timesheet.");
          }
        }
      } finally {
        setLoading(false);
        hideFullPageLoader(loaderId);
      }
    },
    [employeeCode, handleApiValidation, hideFullPageLoader, showFullPageLoader],
  );

  useEffect(() => {
    if (
      securityAccessLoading ||
      !canOpenLogTimesheetList ||
      initialLoadRef.current
    )
      return;
    initialLoadRef.current = true;
    loadRows(getMonthBounds(), {
      showErrors: false,
      loaderText: "Loading Log Timesheet...",
    });
  }, [canOpenLogTimesheetList, loadRows, securityAccessLoading]);

  const updateFilter = (event) => {
    const { name, value } = event.target;
    setFilters((current) => ({ ...current, [name]: value }));
    clearError(name);
  };

  const searchTimesheet = async () => {
    if (!canOpenLogTimesheetList) return;
    if (!validateDates()) return;
    await loadRows(filters, {
      showErrors: true,
      loaderText: "Searching Timesheet...",
    });
  };

  const refreshGrid = async () => {
    if (!canOpenLogTimesheetList) return;
    setGridRefreshTrigger((current) => current + 1);
    clearErrors();
    await loadRows(filters, {
      showErrors: false,
      loaderText: "Refreshing Log Timesheet...",
    });
  };

  const editTimesheet = (row) => {
    if (!canEditTimesheet || !canOpenLogTimesheetList) return;
    if (row?.isApproved) {
      ToastService.error(
        row?.cannotEditAfterApproveMessage ||
          "You Cannot Edit The Timesheet. Since Timesheet is Already Approved !!!",
      );
      return;
    }

    if (row?.isSentForApproval) {
      ToastService.error(
        row?.cannotEditAfterSentForApprovalMessage ||
          "You Cannot Edit The Timesheet. Since Timesheet is Already Sent for Approval !!!",
      );
      return;
    }

    const encodedId = encodeRouteId(row.projectTimeSheetID);
    navigate(`${NavigateLogTimesheet}/${encodedId}`, {
      state: {
        mode: "edit",
        projectTimeSheetID: row.projectTimeSheetID,
        taskDate: row.taskDate,
        timesheetMonth: row.timeSheetMonth,
        timesheetYear: row.timeSheetYear,
      },
    });
  };

  const addTimesheet = () => {
    if (!canLogNewTimesheet || !canOpenLogTimesheetList) return;
    if (!allowAddTimesheet.allowed) {
      ToastService.error(
        allowAddTimesheet.message || "You Cannot Add Timesheet.",
      );
      return;
    }

    navigate(NavigateAddLogTimesheet);
  };

  const columns = useMemo(
    () => [
      { key: "slNo", label: "SL No", width: "115px" },
      { key: "clientName", label: "Client Name", width: "180px" },
      { key: "projectName", label: "Project Name", width: "200px" },
      { key: "displayTaskDate", label: "Task Date", width: "150px" },
      {
        key: "totalHoursWorked",
        label: "Hours Worked",
        width: "180px",
      },
      { key: "projectStatus", label: "Project Status", width: "190px" },
    ],
    [],
  );

  const renderActions = useCallback(
    (row) => (
      <div className="tp-row-actions">
        {canEditTimesheet && (
          <button
            type="button"
            className="tp-row-edit"
            title="Edit Timesheet"
            aria-label="Edit Timesheet"
            onClick={() => editTimesheet(row)}
          >
            <SvgIcon name="edit" size={18} />
          </button>
        )}
      </div>
    ),
    [canEditTimesheet],
  );

  if (securityAccessLoading)
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" /> Loading security
        permissions...
      </div>
    );
  if (!canOpenLogTimesheetList) return <AccessDenied />;

  return (
    <div className="tp-employee-form-page tp-log-timesheet-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="timeEntry" size={18} />
          <h1>Log Timesheet</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={refreshGrid}
            disabled={loading}
            title="Refresh Grid"
            aria-label="Refresh Grid"
          >
            <SvgIcon name="refresh" size={18} />
          </button>

          {canLogNewTimesheet && (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={addTimesheet}
              disabled={loading}
              title="Add Timesheet"
              aria-label="Add Timesheet"
            >
              <SvgIcon name="addTimesheet" size={18} />
            </button>
          )}

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={searchTimesheet}
            disabled={loading}
            title="Search Timesheet"
            aria-label="Search Timesheet"
          >
            <SvgIcon name="reportSearch" size={18} />
          </button>

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => closeTab(NavigateLogTimesheet)}
            disabled={loading}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>

      <div className="tp-card tp-log-timesheet-card tp-employee-form-card-attached">
        <div className="row g-3 tp-log-timesheet-filters">
          <div className="col-md-3">
            <DatePicker
              name="fromDate"
              label="From Date"
              required
              value={filters.fromDate}
              onChange={updateFilter}
              error={errors.fromDate}
            />
          </div>
          <div className="col-md-3">
            <DatePicker
              name="toDate"
              label="To Date"
              required
              value={filters.toDate}
              onChange={updateFilter}
              error={errors.toDate}
            />
          </div>
        </div>
        <div className="tp-log-timesheet-grid">
          <SortableTable
            refreshTrigger={gridRefreshTrigger}
            columns={columns}
            data={rows}
            keyField="listKey"
            emptyText={
              searched
                ? "Timesheet Not Found For The Selected From Date and To Date."
                : ""
            }
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={1}
            defaultPageSize={30}
            maxHeight={420}
            showActions
            actionColumnWidth="80px"
            showViewAction={false}
            showEditAction={false}
            showDeleteAction={false}
            showRowMore={false}
            renderActions={renderActions}
            loading={loading}
            loadingText="Loading Timesheet..."
          />
        </div>
      </div>
    </div>
  );
}
