import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useEffect, useRef, useState } from "react";
import SvgIcon from "../../components/common/SvgIcon";
import EmployeeTimesheetDetailsModal from "./EmployeeTimesheetDetailsModal";
import { SearchableDropdown } from "../../components/forms/FormElements";
import { useAuth } from "../../context/AuthContext";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import useFormValidation from "../../hooks/useFormValidation";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { getDropdownItems } from "../../services/dropdownService";
import { getOrExportEmployeeTimesheetList } from "../../services/downloadTimesheetService";
import ToastService from "../../services/toastService";
import { downloadTimesheetValidationRules } from "../../validation/downloadTimesheetValidationRules";
import { exportBase64File } from "../../utils/fileExportUtils";
import { normalizeText } from "../../utils/valueUtils";
import { resolveCurrentMonth, resolveCurrentYear } from "../../utils/dateUtils";
import {
  getLoggedInEmployeeCode,
  getLoggedInEmployeeName,
} from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const DOWNLOAD_TIMESHEET_API_FIELD_MAPPINGS = {
  employeeCode: { apiKeys: ["EmployeeCode"] },
  clientID: { apiKeys: ["ClientID", "ProjectID"] },
  timesheetMonth: { apiKeys: ["TimesheetMonth"] },
  timesheetYear: { apiKeys: ["TimesheetYear"] },
  action: { apiKeys: ["Action"] },
};

const blankFilters = {
  clientID: "0",
  employeeCode: "",
  timesheetMonth: "",
  timesheetYear: "",
};

const isAdminRole = (user) =>
  normalizeText(user?.role ?? user?.UserRole ?? user?.userRole) === "admin";

const NavigateDownloadTimesheet =
  ROUTE_CONSTANTS.MANAGE_TIMESHEET.DOWNLOAD_TIMESHEET;

export default function DownloadTimesheet() {
  const { user } = useAuth();
  const { closeTab } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();
  const canOpenScreen = hasPermission("Download Timesheet", "Open");
  const adminUser = isAdminRole(user);

  const [filters, setFilters] = useState(blankFilters);
  const [employeeOptions, setEmployeeOptions] = useState([]);
  const [clientOptions, setClientOptions] = useState([]);
  const [monthOptions, setMonthOptions] = useState([]);
  const [yearOptions, setYearOptions] = useState([]);
  const [loadingDropdowns, setLoadingDropdowns] = useState(true);
  const [processingAction, setProcessingAction] = useState("");
  const [timesheetRows, setTimesheetRows] = useState([]);
  const [showTimesheetModal, setShowTimesheetModal] = useState(false);
  const dropdownLoadPromiseRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(downloadTimesheetValidationRules);

  useEffect(() => {
    if (securityAccessLoading || !canOpenScreen) return undefined;
    let active = true;

    if (!dropdownLoadPromiseRef.current) {
      dropdownLoadPromiseRef.current = Promise.all([
        adminUser
          ? getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.ALL_CLIENT })
          : Promise.resolve({ options: [] }),
        getDropdownItems({
          sourceTable: DROPDOWN_SOURCE_TABLE.EMPLOYEE_PROJECT_WITH_ALL,
        }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.MONTH }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.YEAR }),
      ]);
    }

    const loadDropdowns = async () => {
      const loaderRequestId = showFullPageLoader(
        "Loading Download Timesheet...",
      );
      setLoadingDropdowns(true);

      try {
        const [clientResult, employeeResult, monthResult, yearResult] =
          await dropdownLoadPromiseRef.current;

        if (!active) return;

        const loggedInEmployeeCode = getLoggedInEmployeeCode(user);
        let resolvedEmployeeOptions = employeeResult.options;

        if (adminUser) {
          setClientOptions(clientResult.options);
        }

        if (!adminUser) {
          resolvedEmployeeOptions = employeeResult.options.filter(
            (option) =>
              normalizeText(option?.value) ===
              normalizeText(loggedInEmployeeCode),
          );

          if (!resolvedEmployeeOptions.length && loggedInEmployeeCode) {
            resolvedEmployeeOptions = [
              {
                value: loggedInEmployeeCode,
                label: getLoggedInEmployeeName(user),
              },
            ];
          }
        }

        const employeeOption = adminUser
          ? resolvedEmployeeOptions.find(
              (option) =>
                normalizeText(option?.value) === "0" ||
                normalizeText(option?.label) === "all",
            ) || resolvedEmployeeOptions[0]
          : resolvedEmployeeOptions[0];
        const monthOption = resolveCurrentMonth(monthResult.options);
        const yearOption = resolveCurrentYear(yearResult.options);

        setEmployeeOptions(resolvedEmployeeOptions);
        setMonthOptions(monthResult.options);
        setYearOptions(yearResult.options);
        const clientOption = clientResult.options.find(
          (option) =>
            normalizeText(option?.value) === "0" ||
            normalizeText(option?.label) === "all",
        );

        setFilters({
          clientID: String(clientOption?.value ?? "0"),
          employeeCode: String(
            employeeOption?.value ?? (adminUser ? "" : loggedInEmployeeCode),
          ),
          timesheetMonth: String(monthOption?.value ?? ""),
          timesheetYear: String(yearOption?.value ?? new Date().getFullYear()),
        });
        clearErrors();
      } catch (error) {
        if (!active) return;
        showApiError(error, "Unable to load Download Timesheet dropdowns.");
      } finally {
        hideFullPageLoader(loaderRequestId);
        if (active) setLoadingDropdowns(false);
      }
    };

    loadDropdowns();

    return () => {
      active = false;
    };
  }, [
    adminUser,
    clearErrors,
    hideFullPageLoader,
    showFullPageLoader,
    user,
    securityAccessLoading,
    canOpenScreen,
  ]);

  const updateFilter = (event) => {
    const { name, value } = event.target;
    setFilters((current) => ({ ...current, [name]: value }));
    clearError(name);
  };

  const executeTimesheetAction = async (action) => {
    if (processingAction) return;

    const isProjectWise = action === "ExportProjectWise";
    const isAllEmployee =
      normalizeText(filters.employeeCode) === "0" ||
      employeeOptions.some(
        (option) =>
          normalizeText(option?.value) ===
            normalizeText(filters.employeeCode) &&
          normalizeText(option?.label) === "all",
      );

    const isFormValid = validate(filters);

    if (!isProjectWise && isAllEmployee) {
      setFieldError("employeeCode", "Select the Specific Employee.");
    }

    if (isProjectWise && adminUser && !String(filters.clientID ?? "").trim()) {
      setFieldError("clientID", "Select The Client.");
    }

    if (
      !isFormValid ||
      (!isProjectWise && isAllEmployee) ||
      (isProjectWise && adminUser && !String(filters.clientID ?? "").trim())
    )
      return;

    const actionText =
      action === "View"
        ? "Loading Timesheet Details..."
        : action === "ExportDateWise"
          ? "Downloading DateWise Timesheet..."
          : action === "ExportProjectWise"
            ? "Downloading ProjectWise Timesheet..."
            : "Downloading Timesheet...";

    const loaderRequestId = showFullPageLoader(actionText);
    setProcessingAction(action);

    try {
      const result = await getOrExportEmployeeTimesheetList({
        employeeCode:
          isProjectWise && isAllEmployee ? "" : filters.employeeCode,
        timesheetMonth: filters.timesheetMonth,
        timesheetYear: filters.timesheetYear,
        projectID:
          isProjectWise && adminUser ? Number(filters.clientID || 0) : 0,
        projectTimeSheetID: 0,
        action,
      });

      if (action === "View") {
        setTimesheetRows(result.timesheetRows);
        setShowTimesheetModal(true);
      } else {
        exportBase64File({
          base64Content: result.exportDetails.fileBase64,
          contentType: result.exportDetails.contentType,
          fileName: result.exportDetails.fileName,
        });
      }
    } catch (error) {
      if (error?.isApiValidationError) {
        const validationErrors = error.validationErrors || [];
        applyApiValidationErrors(
          validationErrors,
          DOWNLOAD_TIMESHEET_API_FIELD_MAPPINGS,
          setFieldError,
        );

        // Action is not an editable input; show its API message in a toast.
        const actionErrors = validationErrors.filter((item) =>
          /^(?:\[\d+\]\.)?action$/i.test(
            String(item?.Key || item?.key || "").trim(),
          ),
        );
        if (
          actionErrors.length &&
          !error?._globalToastHandled &&
          !error?.__globalToastHandled
        ) {
          ToastService.error(
            actionErrors
              .map((item) => item?.Message || item?.message)
              .filter(Boolean)
              .join("\n"),
          );
        }
      } else {
        showApiError(
          error,
          action === "View"
            ? "Unable to load Timesheet Details."
            : "Unable to download Timesheet.",
        );
      }
    } finally {
      setProcessingAction("");
      hideFullPageLoader(loaderRequestId);
    }
  };

  const controlsDisabled = loadingDropdowns || Boolean(processingAction);

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
    <>
      <div className="tp-screen-form-page tp-download-timesheet-page">
        <div className="tp-form-page-heading tp-form-titlebar">
          <div className="tp-form-titlebar-title">
            <SvgIcon name="timesheet" size={17} />
            <h1>Download Timesheet</h1>
          </div>

          <div className="tp-form-page-heading-actions">
            {adminUser && (
              <button
                type="button"
                className="tp-form-page-icon-button"
                onClick={() => executeTimesheetAction("View")}
                disabled={controlsDisabled}
                title="View Timesheet"
                aria-label="View Timesheet"
              >
                <SvgIcon name="viewTimesheet" size={19} />
              </button>
            )}

            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={() => executeTimesheetAction("Export")}
              disabled={controlsDisabled}
              title="Download Timesheet"
              aria-label="Download Timesheet"
            >
              <SvgIcon name="downloadTimesheet" size={19} />
            </button>

            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={() => executeTimesheetAction("ExportDateWise")}
              disabled={controlsDisabled}
              title="Download DateWise Timesheet"
              aria-label="Download DateWise Timesheet"
            >
              <SvgIcon name="downloadDateWiseTimesheet" size={19} />
            </button>

            {adminUser && (
              <button
                type="button"
                className="tp-form-page-icon-button"
                onClick={() => executeTimesheetAction("ExportProjectWise")}
                disabled={controlsDisabled}
                title="Download ProjectWise Timesheet"
                aria-label="Download ProjectWise Timesheet"
              >
                <SvgIcon name="downloadProjectWiseTimesheet" size={19} />
              </button>
            )}

            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={() => closeTab(NavigateDownloadTimesheet)}
              disabled={controlsDisabled}
              title="Cancel"
              aria-label="Cancel"
            >
              <SvgIcon name="close" size={18} />
            </button>
          </div>
        </div>

        <div className="tp-card tp-form-card tp-form-card-attached tp-download-timesheet-card">
          <div className="row g-3 tp-download-timesheet-filters">
            {adminUser && (
              <div className="col-md-3">
                <SearchableDropdown
                  name="clientID"
                  label="Client"
                  value={filters.clientID}
                  onChange={updateFilter}
                  options={clientOptions}
                  columns={[{ key: "label", label: "Client" }]}
                  error={errors.clientID}
                  placeholder="Select client"
                  disabled={controlsDisabled}
                />
              </div>
            )}

            <div className={adminUser ? "col-md-3" : "col-md-4"}>
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
                disabled={controlsDisabled || !adminUser}
              />
            </div>

            <div className={adminUser ? "col-md-3" : "col-md-4"}>
              <SearchableDropdown
                name="timesheetMonth"
                label="Timesheet Month"
                required
                value={filters.timesheetMonth}
                onChange={updateFilter}
                options={monthOptions}
                columns={[{ key: "label", label: "Month" }]}
                error={errors.timesheetMonth}
                placeholder="Select month"
                disabled={controlsDisabled}
              />
            </div>

            <div className={adminUser ? "col-md-3" : "col-md-4"}>
              <SearchableDropdown
                name="timesheetYear"
                label="Timesheet Year"
                required
                value={filters.timesheetYear}
                onChange={updateFilter}
                options={yearOptions}
                columns={[{ key: "label", label: "Year" }]}
                error={errors.timesheetYear}
                placeholder="Select year"
                disabled={controlsDisabled}
              />
            </div>
          </div>
        </div>
      </div>

      <EmployeeTimesheetDetailsModal
        show={showTimesheetModal}
        rows={timesheetRows}
        onClose={() => setShowTimesheetModal(false)}
      />
    </>
  );
}
