import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useEffect, useMemo, useRef, useState } from "react";
import SortableTable from "../../components/common/SortableTable";
import SvgIcon from "../../components/common/SvgIcon";
import { SearchableDropdown } from "../../components/forms/FormElements";
import { getDropdownItems } from "../../services/dropdownService";
import { getEmployeeLeaveReportList } from "../../services/leaveReportService";
import useFormValidation from "../../hooks/useFormValidation";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { leaveReportValidationRules } from "../../validation/leaveReportValidationRules";
import { exportBase64File } from "../../utils/fileExportUtils";
import { resolveDefaultFilters } from "../../utils/reportUtil";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const LEAVE_REPORT_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  leaveMonth: { apiKeys: ["LeaveMonth"] },
  leaveYear: { apiKeys: ["LeaveYear"] },
  action: { apiKeys: ["Action"] },
};

const blankFilters = {
  employeeCode: "0",
  leaveMonth: "",
  leaveYear: "",
};

const NavigateLeaveReport = ROUTE_CONSTANTS.REPORTS.LEAVE_REPORT;

export default function LeaveReport() {
  const { closeTab } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();
  const canOpenScreen = hasPermission("Leave Report", "Open");

  const [filters, setFilters] = useState(blankFilters);
  const [employeeOptions, setEmployeeOptions] = useState([]);
  const [monthOptions, setMonthOptions] = useState([]);
  const [yearOptions, setYearOptions] = useState([]);
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);
  const [searched, setSearched] = useState(false);
  const [actionError, setActionError] = useState("");
  const dropdownLoadPromiseRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(leaveReportValidationRules);

  useEffect(() => {
    if (securityAccessLoading || !canOpenScreen) return undefined;
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
          "Leave"
        );

        setEmployeeOptions(employeeDropdownOptions);
        setMonthOptions(monthDropdownOptions);
        setYearOptions(yearDropdownOptions);
        setFilters(defaultFilters);
        clearErrors();

        loaderRequestId = showFullPageLoader("Loading Leave Report...");

        const result = await getEmployeeLeaveReportList({
          ...defaultFilters,
          action: "Search",
        });

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
    clearErrors,
    hideFullPageLoader,
    showFullPageLoader,
    securityAccessLoading,
    canOpenScreen,
  ]);

  const updateFilter = (event) => {
    const { name, value } = event.target;
    setFilters((current) => ({ ...current, [name]: value }));
    clearError(name);
    setActionError("");
  };

  const searchReport = async () => {
    setActionError("");
    if (!validate(filters)) return;

    const loaderRequestId = showFullPageLoader("Searching Leave Report...");
    setLoading(true);

    try {
      const result = await getEmployeeLeaveReportList({
        ...filters,
        action: "Search",
      });

      setRows(result.reportRows);
      setSearched(true);
    } catch (error) {
      setRows([]);
      setSearched(true);

      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          LEAVE_REPORT_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            if (fieldName === "action") {
              setActionError(message);
            } else {
              setFieldError(fieldName, message);
            }
          },
        );
      } else {
        showApiError(error, "Unable to load Leave Report.");
      }
    } finally {
      setLoading(false);
      hideFullPageLoader(loaderRequestId);
    }
  };

  const exportReport = async () => {
    setActionError("");
    if (!validate(filters) || exporting) return;

    const loaderRequestId = showFullPageLoader("Exporting Leave Report...");
    setExporting(true);

    try {
      const result = await getEmployeeLeaveReportList({
        ...filters,
        action: "Export",
      });

      exportBase64File({
        base64Content: result.exportDetails.fileBase64,
        contentType: result.exportDetails.contentType,
        fileName: result.exportDetails.fileName,
      });
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          LEAVE_REPORT_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            if (fieldName === "action") {
              setActionError(message);
            } else {
              setFieldError(fieldName, message);
            }
          },
        );
      } else {
        showApiError(error, "Unable to export Leave Report.");
      }
    } finally {
      setExporting(false);
      hideFullPageLoader(loaderRequestId);
    }
  };

  const columns = useMemo(
    () => [
      { key: "slNo", label: "SL No.", width: "120px" },
      { key: "fullName", label: "Employee Name", width: "200px" },
      { key: "emailID", label: "Email ID", width: "260px" },
      { key: "onLeave", label: "On Leave", width: "140px" },
      { key: "holiday", label: "Holiday", width: "140px" },
      { key: "compOff", label: "Compoff", width: "140px" },
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

  if (!canOpenScreen) return <AccessDenied />;

  return (
    <div className="tp-employee-form-page tp-leave-report-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="calendar" size={17} />
          <h1>Leave Report</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={searchReport}
            disabled={loading || exporting}
            title="Search Report"
            aria-label="Search Report"
          >
            <SvgIcon name="reportSearch" size={19} />
          </button>
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={exportReport}
            disabled={loading || exporting}
            title="Export To Excel"
            aria-label="Export To Excel"
          >
            <SvgIcon name="exportExcel" size={19} />
          </button>
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => closeTab(NavigateLeaveReport)}
            disabled={loading || exporting}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>

      <div className="tp-card tp-employee-form-card tp-employee-form-card-attached tp-leave-report-card">
        {actionError && (
          <div className="alert alert-danger" role="alert">
            {actionError}
          </div>
        )}
        <div className="row g-3 tp-leave-report-filters">
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
              disabled={loading || exporting}
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="leaveMonth"
              label="Month"
              required
              value={filters.leaveMonth}
              onChange={updateFilter}
              options={monthOptions}
              columns={[{ key: "label", label: "Month" }]}
              error={errors.leaveMonth}
              placeholder="Select month"
              disabled={loading || exporting}
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="leaveYear"
              label="Year"
              required
              value={filters.leaveYear}
              onChange={updateFilter}
              options={yearOptions}
              columns={[{ key: "label", label: "Year" }]}
              error={errors.leaveYear}
              placeholder="Select year"
              disabled={loading || exporting}
            />
          </div>
        </div>

        <div className="tp-leave-report-grid">
          <SortableTable
            columns={columns}
            data={rows}
            keyField="slNo"
            emptyText={
              searched
                ? "Leave Report Not Found."
                : "Search to view Leave Report."
            }
            enablePagination
            enableSorting
            enableGlobalSearch
            enableColumnFilters
            stickyHeader
            freezeColumns={1}
            defaultPageSize={30}
            maxHeight={420}
            loading={loading}
            loadingText="Loading Leave Report..."
          />
        </div>
      </div>
    </div>
  );
}
