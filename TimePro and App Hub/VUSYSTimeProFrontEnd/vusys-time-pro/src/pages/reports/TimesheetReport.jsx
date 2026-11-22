import { useEffect, useMemo, useRef, useState } from "react";
import SortableTable from "../../components/common/SortableTable";
import ConfirmModal from "../../components/common/ConfirmModal";
import TimesheetApprovalDetailsModal from "./TimesheetApprovalDetailsModal";
import AccessDenied from "../../components/common/AccessDenied";
import SvgIcon from "../../components/common/SvgIcon";
import { SearchableDropdown } from "../../components/forms/FormElements";
import ToastService from "../../services/toastService";
import { getDropdownItems } from "../../services/dropdownService";
import {
  getTimesheetReportList,
  sendTimesheetSubmissionReminderNotification,
} from "../../services/timesheetReportService";
import useFormValidation from "../../hooks/useFormValidation";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { timesheetReportValidationRules } from "../../validation/timesheetReportValidationRules";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { resolveDefaultFilters } from "../../utils/reportUtil";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const TIMESHEET_REPORT_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  timesheetMonth: { apiKeys: ["TimesheetMonth"] },
  timesheetYear: { apiKeys: ["TimesheetYear"] },
};

const blankFilters = {
  employeeCode: "0",
  timesheetMonth: "",
  timesheetYear: "",
};

const NavigateTimesheetReport = ROUTE_CONSTANTS.REPORTS.TIMESHEET_REPORT;

export default function TimesheetReport() {
  const { closeTab } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const canOpenReport = hasPermission("Timesheet Report", "Open");

  const [filters, setFilters] = useState(blankFilters);
  const [employeeOptions, setEmployeeOptions] = useState([]);
  const [monthOptions, setMonthOptions] = useState([]);
  const [yearOptions, setYearOptions] = useState([]);
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searched, setSearched] = useState(false);
  const [notifyRow, setNotifyRow] = useState(null);
  const [notifyLoading, setNotifyLoading] = useState(false);
  const [detailsRow, setDetailsRow] = useState(null);
  const dropdownLoadPromiseRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(timesheetReportValidationRules);

  useEffect(() => {
    if (securityAccessLoading || !canOpenReport) return undefined;

    let active = true;
    setLoading(true);

    if (!dropdownLoadPromiseRef.current) {
      dropdownLoadPromiseRef.current = Promise.all([
        getDropdownItems({
          sourceTable: DROPDOWN_SOURCE_TABLE.EMPLOYEE_PROJECT_WITH_ALL,
        }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.MONTH }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.YEAR }),
      ]);
    }

    const loadInitialReport = async () => {
      let loaderRequestId = null;

      try {
        const [employeeResult, monthResult, yearResult] =
          await dropdownLoadPromiseRef.current;

        if (!active) return;

        const employeeDropdownOptions = employeeResult.options;
        const monthDropdownOptions = monthResult.options;
        const yearDropdownOptions = yearResult.options;
        const defaultFilters = resolveDefaultFilters(
          employeeDropdownOptions,
          monthDropdownOptions,
          yearDropdownOptions,
          "Timesheet"
        );

        setEmployeeOptions(employeeDropdownOptions);
        setMonthOptions(monthDropdownOptions);
        setYearOptions(yearDropdownOptions);
        setFilters(defaultFilters);
        clearErrors();

        loaderRequestId = showFullPageLoader("Loading Timesheet Report...");

        const result = await getTimesheetReportList(defaultFilters);

        if (!active) return;

        setRows(result.reportRows);
        setSearched(false);
      } catch (error) {
        if (!active) return;

        setRows([]);
        setSearched(false);

        /*
         * This report is loaded automatically when the page opens.
         * Do not display API/validation errors during the initial load.
         * Errors are displayed only when the user clicks Search Report.
         */
      } finally {
        if (loaderRequestId) {
          hideFullPageLoader(loaderRequestId);
        }

        if (active) setLoading(false);
      }
    };

    loadInitialReport();

    return () => {
      active = false;
    };
  }, [
    canOpenReport,
    clearErrors,
    hideFullPageLoader,
    securityAccessLoading,
    showFullPageLoader,
  ]);

  const updateFilter = (event) => {
    const { name, value } = event.target;
    setFilters((current) => ({ ...current, [name]: value }));
    clearError(name);
  };

  const searchReport = async () => {
    if (!validate(filters)) return;

    const loaderRequestId = showFullPageLoader("Searching Timesheet Report...");
    setLoading(true);

    try {
      const result = await getTimesheetReportList(filters);
      setRows(result.reportRows);
      setSearched(true);
    } catch (error) {
      setRows([]);
      setSearched(true);

      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          TIMESHEET_REPORT_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to load Timesheet Report.");
      }
    } finally {
      setLoading(false);
      hideFullPageLoader(loaderRequestId);
    }
  };

  const confirmNotification = async () => {
    if (!notifyRow || notifyLoading) return;

    const loaderRequestId = showFullPageLoader(
      "Sending Timesheet Submission Notification...",
    );
    setNotifyLoading(true);

    try {
      const result = await sendTimesheetSubmissionReminderNotification({
        employeeCode: notifyRow.employeeCode,
        timesheetMonth: notifyRow.timeSheetMonth,
        timesheetYear: notifyRow.timeSheetYear,
      });

      ToastService.success(
        result.message ||
          "Timesheet Submission Notification sent successfully.",
      );
      setNotifyRow(null);
    } catch (error) {
      if (error?.isApiValidationError) {
        setNotifyRow(null);
        applyApiValidationErrors(
          error.validationErrors || [],
          TIMESHEET_REPORT_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(
          error,
          "Unable to send Timesheet Submission Notification.",
        );
      }
    } finally {
      setNotifyLoading(false);
      hideFullPageLoader(loaderRequestId);
    }
  };

  const columns = useMemo(
    () => [
      {
        key: "fullName",
        label: "Employee Name",
        width: "200px",
      },
      { key: "timeSheetMonth", label: "Month", width: "150px" },
      { key: "timeSheetYear", label: "Year", width: "150px" },
      {
        key: "totalHoursWorked",
        label: "Hours Worked",
        width: "180px",
      },
      {
        key: "timesheetApprovalStatus",
        label: "Approval Status",
        width: "200px",
      },
      {
        key: "approvedOrRejectedReason",
        label: "Approved/Rejected Details",
        width: "220px",
        sortable: false,
        filterable: false,
        render: (value, row) =>
          String(value || "").trim() ? (
            <button
              type="button"
              className="tp-report-link-button"
              onClick={() => setDetailsRow(row)}
            >
              View Details
            </button>
          ) : null,
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

  if (!canOpenReport) {
    return <AccessDenied />;
  }

  return (
    <div className="tp-screen-form-page tp-timesheet-report-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="report" size={17} />
          <h1>Timesheet Report</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={searchReport}
            disabled={loading}
            title="Search Report"
            aria-label="Search Report"
          >
            <SvgIcon name="reportSearch" size={19} />
          </button>
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => closeTab(NavigateTimesheetReport)}
            disabled={loading}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>

      <div className="tp-card tp-form-card tp-form-card-attached tp-timesheet-report-card">
        <div className="row g-3 tp-timesheet-report-filters">
          <div className="col-md-4">
            <SearchableDropdown
              name="employeeCode"
              label="Employee"
              required
              value={filters.employeeCode}
              onChange={updateFilter}
              options={employeeOptions}
              columns={[{ key: "label", label: "Employee" }]}
              error={errors.employeeCode}
              placeholder="Select employee"
              disabled={loading}
            />
          </div>
          <div className="col-md-4">
            <SearchableDropdown
              name="timesheetMonth"
              label="Month"
              required
              value={filters.timesheetMonth}
              onChange={updateFilter}
              options={monthOptions}
              columns={[{ key: "label", label: "Month" }]}
              error={errors.timesheetMonth}
              placeholder="Select month"
              disabled={loading}
            />
          </div>
          <div className="col-md-4">
            <SearchableDropdown
              name="timesheetYear"
              label="Year"
              required
              value={filters.timesheetYear}
              onChange={updateFilter}
              options={yearOptions}
              columns={[{ key: "label", label: "Year" }]}
              error={errors.timesheetYear}
              placeholder="Select year"
              disabled={loading}
            />
          </div>
        </div>

        <div className="tp-timesheet-report-grid">
          <SortableTable
            columns={columns}
            data={rows}
            keyField="projectTimeSheetID"
            emptyText={
              searched
                ? "Timesheet Report Not Found."
                : "Search to view Timesheet Report."
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
            actionColumnWidth="145px"
            showRowMore={false}
            showViewAction={false}
            showEditAction={false}
            showDeleteAction={false}
            renderActions={(row) =>
              row.isShowSendNotificationButton ? (
                <button
                  type="button"
                  className="tp-report-link-button"
                  onClick={() => setNotifyRow(row)}
                >
                  Notify To User
                </button>
              ) : null
            }
            loading={loading}
            loadingText="Loading Timesheet Report..."
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(notifyRow)}
        title="Send Timesheet Submission Notification"
        message="Are You Sure You Want To Send Timesheet Submission Notification ?"
        confirmText="Yes, Confirm"
        cancelText="No"
        loading={notifyLoading}
        onConfirm={confirmNotification}
        onCancel={() => setNotifyRow(null)}
      />

      <TimesheetApprovalDetailsModal
        show={Boolean(detailsRow)}
        row={detailsRow}
        onClose={() => setDetailsRow(null)}
      />
    </div>
  );
}
