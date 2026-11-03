import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import ConfirmModal from "../../components/common/ConfirmModal";
import SortableTable from "../../components/common/SortableTable";
import SvgIcon from "../../components/common/SvgIcon";
import { SearchableDropdown } from "../../components/forms/FormElements";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import useFormValidation from "../../hooks/useFormValidation";
import { getDropdownItems } from "../../services/dropdownService";
import {
  approveOrRejectTimesheet,
  getTimesheetApprovalRejectionList,
} from "../../services/timesheetApprovalRejectionService";
import { getOrExportEmployeeTimesheetList } from "../../services/downloadTimesheetService";
import ToastService from "../../services/toastService";
import { exportBase64File } from "../../utils/fileExportUtils";
import { timesheetApprovalRejectionFilterValidationRules } from "../../validation/timesheetApprovalRejectionValidationRules";
import EmployeeTimesheetDetailsModal from "./EmployeeTimesheetDetailsModal";
import TimesheetApproveRejectModal from "./TimesheetApproveRejectModal";
import TimesheetSubmissionDetailsModal from "./TimesheetSubmissionDetailsModal";
import { normalizeText } from "../../utils/valueUtils";
import { resolveCurrentMonth, resolveCurrentYear } from "../../utils/dateUtils";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const blankFilters = {
  employeeCode: "",
  timesheetMonth: "",
  timesheetYear: "",
};

const NavigateTimesheetApprovalRejectionList =
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_APPROVAL_REJECTION_LIST;

const resolveAllEmployee = (options = []) =>
  options.find(
    (option) =>
      normalizeText(option?.value) === "0" ||
      normalizeText(option?.label) === "all",
  );

const TIMESHEET_APPROVAL_LIST_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  timesheetMonth: { apiKeys: ["TimesheetMonth"] },
  timesheetYear: { apiKeys: ["TimesheetYear"] },
};

const TIMESHEET_APPROVE_REJECT_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  timesheetMonth: { apiKeys: ["TimesheetMonth"] },
  timesheetYear: { apiKeys: ["TimesheetYear"] },
  remarks: { apiKeys: ["Remarks"] },
  approvalStatus: { apiKeys: ["ApproveStatus"] },
};

function TimesheetRowActionMenu({
  anchorRect,
  row,
  onClose,
  onApproveReject,
  onDownload,
  canApproveTimesheet,
}) {
  useEffect(() => {
    if (!anchorRect) return undefined;

    const close = () => onClose?.();
    window.addEventListener("resize", close);
    window.addEventListener("scroll", close, true);

    return () => {
      window.removeEventListener("resize", close);
      window.removeEventListener("scroll", close, true);
    };
  }, [anchorRect, onClose]);

  if (!anchorRect || !row) return null;

  const style = {
    position: "fixed",
    top: Math.min(anchorRect.bottom + 7, window.innerHeight - 112),
    left: Math.min(Math.max(12, anchorRect.left - 30), window.innerWidth - 150),
    zIndex: 4000,
  };

  return createPortal(
    <>
      <button
        type="button"
        className="tp-action-menu-backdrop"
        aria-label="Close action menu"
        onClick={onClose}
      />

      <div
        className="tp-row-action-menu tp-timesheet-approval-action-menu"
        style={style}
        role="menu"
      >
        {canApproveTimesheet &&
          normalizeText(row.timesheetApprovalStatus) ===
            "pending for approval" && (
            <button
              type="button"
              role="menuitem"
              onClick={() => {
                onApproveReject?.(row);
                onClose?.();
              }}
            >
              <SvgIcon name="timesheetApproval" size={16} />
              <span>Approve / Reject</span>
            </button>
          )}

        <button
          type="button"
          role="menuitem"
          onClick={() => {
            onDownload?.(row);
            onClose?.();
          }}
        >
          <SvgIcon name="downloadTimesheet" size={16} />
          <span>Download</span>
        </button>
      </div>
    </>,
    document.body,
  );
}

export default function TimesheetApprovalRejectionList() {
  const { closeTab } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();
  const canOpenScreen = hasPermission("Approval/Rejection List", "Open");
  const canApproveTimesheet = hasPermission(
    "Approval/Rejection List",
    "Approve",
  );

  const [filters, setFilters] = useState(blankFilters);
  const [employeeOptions, setEmployeeOptions] = useState([]);
  const [monthOptions, setMonthOptions] = useState([]);
  const [yearOptions, setYearOptions] = useState([]);
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searched, setSearched] = useState(false);

  const [actionMenu, setActionMenu] = useState({
    row: null,
    rect: null,
  });
  const [approveRejectRow, setApproveRejectRow] = useState(null);
  const [approveRejectFieldErrors, setApproveRejectFieldErrors] = useState({});
  const [approvalRequest, setApprovalRequest] = useState(null);
  const [approvalSaving, setApprovalSaving] = useState(false);
  const [timesheetDetailsRows, setTimesheetDetailsRows] = useState([]);
  const [showTimesheetDetails, setShowTimesheetDetails] = useState(false);
  const [approvedRejectedDetailsRow, setApprovedRejectedDetailsRow] =
    useState(null);

  const dropdownLoadPromiseRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(timesheetApprovalRejectionFilterValidationRules);

  const loadRows = useCallback(
    async (
      resolvedFilters,
      loaderMessage = "Loading Timesheet Approval/Rejection List...",
      showErrorMessage = true,
    ) => {
      const loaderRequestId = showFullPageLoader(loaderMessage);
      setLoading(true);

      try {
        const result = await getTimesheetApprovalRejectionList({
          employeeCode: resolvedFilters.employeeCode,
          timesheetMonth: resolvedFilters.timesheetMonth,
          timesheetYear: resolvedFilters.timesheetYear,
          paramType: "GetAllEmployeeProjectTimesheetForApproval",
        });

        setRows(result.approvalRows);
        setSearched(showErrorMessage);
        return result;
      } catch (error) {
        setRows([]);
        setSearched(showErrorMessage);

        /*
         * The page performs an automatic initial API load. Do not show
         * "Timesheet Approval List Not Found." (or API validation messages)
         * during that initial load. API errors are shown only after the user
         * explicitly clicks Search Report.
         */
        if (showErrorMessage) {
          if (error?.isApiValidationError) {
            applyApiValidationErrors(
              error.validationErrors || [],
              TIMESHEET_APPROVAL_LIST_API_FIELD_MAPPINGS,
              setFieldError,
            );
          } else {
            showApiError(
              error,
              "Unable to load Timesheet Approval/Rejection List.",
            );
          }
        }
        return null;
      } finally {
        setLoading(false);
        hideFullPageLoader(loaderRequestId);
      }
    },
    [hideFullPageLoader, setFieldError, showFullPageLoader],
  );

  useEffect(() => {
    if (securityAccessLoading || !canOpenScreen) return undefined;
    let active = true;

    if (!dropdownLoadPromiseRef.current) {
      dropdownLoadPromiseRef.current = Promise.all([
        getDropdownItems({
          sourceTable: DROPDOWN_SOURCE_TABLE.EMPLOYEE_PROJECT_WITH_ALL,
        }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.MONTH }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.YEAR }),
      ]);
    }

    const loadInitialData = async () => {
      setLoading(true);

      try {
        const [employeeResult, monthResult, yearResult] =
          await dropdownLoadPromiseRef.current;

        if (!active) return;

        const employeeOption = resolveAllEmployee(employeeResult.options);
        const monthOption = resolveCurrentMonth(monthResult.options);
        const yearOption = resolveCurrentYear(yearResult.options);

        const defaultFilters = {
          employeeCode: String(employeeOption?.value ?? "0"),
          timesheetMonth: String(monthOption?.value ?? ""),
          timesheetYear: String(yearOption?.value ?? new Date().getFullYear()),
        };

        setEmployeeOptions(employeeResult.options);
        setMonthOptions(monthResult.options);
        setYearOptions(yearResult.options);
        setFilters(defaultFilters);
        clearErrors();

        await loadRows(
          defaultFilters,
          "Loading Timesheet Approval/Rejection List...",
          false,
        );
      } catch (error) {
        if (!active) return;

        setRows([]);
        setSearched(true);
        showApiError(
          error,
          "Unable to load Timesheet Approval/Rejection dropdowns.",
        );
      } finally {
        if (active) setLoading(false);
      }
    };

    loadInitialData();

    return () => {
      active = false;
    };
  }, [clearErrors, loadRows, securityAccessLoading, canOpenScreen]);

  const updateFilter = (event) => {
    const { name, value } = event.target;
    setFilters((current) => ({ ...current, [name]: value }));
    clearError(name);
  };

  const searchReport = async () => {
    if (!validate(filters)) return;

    await loadRows(
      filters,
      "Searching Timesheet Approval/Rejection List...",
      true,
    );
  };

  const handleDownload = useCallback(
    async (row) => {
      const loaderRequestId = showFullPageLoader("Downloading Timesheet...");

      try {
        const result = await getOrExportEmployeeTimesheetList({
          employeeCode: row.employeeCode,
          timesheetMonth: row.timeSheetMonth,
          timesheetYear: row.timeSheetYear,
          projectTimeSheetID: 0,
          action: "Export",
        });

        exportBase64File({
          base64Content: result.exportDetails.fileBase64,
          contentType: result.exportDetails.contentType,
          fileName: result.exportDetails.fileName,
        });
      } catch (error) {
        showApiError(error, "Unable to download Timesheet.");
      } finally {
        hideFullPageLoader(loaderRequestId);
      }
    },
    [hideFullPageLoader, showFullPageLoader],
  );

  const handleViewTimesheet = useCallback(
    async (row) => {
      const loaderRequestId = showFullPageLoader(
        "Loading Timesheet Details...",
      );

      try {
        const result = await getOrExportEmployeeTimesheetList({
          employeeCode: row.employeeCode,
          timesheetMonth: row.timeSheetMonth,
          timesheetYear: row.timeSheetYear,
          projectTimeSheetID: 0,
          action: "View",
        });

        setTimesheetDetailsRows(result.timesheetRows);
        setShowTimesheetDetails(true);
      } catch (error) {
        setTimesheetDetailsRows([]);
        showApiError(error, "Unable to load Timesheet Details.");
      } finally {
        hideFullPageLoader(loaderRequestId);
      }
    },
    [hideFullPageLoader, showFullPageLoader],
  );

  const openApproveReject = (row) => {
    if (!canApproveTimesheet) return;
    setApproveRejectFieldErrors({});
    setApproveRejectRow(row);
  };

  const requestApprovalConfirmation = (form) => {
    if (!approveRejectRow || !canApproveTimesheet) return;

    setApproveRejectFieldErrors({});
    setApprovalRequest({
      row: approveRejectRow,
      form,
    });
  };

  const confirmApproveReject = async () => {
    if (!approvalRequest || approvalSaving || !canApproveTimesheet) return;

    const { row, form } = approvalRequest;
    const isApprove = form.approveStatus === "Approve";
    const loaderRequestId = showFullPageLoader(
      isApprove ? "Approving Timesheet..." : "Rejecting Timesheet...",
    );

    setApprovalSaving(true);

    try {
      const result = await approveOrRejectTimesheet({
        employeeCode: row.employeeCode,
        timesheetMonth: row.timeSheetMonth,
        timesheetYear: row.timeSheetYear,
        projectTimeSheetID: row.projectTimeSheetID,
        remarks: form.remarks,
        userToken: "",
        source: "",
        approveStatus: form.approveStatus,
      });

      ToastService.success(
        result.message ||
          (isApprove
            ? "Timesheet Approved Successfully."
            : "Timesheet Rejected Successfully."),
      );

      setApprovalRequest(null);
      setApproveRejectRow(null);
      setApproveRejectFieldErrors({});
      await loadRows(
        filters,
        "Refreshing Timesheet Approval/Rejection List...",
      );
    } catch (error) {
      setApprovalRequest(null);

      if (error?.isApiValidationError) {
        setApproveRejectFieldErrors({});
        applyApiValidationErrors(
          error.validationErrors || [],
          TIMESHEET_APPROVE_REJECT_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            setApproveRejectFieldErrors((current) => ({
              ...current,
              [fieldName]: message,
            }));
          },
        );
      } else {
        showApiError(error, "Unable to Approve/Reject Timesheet.");
      }
    } finally {
      setApprovalSaving(false);
      hideFullPageLoader(loaderRequestId);
    }
  };

  const columns = useMemo(
    () => [
      { key: "employeeCode", label: "Employee Code", width: "185px" },
      { key: "fullName", label: "Name", width: "190px" },
      { key: "timeSheetMonth", label: "Month", width: "130px" },
      { key: "timeSheetYear", label: "Year", width: "110px" },
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
    (row) => (
      <div className="tp-row-actions">
        <button
          type="button"
          className="tp-row-more"
          title="More actions"
          aria-label="More actions"
          onClick={(event) => {
            event.stopPropagation();
            setActionMenu({
              row,
              rect: event.currentTarget.getBoundingClientRect(),
            });
          }}
        >
          <SvgIcon name="more" size={20} />
        </button>
      </div>
    ),
    [],
  );

  const confirmMessage =
    approvalRequest?.form?.approveStatus === "Approve"
      ? "Are You Sure You Want To Approve This Timesheet ?"
      : "Are You Sure You Want To Reject This Timesheet ?";

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
    <div className="tp-employee-form-page tp-timesheet-approval-rejection-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="timesheetApproval" size={18} />
          <h1>Timesheet Approval/Rejection List</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={searchReport}
            disabled={loading || approvalSaving}
            title="Search Timesheet Approval/Rejection"
            aria-label="Search Timesheet Approval/Rejection"
          >
            <SvgIcon name="reportSearch" size={19} />
          </button>

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => closeTab(NavigateTimesheetApprovalRejectionList)}
            disabled={loading || approvalSaving}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>

      <div className="tp-card tp-employee-form-card tp-employee-form-card-attached tp-timesheet-approval-rejection-card">
        <div className="row g-3 tp-timesheet-approval-rejection-filters">
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
              disabled={loading || approvalSaving}
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
              disabled={loading || approvalSaving}
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
              disabled={loading || approvalSaving}
            />
          </div>
        </div>

        <div className="tp-timesheet-approval-rejection-grid">
          <SortableTable
            columns={columns}
            data={rows}
            keyField="projectTimeSheetID"
            emptyText={
              searched
                ? "Timesheet Approval List Not Found."
                : "Search to view Timesheet Approval/Rejection List."
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
            actionColumnWidth="90px"
            showRowMore={false}
            showViewAction={false}
            showEditAction={false}
            showDeleteAction={false}
            renderActions={renderActions}
            loading={loading}
            loadingText="Loading Timesheet Approval/Rejection List..."
          />
        </div>
      </div>

      <TimesheetRowActionMenu
        anchorRect={actionMenu.rect}
        row={actionMenu.row}
        onClose={() => setActionMenu({ row: null, rect: null })}
        onApproveReject={openApproveReject}
        canApproveTimesheet={canApproveTimesheet}
        onDownload={handleDownload}
      />

      <TimesheetApproveRejectModal
        show={Boolean(approveRejectRow)}
        row={approveRejectRow}
        saving={approvalSaving}
        apiFieldErrors={approveRejectFieldErrors}
        onViewTimesheet={handleViewTimesheet}
        onSave={requestApprovalConfirmation}
        onClose={() => {
          if (!approvalSaving) {
            setApproveRejectRow(null);
            setApproveRejectFieldErrors({});
          }
        }}
      />

      <ConfirmModal
        show={Boolean(approvalRequest)}
        title={
          approvalRequest?.form?.approveStatus === "Approve"
            ? "Approve Timesheet"
            : "Reject Timesheet"
        }
        message={confirmMessage}
        confirmText="Yes, Confirm"
        cancelText="No"
        loading={approvalSaving}
        onConfirm={confirmApproveReject}
        onCancel={() => {
          if (!approvalSaving) setApprovalRequest(null);
        }}
      />

      <EmployeeTimesheetDetailsModal
        show={showTimesheetDetails}
        rows={timesheetDetailsRows}
        onClose={() => {
          setShowTimesheetDetails(false);
          setTimesheetDetailsRows([]);
        }}
      />

      <TimesheetSubmissionDetailsModal
        show={Boolean(approvedRejectedDetailsRow)}
        row={approvedRejectedDetailsRow}
        onClose={() => setApprovedRejectedDetailsRow(null)}
      />
    </div>
  );
}
