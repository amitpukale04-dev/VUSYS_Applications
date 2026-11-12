import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  Checkbox,
  DatePicker,
  SearchableDropdown,
  TextArea,
  TextBox,
} from "../../components/forms/FormElements";
import SvgIcon from "../../components/common/SvgIcon";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import { getDropdownItems } from "../../services/dropdownService";
import {
  getProjectDetails,
  saveAndUpdateProjectDetails,
} from "../../services/projectService";
import useFormValidation from "../../hooks/useFormValidation";
import { projectValidationRules } from "../../validation/projectValidationRules";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import {
  COMMON_MESSAGE,
  DROPDOWN_SOURCE_TABLE,
} from "../../constants/constants";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { decodeRouteId, encodeRouteId } from "../../utils/routeIdUtils";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const PROJECT_API_FIELD_MAPPINGS = {
  projectCode: { apiKeys: ["ProjectCode"] },
  clientName: { apiKeys: ["ClientName"] },
  projectName: { apiKeys: ["ProjectName"] },
  projectDescription: { apiKeys: ["ProjectDescription"] },
  startDate: { apiKeys: ["StartDate"] },
  endDate: { apiKeys: ["EndDate"] },
  allocatedHours: { apiKeys: ["AllocatedHours"] },
  consumedHours: { apiKeys: ["ConsumedHours"] },
  projectStatus: { apiKeys: ["ProjectStatus"] },
};

const blankProject = {
  projectCode: "",
  clientName: "",
  projectName: "",
  projectDescription: "",
  startDate: "",
  endDate: "",
  allocatedHours: "0",
  consumedHours: "0",
  projectStatus: "",
  isDeleted: false,
};

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateProject = ROUTE_CONSTANTS.MANAGE_PROJECTS.PROJECTS;

const createComparableSnapshot = (value) =>
  JSON.stringify({
    projectCode: value.projectCode,
    clientName: value.clientName,
    projectName: value.projectName,
    projectDescription: value.projectDescription,
    startDate: value.startDate,
    endDate: value.endDate,
    allocatedHours: value.allocatedHours,
    consumedHours: value.consumedHours,
    projectStatus: value.projectStatus,
    isDeleted: value.isDeleted,
  });

export default function ProjectFormPage() {
  const { registerTabDirtyState } = useTabs();
  const { projectId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const isAddMode = !projectId || projectId === "new";
  const decodedProjectId = isAddMode ? 0 : decodeRouteId(projectId);
  const requestedMode =
    location.state?.mode ||
    new URLSearchParams(location.search).get("mode") ||
    "view";

  const hasCreatePermission = hasPermission("Project Master", "Create");
  const hasEditPermission = hasPermission("Project Master", "Edit");
  const hasViewPermission = hasPermission("Project Master", "View");

  const [mode, setMode] = useState(isAddMode ? "add" : requestedMode);
  const [form, setForm] = useState(blankProject);
  const [projectStatusOptions, setProjectStatusOptions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [dirtyOpen, setDirtyOpen] = useState(false);

  const originalSnapshotRef = useRef(createComparableSnapshot(blankProject));
  const dropdownLoadPromiseRef = useRef(null);
  const detailsLoadPromiseRef = useRef({ id: "", promise: null });
  const pendingActionRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(projectValidationRules);

  // Display the existing global loader while project details/dropdowns load.
  // Cleanup also dismisses it on navigation or unmount.
  useEffect(() => {
    if (!loading || securityAccessLoading) return undefined;

    const loaderRequestId = showFullPageLoader("Loading Project Details...");
    return () => hideFullPageLoader(loaderRequestId);
  }, [loading, securityAccessLoading, showFullPageLoader, hideFullPageLoader]);

  const readOnly = mode === "view";
  const canSave = mode === "add" || mode === "edit";
  const canCancel = mode === "add" || mode === "edit";
  const canEdit = !isAddMode && mode !== "edit" && hasEditPermission;

  const title = isAddMode
    ? "Add Project"
    : mode === "edit"
      ? "Edit Project"
      : "View Project";

  const isDirty = useMemo(
    () => createComparableSnapshot(form) !== originalSnapshotRef.current,
    [form],
  );

  useEffect(() => {
    registerTabDirtyState(NavigateProject, isDirty);

    return () => {
      registerTabDirtyState(NavigateProject, false);
    };
  }, [isDirty, registerTabDirtyState]);

  useEffect(() => {
    let active = true;
    const normalizedProjectId = String(decodedProjectId || "");

    if (!isAddMode && !decodedProjectId) {
      navigate(NavigateProject, { replace: true });
      return undefined;
    }

    setLoading(true);

    if (!dropdownLoadPromiseRef.current) {
      dropdownLoadPromiseRef.current = getDropdownItems({
        sourceTable: DROPDOWN_SOURCE_TABLE.PROJECT_STATUS,
      });
    }

    if (
      !isAddMode &&
      (detailsLoadPromiseRef.current.id !== normalizedProjectId ||
        !detailsLoadPromiseRef.current.promise)
    ) {
      detailsLoadPromiseRef.current = {
        id: normalizedProjectId,
        promise: getProjectDetails(decodedProjectId),
      };
    }

    Promise.all([
      dropdownLoadPromiseRef.current,
      isAddMode ? Promise.resolve(null) : detailsLoadPromiseRef.current.promise,
    ])
      .then(([statusResult, detailsResult]) => {
        if (!active) return;

        setProjectStatusOptions(statusResult.options);

        if (!detailsResult?.project) {
          setForm(blankProject);
          originalSnapshotRef.current = createComparableSnapshot(blankProject);
          return;
        }

        const nextForm = {
          ...blankProject,
          ...detailsResult.project,
          allocatedHours: String(detailsResult.project.allocatedHours ?? 0),
          consumedHours: String(detailsResult.project.consumedHours ?? 0),
          projectStatus: String(detailsResult.project.projectStatus || ""),
        };

        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
      })
      .catch((error) => {
        if (!active) return;
        showApiError(error, "Unable to load Project Details.");
        if (!isAddMode) {
          navigate(NavigateProject, { replace: true });
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [decodedProjectId, isAddMode, navigate]);

  useEffect(() => {
    const handleBeforeUnload = (event) => {
      if (!canSave || !isDirty) return;
      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [canSave, isDirty]);

  const update = (event) => {
    const { name, type, checked, value } = event.target;
    const nextValue = type === "checkbox" ? checked : value;

    setForm((current) => ({ ...current, [name]: nextValue }));
    clearError(name);

    if (name === "startDate") {
      clearError("endDate");
    }
  };

  const runWithDirtyGuard = (action) => {
    if (canSave && isDirty) {
      pendingActionRef.current = action;
      setDirtyOpen(true);
      return;
    }

    action();
  };

  const discardChanges = () => {
    setDirtyOpen(false);
    const action = pendingActionRef.current;
    pendingActionRef.current = null;
    action?.();
  };

  const reloadDetails = async () => {
    const result = await getProjectDetails(decodedProjectId);
    const nextForm = {
      ...blankProject,
      ...result.project,
      allocatedHours: String(result.project.allocatedHours ?? 0),
      consumedHours: String(result.project.consumedHours ?? 0),
      projectStatus: String(result.project.projectStatus || ""),
    };

    setForm(nextForm);
    originalSnapshotRef.current = createComparableSnapshot(nextForm);
    return result;
  };

  const cancelEdit = async () => {
    clearErrors();

    if (isAddMode) {
      navigate(NavigateProject);
      return;
    }

    setLoading(true);

    try {
      await reloadDetails();
      setMode("view");
    } catch (error) {
      showApiError(error, "Unable to reload Project Details.");
    } finally {
      setLoading(false);
    }
  };

  const save = async (event) => {
    event.preventDefault();

    if (!canSave || saving) return;

    /*
     * Edit mode must never call the API when the loaded record has not
     * actually been modified.
     */
    if (!isAddMode && mode === "edit" && !isDirty) {
      ToastService.error(COMMON_MESSAGE.NO_CHANGES_MADE_TO_FORM);
      return;
    }

    if (!validate(form)) return;

    const loaderRequestId = showFullPageLoader("Saving Project Details...");
    setSaving(true);

    try {
      const result = await saveAndUpdateProjectDetails({
        projectID: isAddMode ? 0 : decodedProjectId,
        projectCode: form.projectCode,
        clientName: form.clientName,
        projectName: form.projectName,
        projectDescription: form.projectDescription,
        startDate: form.startDate,
        endDate: form.endDate,
        allocatedHours: form.allocatedHours,
        consumedHours: form.consumedHours,
        projectStatus: form.projectStatus,
        isDeleted: mode === "edit" ? form.isDeleted : false,
        createdBy: isAddMode ? loggedInUserId : 0,
        updatedBy: isAddMode ? 0 : loggedInUserId,
      });

      ToastService.success(
        result.message || "Project Details Saved Successfully.",
      );

      const savedId = result.id || decodedProjectId;

      if (!isAddMode && form.isDeleted) {
        navigate(NavigateProject);
        return;
      }

      if (savedId > 0) {
        const detailsResult = await getProjectDetails(savedId);
        const nextForm = {
          ...blankProject,
          ...detailsResult.project,
          allocatedHours: String(detailsResult.project.allocatedHours ?? 0),
          consumedHours: String(detailsResult.project.consumedHours ?? 0),
          projectStatus: String(detailsResult.project.projectStatus || ""),
        };

        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
        setMode("view");

        if (isAddMode) {
          detailsLoadPromiseRef.current = {
            id: String(savedId),
            promise: Promise.resolve(detailsResult),
          };
          navigate(NavigateProject);
        } else {
          navigate(NavigateProject);
        }
      } else {
        navigate(NavigateProject);
      }
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          PROJECT_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to save Project Details.");
      }
    } finally {
      hideFullPageLoader(loaderRequestId);
      setSaving(false);
    }
  };

  if (securityAccessLoading) {
    return (
      <div className="tp-form-page-loading">
        <span className="spinner-border text-primary" aria-hidden="true" />
        <p>Loading security permissions...</p>
      </div>
    );
  }

  const hasRequestedAccess = isAddMode
    ? hasCreatePermission
    : requestedMode === "edit"
      ? hasEditPermission
      : hasViewPermission;

  if (!hasRequestedAccess) {
    return (
      <AccessDenied
        message="You don't have permission to access this Project action."
        returnPath={NavigateProject}
        returnLabel="Back to Projects"
      />
    );
  }

  if (loading) {
    return null;
  }

  return (
    <div className="tp-screen-form-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="project" size={16} />
          <h1>{title}</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          {hasEditPermission && !isAddMode && (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={() => setMode("edit")}
              disabled={!canEdit}
              title="Edit Project"
              aria-label="Edit Project"
            >
              <SvgIcon name="edit" size={18} />
            </button>
          )}

          <button
            type="submit"
            form="project-form"
            className="tp-form-page-icon-button"
            disabled={!canSave || saving}
            title="Save Project"
            aria-label="Save Project"
          >
            {saving ? (
              <span className="spinner-border spinner-border-sm" />
            ) : (
              <SvgIcon name="save" size={18} />
            )}
          </button>

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => runWithDirtyGuard(cancelEdit)}
            disabled={!canCancel || saving}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>

          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={() => runWithDirtyGuard(() => navigate(NavigateProject))}
            title="Back to Projects"
            aria-label="Back to Projects"
          >
            <SvgIcon name="back" size={18} />
          </button>
        </div>
      </div>

      <form
        id="project-form"
        className="tp-card tp-form-card tp-form-card-attached"
        onSubmit={save}
        noValidate
      >
        <div className="row g-3">
          <div className="col-md-4">
            <TextBox
              name="projectCode"
              label="Project Code"
              required
              value={form.projectCode}
              onChange={update}
              error={errors.projectCode}
              placeholder="Enter Project Code"
              disabled={readOnly}
              maxLength={100}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="clientName"
              label="Client Name"
              required
              value={form.clientName}
              onChange={update}
              error={errors.clientName}
              placeholder="Enter Client Name"
              disabled={readOnly}
              maxLength={200}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="projectName"
              label="Project Name"
              required
              value={form.projectName}
              onChange={update}
              error={errors.projectName}
              placeholder="Enter Project Name"
              disabled={readOnly}
              maxLength={200}
            />
          </div>

          <div className="col-md-6">
            <TextArea
              name="projectDescription"
              label="Project Description"
              required
              value={form.projectDescription}
              onChange={update}
              error={errors.projectDescription}
              placeholder="Enter Project Description"
              disabled={readOnly}
              rows={5}
              maxLength={2000}
            />
          </div>

          <div className="col-md-6">
            <div className="row g-3">
              <div className="col-md-6">
                <DatePicker
                  name="startDate"
                  label="Start Date"
                  required
                  value={form.startDate}
                  onChange={update}
                  error={errors.startDate}
                  disabled={readOnly}
                  allowFutureDates
                  allowPastDates
                />
              </div>

              <div className="col-md-6">
                <DatePicker
                  name="endDate"
                  label="End Date"
                  required
                  value={form.endDate}
                  onChange={update}
                  error={errors.endDate}
                  disabled={readOnly}
                  allowFutureDates
                  allowPastDates
                />
              </div>
            </div>
          </div>

          <div className="col-md-4">
            <TextBox
              name="allocatedHours"
              label="Allocated Hours"
              type="number"
              required
              value={form.allocatedHours}
              onChange={update}
              error={errors.allocatedHours}
              placeholder="Enter Allocated Hours"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="consumedHours"
              label="Consumed Hours"
              type="number"
              required
              value={form.consumedHours}
              onChange={update}
              error={errors.consumedHours}
              placeholder="Enter Consumed Hours"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="projectStatus"
              label="Project Status"
              required
              placeholder="Search and Select Project Status"
              value={form.projectStatus}
              onChange={update}
              options={projectStatusOptions}
              columns={[{ key: "label", label: "Project Status" }]}
              error={errors.projectStatus}
              disabled={readOnly}
            />
          </div>

          {mode === "edit" && (
            <div className="col-md-4 d-flex align-items-end pb-2">
              <Checkbox
                name="isDeleted"
                label="Obsolete"
                checked={form.isDeleted}
                onChange={update}
                switchStyle
                disabled={saving}
              />
            </div>
          )}
        </div>
      </form>

      <DirtyStateModal
        open={dirtyOpen}
        onStay={() => {
          setDirtyOpen(false);
          pendingActionRef.current = null;
        }}
        onDiscard={discardChanges}
      />
    </div>
  );
}
