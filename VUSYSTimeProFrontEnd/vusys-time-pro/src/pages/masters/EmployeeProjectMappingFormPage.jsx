import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  Checkbox,
  SearchableDropdown,
} from "../../components/forms/FormElements";
import SvgIcon from "../../components/common/SvgIcon";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import { getDropdownItems } from "../../services/dropdownService";
import {
  getEmployeeProjectMappingDetails,
  saveAndUpdateEmployeeProjectMappingDetails,
} from "../../services/employeeProjectMappingService";
import useFormValidation from "../../hooks/useFormValidation";
import { employeeProjectMappingValidationRules } from "../../validation/employeeProjectMappingValidationRules";
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

const EMPLOYEE_PROJECT_MAPPING_API_FIELD_MAPPINGS = {
  projectID: { apiKeys: ["ProjectID"] },
  employeeCode: { apiKeys: ["EmployeeCode"] },
};

const blankMapping = {
  employeeCode: "",
  projectID: "",
  isNotifyToEmployee: true,
  isDeleted: false,
};

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());
const NavigateEmployeeProjectMapping =
  ROUTE_CONSTANTS.MANAGE_PROJECTS.EMPLOYEE_PROJECT_MAPPING;

const isTimeawayProject = (projectID, projectOptions = []) => {
  const selectedProject = projectOptions.find(
    (option) => String(option?.value || "") === String(projectID || ""),
  );

  const projectName = String(selectedProject?.label || "")
    .trim()
    .toLowerCase();

  /*
   * "Timeaway" is the current project name. "Timeway" is also accepted
   * defensively because some older screens/data used that spelling.
   */
  return projectName === "timeaway" || projectName === "timeway";
};

const createComparableSnapshot = (value) =>
  JSON.stringify({
    employeeCode: value.employeeCode,
    projectID: value.projectID,
    isNotifyToEmployee: value.isNotifyToEmployee,
    isDeleted: value.isDeleted,
  });

export default function EmployeeProjectMappingFormPage() {
  const { registerTabDirtyState } = useTabs();
  const { mappingId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const isAddMode = !mappingId || mappingId === "new";
  const decodedMappingId = isAddMode ? 0 : decodeRouteId(mappingId);
  const requestedMode =
    location.state?.mode ||
    new URLSearchParams(location.search).get("mode") ||
    "view";

  const hasCreatePermission = hasPermission(
    "Employee Project Mapping Master",
    "Create",
  );
  const hasEditPermission = hasPermission(
    "Employee Project Mapping Master",
    "Edit",
  );
  const hasViewPermission = hasPermission(
    "Employee Project Mapping Master",
    "View",
  );

  const [mode, setMode] = useState(isAddMode ? "add" : requestedMode);
  const [form, setForm] = useState(blankMapping);
  const [employeeOptions, setEmployeeOptions] = useState([]);
  const [projectOptions, setProjectOptions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [dirtyOpen, setDirtyOpen] = useState(false);

  const originalSnapshotRef = useRef(createComparableSnapshot(blankMapping));
  const dropdownLoadPromiseRef = useRef(null);
  const detailsLoadPromiseRef = useRef({ id: "", promise: null });
  const pendingActionRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(employeeProjectMappingValidationRules);

  const readOnly = mode === "view";
  const canSave = mode === "add" || mode === "edit";
  const canCancel = mode === "add" || mode === "edit";
  const canEdit = !isAddMode && mode !== "edit" && hasEditPermission;

  const title = isAddMode
    ? "Add Employee Project Mapping"
    : mode === "edit"
      ? "Edit Employee Project Mapping"
      : "View Employee Project Mapping";

  const isDirty = useMemo(
    () => createComparableSnapshot(form) !== originalSnapshotRef.current,
    [form],
  );

  useEffect(() => {
    registerTabDirtyState(NavigateEmployeeProjectMapping, isDirty);

    return () => {
      registerTabDirtyState(NavigateEmployeeProjectMapping, false);
    };
  }, [isDirty, registerTabDirtyState]);

  const mapDetailsToForm = (mapping, options = projectOptions) => {
    const projectID = mapping?.projectID ? String(mapping.projectID) : "";

    return {
      ...blankMapping,
      employeeCode: String(mapping?.employeeCode || ""),
      projectID,
      isNotifyToEmployee: isTimeawayProject(projectID, options)
        ? false
        : Boolean(mapping?.isNotifyToEmployee),
      isDeleted: Boolean(mapping?.isDeleted),
    };
  };

  useEffect(() => {
    let active = true;
    const normalizedMappingId = String(decodedMappingId || "");

    if (!isAddMode && !decodedMappingId) {
      navigate(NavigateEmployeeProjectMapping, { replace: true });
      return undefined;
    }

    setLoading(true);
    const loaderRequestId = showFullPageLoader(
      "Loading Employee Project Mapping Details...",
    );

    if (!dropdownLoadPromiseRef.current) {
      dropdownLoadPromiseRef.current = Promise.all([
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.EMPLOYEE }),
        getDropdownItems({ sourceTable: DROPDOWN_SOURCE_TABLE.PROJECT }),
      ]);
    }

    if (
      !isAddMode &&
      (detailsLoadPromiseRef.current.id !== normalizedMappingId ||
        !detailsLoadPromiseRef.current.promise)
    ) {
      detailsLoadPromiseRef.current = {
        id: normalizedMappingId,
        promise: getEmployeeProjectMappingDetails(decodedMappingId),
      };
    }

    Promise.all([
      dropdownLoadPromiseRef.current,
      isAddMode ? Promise.resolve(null) : detailsLoadPromiseRef.current.promise,
    ])
      .then(([[employeeResult, projectResult], detailsResult]) => {
        if (!active) return;

        setEmployeeOptions(
          employeeResult.options.map((option) => ({
            ...option,
            employeeCode: option.value,
            employeeName: option.label,
          })),
        );
        setProjectOptions(projectResult.options);

        if (!detailsResult?.mapping) {
          const nextForm = { ...blankMapping, isNotifyToEmployee: true };
          setForm(nextForm);
          originalSnapshotRef.current = createComparableSnapshot(nextForm);
          return;
        }

        const nextForm = mapDetailsToForm(
          detailsResult.mapping,
          projectResult.options,
        );
        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
      })
      .catch((error) => {
        if (!active) return;
        showApiError(error, "Unable to load Employee Project Mapping Details.");
        if (!isAddMode) {
          navigate(NavigateEmployeeProjectMapping, { replace: true });
        }
      })
      .finally(() => {
        hideFullPageLoader(loaderRequestId);
        if (active) setLoading(false);
      });

    return () => {
      active = false;
      hideFullPageLoader(loaderRequestId);
    };
  }, [decodedMappingId, isAddMode, navigate]);

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

    if (name === "projectID") {
      const isTimeaway = isTimeawayProject(nextValue, projectOptions);

      setForm((current) => ({
        ...current,
        projectID: nextValue,
        isNotifyToEmployee: isTimeaway
          ? false
          : isAddMode
            ? true
            : current.isNotifyToEmployee,
      }));
      clearError(name);
      return;
    }

    setForm((current) => ({ ...current, [name]: nextValue }));
    clearError(name);
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
    const result = await getEmployeeProjectMappingDetails(decodedMappingId);
    const nextForm = mapDetailsToForm(result.mapping);
    setForm(nextForm);
    originalSnapshotRef.current = createComparableSnapshot(nextForm);
    return result;
  };

  const enterEditMode = () => {
    // Business requirement: Notify To Employee must always be unchecked and
    // disabled in Edit mode, regardless of the value stored previously.
    setForm((current) => ({ ...current, isNotifyToEmployee: false }));
    setMode("edit");
  };

  const cancelEdit = async () => {
    clearErrors();

    if (isAddMode) {
      navigate(NavigateEmployeeProjectMapping);
      return;
    }

    setLoading(true);
    const loaderRequestId = showFullPageLoader(
      "Loading Employee Project Mapping Details...",
    );

    try {
      await reloadDetails();
      setMode("view");
    } catch (error) {
      showApiError(error, "Unable to reload Employee Project Mapping Details.");
    } finally {
      hideFullPageLoader(loaderRequestId);
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

    const loaderRequestId = showFullPageLoader(
      "Saving Employee Project Mapping Details...",
    );
    setSaving(true);

    try {
      const result = await saveAndUpdateEmployeeProjectMappingDetails({
        id: isAddMode ? 0 : decodedMappingId,
        projectID: form.projectID,
        employeeCode: form.employeeCode,
        isNotifyToEmployee: isTimeawayProject(form.projectID, projectOptions)
          ? false
          : isAddMode
            ? true
            : false,
        isDeleted: mode === "edit" ? form.isDeleted : false,
        createdBy: isAddMode ? loggedInUserId : 0,
        updatedBy: isAddMode ? 0 : loggedInUserId,
      });

      ToastService.success(
        result.message ||
          "Employee Project Mapping Details Saved Successfully.",
      );

      const savedId = result.id || decodedMappingId;

      if (!isAddMode && form.isDeleted) {
        navigate(NavigateEmployeeProjectMapping);
        return;
      }

      if (savedId > 0) {
        const detailsResult = await getEmployeeProjectMappingDetails(savedId);
        const nextForm = mapDetailsToForm(detailsResult.mapping);
        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
        setMode("view");

        if (isAddMode) {
          detailsLoadPromiseRef.current = {
            id: String(savedId),
            promise: Promise.resolve(detailsResult),
          };
          navigate(NavigateEmployeeProjectMapping);
        } else {
          navigate(NavigateEmployeeProjectMapping);
        }
      } else {
        navigate(NavigateEmployeeProjectMapping);
      }
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          EMPLOYEE_PROJECT_MAPPING_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to save Employee Project Mapping Details.");
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
        message="You don't have permission to access this Employee Project Mapping action."
        returnPath={NavigateEmployeeProjectMapping}
        returnLabel="Back to Employee Project Mapping"
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
              onClick={enterEditMode}
              disabled={!canEdit}
              title="Edit Employee Project Mapping"
              aria-label="Edit Employee Project Mapping"
            >
              <SvgIcon name="edit" size={18} />
            </button>
          )}

          <button
            type="submit"
            form="employee-project-mapping-form"
            className="tp-form-page-icon-button"
            disabled={!canSave || saving}
            title="Save Employee Project Mapping"
            aria-label="Save Employee Project Mapping"
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
            onClick={() =>
              runWithDirtyGuard(() => navigate(NavigateEmployeeProjectMapping))
            }
            title="Back to Employee Project Mapping"
            aria-label="Back to Employee Project Mapping"
          >
            <SvgIcon name="back" size={18} />
          </button>
        </div>
      </div>

      <form
        id="employee-project-mapping-form"
        className="tp-card tp-form-card tp-form-card-attached"
        onSubmit={save}
        noValidate
      >
        <div className="row g-3">
          <div className="col-md-4">
            <SearchableDropdown
              name="employeeCode"
              label="Employee"
              required
              value={form.employeeCode}
              onChange={update}
              options={employeeOptions}
              columns={[
                { key: "employeeCode", label: "Employee Code" },
                { key: "employeeName", label: "Employee Name" },
              ]}
              error={errors.employeeCode}
              placeholder="Search and Select Employee"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4">
            <SearchableDropdown
              name="projectID"
              label="Project"
              required
              value={form.projectID}
              onChange={update}
              options={projectOptions}
              error={errors.projectID}
              placeholder="Search and Select Project"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-4 tp-employee-project-mapping-notify">
            <Checkbox
              name="isNotifyToEmployee"
              label="Notify To Employee"
              checked={
                isTimeawayProject(form.projectID, projectOptions)
                  ? false
                  : isAddMode
                    ? true
                    : mode === "edit"
                      ? false
                      : form.isNotifyToEmployee
              }
              onChange={update}
              disabled
              switchStyle
            />
          </div>

          {mode === "edit" && (
            <div className="col-md-4 pt-2">
              <Checkbox
                name="isDeleted"
                label="Obsolete"
                checked={form.isDeleted}
                onChange={update}
                switchStyle
              />
            </div>
          )}
        </div>
      </form>

      <DirtyStateModal
        show={dirtyOpen}
        onConfirm={discardChanges}
        onCancel={() => {
          pendingActionRef.current = null;
          setDirtyOpen(false);
        }}
      />
    </div>
  );
}
