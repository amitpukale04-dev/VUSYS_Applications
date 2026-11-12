import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import SortableTable from "../../components/common/SortableTable";
import ConfirmModal from "../../components/common/ConfirmModal";
import SvgIcon from "../../components/common/SvgIcon";
import { SearchableDropdown } from "../../components/forms/FormElements";
import { useAuth } from "../../context/AuthContext";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import useFormValidation from "../../hooks/useFormValidation";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { getDropdownItems } from "../../services/dropdownService";
import {
  getTimesheetSubmissionList,
  submitTimesheetDetailsForApproval,
} from "../../services/timesheetSubmissionService";
import ToastService from "../../services/toastService";
import { timesheetSubmissionValidationRules } from "../../validation/timesheetSubmissionValidationRules";
import TimesheetSubmissionDetailsModal from "./TimesheetSubmissionDetailsModal";
import { resolveCurrentMonth, resolveCurrentYear } from "../../utils/dateUtils";
import { normalizeText } from "../../utils/valueUtils";
import { getLoggedInEmployeeCode } from "../../utils/userSession";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const TIMESHEET_SUBMISSION_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  timesheetMonth: { apiKeys: ["TimesheetMonth"] },
  timesheetYear: { apiKeys: ["TimesheetYear"] },
};

const blankFilters = {
  timesheetMonth: "",
  timesheetYear: "",
};

const NavigateTimesheetSubmission =
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_SUBMISSION;

export default function TimesheetSubmission() {
  const { user } = useAuth();
  const { closeTab } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();
  const canOpenScreen = hasPermission("Timesheet Submission", "Open");

  const employeeCode = getLoggedInEmployeeCode(user);

  const [filters, setFilters] = useState(blankFilters);
  const [monthOptions, setMonthOptions] = useState([]);
  const [yearOptions, setYearOptions] = useState([]);
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searched, setSearched] = useState(false);
  const [approvalRequest, setApprovalRequest] = useState(null);
  const [approvalLoading, setApprovalLoading] = useState(false);
  const [detailsRow, setDetailsRow] = useState(null);
  const dropdownLoadPromiseRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(timesheetSubmissionValidationRules);

  const loadSubmissionRows = useCallback(
    async (
      resolvedFilters,
      loaderMessage = "Loading Timesheet Submission...",
      showErrorMessage = true,
    ) => {
      const loaderRequestId = showFullPageLoader(loaderMessage);
      setLoading(true);

      try {
        const result = await getTimesheetSubmissionList({
          employeeCode,
          timesheetMonth: resolvedFilters.timesheetMonth,
          timesheetYear: resolvedFilters.timesheetYear,
          paramType: "GetEmployeeProjectTimesheetForSubmission",
        });

        setRows(result.submissionRows);
        setSearched(showErrorMessage);
        return result;
      } catch (error) {
        setRows([]);
        setSearched(showErrorMessage);

        if (showErrorMessage) {
          if (error?.isApiValidationError) {
            applyApiValidationErrors(
              error.validationErrors || [],
              TIMESHEET_SUBMISSION_API_FIELD_MAPPINGS,
              setFieldError,
            );
            // EmployeeCode has no editable field on this screen.
            const employeeErrors = (error.validationErrors || []).filter(
              (item) =>
                String(item?.Key || item?.key || "").toLowerCase() ===
                "employeecode",
            );
            if (
              employeeErrors.length &&
              !error?._globalToastHandled &&
              !error?.__globalToastHandled
            ) {
              ToastService.error(
                employeeErrors
                  .map((item) => item?.Message || item?.message)
                  .filter(Boolean)
                  .join("\n"),
                "API Validations",
                { autoClose: 5000 },
              );
            }
          } else {
            showApiError(error, "Unable to load Timesheet Submission.");
          }
        }
        return null;
      } finally {
        setLoading(false);
        hideFullPageLoader(loaderRequestId);
      }
    },
    [employeeCode, setFieldError, hideFullPageLoader, showFullPageLoader],
  );

  useEffect(() => {
    if (securityAccessLoading || !canOpenScreen) return undefined;
    let active = true;

    if (!dropdownLoadPromiseRef.current) {
      dropdownLoadPromiseRef.current = Promise.all([
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.MONTH }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.YEAR }),
      ]);
    }

    const loadInitialData = async () => {
      setLoading(true);

      try {
        const [monthResult, yearResult] = await dropdownLoadPromiseRef.current;

        if (!active) return;

        const monthOption = resolveCurrentMonth(monthResult.options);
        const yearOption = resolveCurrentYear(yearResult.options);

        const defaultFilters = {
          timesheetMonth: String(monthOption?.value ?? ""),
          timesheetYear: String(yearOption?.value ?? new Date().getFullYear()),
        };

        setMonthOptions(monthResult.options);
        setYearOptions(yearResult.options);
        setFilters(defaultFilters);
        clearErrors();

        if (!employeeCode) {
          setRows([]);
          setSearched(true);
          ToastService.error("LoggedIn Employee Code Not Found.");
          return;
        }

        await loadSubmissionRows(
          defaultFilters,
          "Loading Timesheet Submission...",
          false,
        );
      } catch (error) {
        if (!active) return;

        setRows([]);
        setSearched(true);
        showApiError(error, "Unable to load Timesheet Submission dropdowns.");
      } finally {
        if (active) setLoading(false);
      }
    };

    loadInitialData();

    return () => {
      active = false;
    };
  }, [
    clearErrors,
    employeeCode,
    loadSubmissionRows,
    securityAccessLoading,
    canOpenScreen,
  ]);

  const updateFilter = (event) => {
    const { name, value } = event.target;
    setFilters((current) => ({ ...current, [name]: value }));
    clearError(name);
  };

  const searchSubmission = async () => {
    if (!validate(filters)) return;

    if (!employeeCode) {
      ToastService.error("LoggedIn Employee Code Not Found.");
      return;
    }

    await loadSubmissionRows(
      filters,
      "Searching Timesheet Submission...",
      true,
    );
  };

  const openApprovalConfirmation = (row, approveStatus) => {
    setApprovalRequest({ row, approveStatus });
  };

  const confirmApprovalSubmission = async () => {
    if (!approvalRequest || approvalLoading) return;

    const { row, approveStatus } = approvalRequest;
    const loaderRequestId = showFullPageLoader(
      approveStatus === "Resend For Approval"
        ? "Resending Timesheet For Approval..."
        : "Sending Timesheet For Approval...",
    );

    setApprovalLoading(true);

    try {
      const result = await submitTimesheetDetailsForApproval({
        employeeCode: row.employeeCode || employeeCode,
        timesheetMonth: row.timeSheetMonth,
        timesheetYear: row.timeSheetYear,
        projectTimeSheetID: row.projectTimeSheetID,
        approveStatus,
      });

      ToastService.success(
        result.message || "Timesheet submitted for approval successfully.",
      );

      setApprovalRequest(null);
      await loadSubmissionRows(filters, "Refreshing Timesheet Submission...");
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          TIMESHEET_SUBMISSION_API_FIELD_MAPPINGS,
          setFieldError,
        );
        // EmployeeCode has no editable field on this screen.
        const employeeErrors = (error.validationErrors || []).filter(
          (item) =>
            String(item?.Key || item?.key || "").toLowerCase() ===
            "employeecode",
        );
        if (
          employeeErrors.length &&
          !error?._globalToastHandled &&
          !error?.__globalToastHandled
        ) {
          ToastService.error(
            employeeErrors
              .map((item) => item?.Message || item?.message)
              .filter(Boolean)
              .join("\n"),
            "API Validations",
            { autoClose: 5000 },
          );
        }
      } else {
        showApiError(error, "Unable to submit Timesheet for approval.");
      }
    } finally {
      setApprovalLoading(false);
      hideFullPageLoader(loaderRequestId);
    }
  };

  const columns = useMemo(
    () => [
      {
        key: "timeSheetMonth",
        label: "Month",
        width: "130px",
      },
      {
        key: "timeSheetYear",
        label: "Year",
        width: "110px",
      },
      {
        key: "totalHoursWorked",
        label: "Hours Worked",
        width: "180px",
      },
      {
        key: "timesheetApprovalStatus",
        label: "Approval Status",
        width: "210px",
      },
      {
        key: "approvedOrRejectedReason",
        label: "Approved/Rejected Details",
        width: "240px",
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

  const renderActions = useCallback(
    (row) => {
      const approvalStatus = normalizeText(row.timesheetApprovalStatus);

      const canSendForApproval =
        approvalStatus === "not submitted" &&
        !row.isSentForApproval &&
        row.isShowSendForApprovalButton;

      const canResendForApproval =
        approvalStatus === "rejected" &&
        row.isSentForApproval &&
        row.isAvailableToResendForApproval;

      if (canSendForApproval) {
        return (
          <button
            type="button"
            className="tp-report-link-button"
            title="Click Here To Send For Approval"
            onClick={() => openApprovalConfirmation(row, "Send For Approval")}
            disabled={approvalLoading}
          >
            Send For Approval
          </button>
        );
      }

      if (canResendForApproval) {
        return (
          <button
            type="button"
            className="tp-report-link-button"
            title="Click Here To Send For Approval"
            onClick={() => openApprovalConfirmation(row, "Resend For Approval")}
            disabled={approvalLoading}
          >
            Resend For Approval
          </button>
        );
      }

      return null;
    },
    [approvalLoading],
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
    <div className="tp-screen-form-page tp-timesheet-submission-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="timesheetSubmit" size={18} />
          <h1>Timesheet Submission</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={searchSubmission}
            disabled={loading || approvalLoading}
            title="Search Timesheet Submission"
            aria-label="Search Timesheet Submission"
          >
            <SvgIcon name="reportSearch" size={19} />
          </button>

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => closeTab(NavigateTimesheetSubmission)}
            disabled={loading || approvalLoading}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>

      <div className="tp-card tp-form-card tp-form-card-attached tp-timesheet-submission-card">
        <div className="row g-3 tp-timesheet-submission-filters">
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
              disabled={loading || approvalLoading}
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
              disabled={loading || approvalLoading}
            />
          </div>
        </div>

        <div className="tp-timesheet-submission-grid">
          <SortableTable
            columns={columns}
            data={rows}
            keyField="projectTimeSheetID"
            emptyText={
              searched
                ? "Timesheet Submission Not Found."
                : "Search to view Timesheet Submission."
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
            actionColumnLabel="Actions"
            actionColumnWidth="190px"
            showRowMore={false}
            showViewAction={false}
            showEditAction={false}
            showDeleteAction={false}
            renderActions={renderActions}
            loading={loading}
            loadingText="Loading Timesheet Submission..."
          />
        </div>
      </div>

      <ConfirmModal
        show={Boolean(approvalRequest)}
        title={
          approvalRequest?.approveStatus === "Resend For Approval"
            ? "Resend Timesheet For Approval"
            : "Send Timesheet For Approval"
        }
        message="Are You Sure You Want To Send Timesheet For Approval ?"
        confirmText="Yes, Confirm"
        cancelText="No"
        loading={approvalLoading}
        onConfirm={confirmApprovalSubmission}
        onCancel={() => {
          if (!approvalLoading) setApprovalRequest(null);
        }}
      />

      <TimesheetSubmissionDetailsModal
        show={Boolean(detailsRow)}
        row={detailsRow}
        onClose={() => setDetailsRow(null)}
      />
    </div>
  );
}
