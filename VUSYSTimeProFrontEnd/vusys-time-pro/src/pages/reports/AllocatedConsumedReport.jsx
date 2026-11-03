import AccessDenied from "../../components/common/AccessDenied";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useEffect, useMemo, useRef, useState } from "react";
import SortableTable from "../../components/common/SortableTable";
import SvgIcon from "../../components/common/SvgIcon";
import { SearchableDropdown } from "../../components/forms/FormElements";
import { getDropdownItems } from "../../services/dropdownService";
import { getProjectAllocatedConsumedReportList } from "../../services/allocatedConsumedReportService";
import useFormValidation from "../../hooks/useFormValidation";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { DROPDOWN_SOURCE_TABLE } from "../../constants/constants";
import { allocatedConsumedReportValidationRules } from "../../validation/allocatedConsumedReportValidationRules";
import { exportBase64File } from "../../utils/fileExportUtils";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const ALLOCATED_CONSUMED_API_FIELD_MAPPINGS = {
  projectID: { apiKeys: ["ProjectID"] },
  action: { apiKeys: ["Action"] },
};

const blankFilters = {
  projectID: "0",
};

const NavigateAllocatedConsumedReport =
  ROUTE_CONSTANTS.REPORTS.ALLOCATED_CONSUMED_REPORT;

const resolveDefaultProjectID = (options = []) => {
  const allOption = options.find(
    (option) =>
      String(option?.value ?? "").trim() === "0" ||
      String(option?.label ?? "")
        .trim()
        .toLowerCase() === "all",
  );

  return String(allOption?.value ?? options?.[0]?.value ?? "");
};

export default function AllocatedConsumedReport() {
  const { closeTab } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();
  const canOpenScreen = hasPermission("Allocated/Consumed Report", "Open");

  const [filters, setFilters] = useState(blankFilters);
  const [projectOptions, setProjectOptions] = useState([]);
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);
  const [searched, setSearched] = useState(false);
  const [actionError, setActionError] = useState("");
  const dropdownLoadPromiseRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(allocatedConsumedReportValidationRules);

  useEffect(() => {
    if (securityAccessLoading || !canOpenScreen) return undefined;
    let active = true;
    setLoading(true);

    if (!dropdownLoadPromiseRef.current) {
      dropdownLoadPromiseRef.current = getDropdownItems({
        sourceTable: DROPDOWN_SOURCE_TABLE.ALL_PROJECT,
      });
    }

    const loadInitialReport = async () => {
      let loaderRequestId = null;

      try {
        const projectResult = await dropdownLoadPromiseRef.current;

        if (!active) return;

        const options = projectResult.options;
        const defaultProjectID = resolveDefaultProjectID(options);
        const defaultFilters = { projectID: defaultProjectID };

        setProjectOptions(options);
        setFilters(defaultFilters);
        clearErrors();

        loaderRequestId = showFullPageLoader(
          "Loading Allocated/Consumed Report...",
        );

        const result = await getProjectAllocatedConsumedReportList({
          projectID: defaultProjectID,
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

    const loaderRequestId = showFullPageLoader(
      "Searching Allocated/Consumed Report...",
    );
    setLoading(true);

    try {
      const result = await getProjectAllocatedConsumedReportList({
        projectID: filters.projectID,
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
          ALLOCATED_CONSUMED_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            if (fieldName === "action") {
              setActionError(message);
            } else {
              setFieldError(fieldName, message);
            }
          },
        );
      } else {
        showApiError(error, "Unable to load Allocated/Consumed Report.");
      }
    } finally {
      setLoading(false);
      hideFullPageLoader(loaderRequestId);
    }
  };

  const exportReport = async () => {
    setActionError("");
    if (!validate(filters) || exporting) return;

    const loaderRequestId = showFullPageLoader(
      "Exporting Allocated/Consumed Report...",
    );
    setExporting(true);

    try {
      const result = await getProjectAllocatedConsumedReportList({
        projectID: filters.projectID,
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
          ALLOCATED_CONSUMED_API_FIELD_MAPPINGS,
          (fieldName, message) => {
            if (fieldName === "action") {
              setActionError(message);
            } else {
              setFieldError(fieldName, message);
            }
          },
        );
      } else {
        showApiError(error, "Unable to export Allocated/Consumed Report.");
      }
    } finally {
      setExporting(false);
      hideFullPageLoader(loaderRequestId);
    }
  };

  const columns = useMemo(
    () => [
      { key: "slNo", label: "SL No.", width: "120px" },
      { key: "projectCode", label: "Project Code", width: "180px" },
      { key: "projectName", label: "Project Name", width: "200px" },
      { key: "clientName", label: "Client", width: "180px" },
      { key: "allocatedHours", label: "Allocated Hours", width: "160px" },
      { key: "consumedHours", label: "Consumed Hours", width: "160px" },
      {
        key: "requirementGatheringAndAnalysis",
        label: "Requirement Gathering & Analysis",
        width: "280px",
      },
      { key: "systemDesign", label: "System Design", width: "160px" },
      {
        key: "frontendDevelopment",
        label: "Frontend Development",
        width: "210px",
      },
      {
        key: "backendDevelopment",
        label: "Backend Development",
        width: "210px",
      },
      { key: "codeReview", label: "Code Review", width: "150px" },
      { key: "systemTesting", label: "System Testing", width: "170px" },
      {
        key: "projectManagement",
        label: "Project Management",
        width: "200px",
      },
      { key: "deployment", label: "Deployment", width: "150px" },
      {
        key: "postDeploymentMonitoring",
        label: "Post-Deployment Monitoring",
        width: "240px",
      },
      {
        key: "fixPostReleaseBugs",
        label: "Fix Post-Release Bugs",
        width: "210px",
      },
      { key: "maintenance", label: "Maintenance", width: "160px" },
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
    <div className="tp-employee-form-page tp-allocated-consumed-report-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="utilization" size={17} />
          <h1>Allocated/Consumed Report</h1>
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
            onClick={() => closeTab(NavigateAllocatedConsumedReport)}
            disabled={loading || exporting}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>

      <div className="tp-card tp-employee-form-card tp-employee-form-card-attached tp-allocated-consumed-report-card">
        <div className="row g-3 tp-allocated-consumed-report-filters">
          <div className="col-md-4">
            <SearchableDropdown
              name="projectID"
              label="Project"
              required
              value={filters.projectID}
              onChange={updateFilter}
              options={projectOptions}
              columns={[{ key: "label", label: "Project" }]}
              error={errors.projectID}
              placeholder="Select project"
              disabled={loading || exporting}
            />
          </div>
        </div>
        {actionError && (
          <div className="text-danger small mt-2" role="alert">
            {actionError}
          </div>
        )}

        <div className="tp-allocated-consumed-report-grid">
          <SortableTable
            columns={columns}
            data={rows}
            keyField="slNo"
            emptyText={
              searched
                ? "Allocated/Consumed Report Not Found."
                : "Search to view Allocated/Consumed Report."
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
            loadingText="Loading Allocated/Consumed Report..."
          />
        </div>
      </div>
    </div>
  );
}
