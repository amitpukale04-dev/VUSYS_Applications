import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  Checkbox,
  TextArea,
  TextBox,
} from "../../components/forms/FormElements";
import SvgIcon from "../../components/common/SvgIcon";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import {
  getLeaveTypeDetails,
  saveAndUpdateLeaveTypeDetails,
} from "../../services/leaveTypeService";
import useFormValidation from "../../hooks/useFormValidation";
import { leaveTypeValidationRules } from "../../validation/leaveTypeValidationRules";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { COMMON_MESSAGE } from "../../constants/constants";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { decodeRouteId } from "../../utils/routeIdUtils";
import { normalizeNumber } from "../../utils/valueUtils";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const blankLeaveType = {
  leaveTypeCode: "",
  leaveType: "",
  leaveTypeDescription: "",
  defaultLeaveCount: "0",
  isPaidLeave: false,
  allowHalfDay: false,
  isDeleted: false,
};

const LEAVE_TYPE_API_FIELD_MAPPINGS = {
  leaveTypeCode: { apiKeys: ["LeaveTypeCode"] },
  leaveType: { apiKeys: ["LeaveType"] },
  leaveTypeDescription: { apiKeys: ["LeaveTypeDescription"] },
};

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const NavigateLeaveTypes = ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_TYPES;

const createComparableSnapshot = (value) =>
  JSON.stringify({
    leaveTypeCode: value.leaveTypeCode,
    leaveType: value.leaveType,
    leaveTypeDescription: value.leaveTypeDescription,
    defaultLeaveCount: value.defaultLeaveCount,
    isPaidLeave: value.isPaidLeave,
    allowHalfDay: value.allowHalfDay,
    isDeleted: value.isDeleted,
  });

export default function LeaveTypeFormPage() {
  const { registerTabDirtyState } = useTabs();
  const { leaveTypeId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const isAddMode = !leaveTypeId || leaveTypeId === "new";
  const decodedLeaveTypeId = isAddMode ? 0 : decodeRouteId(leaveTypeId);
  const requestedMode =
    location.state?.mode ||
    new URLSearchParams(location.search).get("mode") ||
    "view";

  const hasCreatePermission = hasPermission("Leave Type Master", "Create");
  const hasEditPermission = hasPermission("Leave Type Master", "Edit");
  const hasViewPermission = hasPermission("Leave Type Master", "View");

  const [mode, setMode] = useState(isAddMode ? "add" : requestedMode);
  const [form, setForm] = useState(blankLeaveType);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [dirtyOpen, setDirtyOpen] = useState(false);

  const originalSnapshotRef = useRef(createComparableSnapshot(blankLeaveType));
  const detailsLoadPromiseRef = useRef({ id: "", promise: null });
  const pendingActionRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(leaveTypeValidationRules);

  const readOnly = mode === "view";
  const canSave = mode === "add" || mode === "edit";
  const canCancel = mode === "add" || mode === "edit";
  const canEdit = !isAddMode && mode !== "edit" && hasEditPermission;

  const title = isAddMode
    ? "Add Leave Type"
    : mode === "edit"
      ? "Edit Leave Type"
      : "View Leave Type";

  const isDirty = useMemo(
    () => createComparableSnapshot(form) !== originalSnapshotRef.current,
    [form],
  );

  useEffect(() => {
    registerTabDirtyState(NavigateLeaveTypes, isDirty);

    return () => {
      registerTabDirtyState(NavigateLeaveTypes, false);
    };
  }, [isDirty, registerTabDirtyState]);

  useEffect(() => {
    let active = true;
    const normalizedLeaveTypeId = String(decodedLeaveTypeId || "");

    if (!isAddMode && !decodedLeaveTypeId) {
      navigate(NavigateLeaveTypes, { replace: true });
      return undefined;
    }

    setLoading(true);
    const loaderRequestId = !isAddMode
      ? showFullPageLoader("Loading leave type details...")
      : null;

    if (
      !isAddMode &&
      (detailsLoadPromiseRef.current.id !== normalizedLeaveTypeId ||
        !detailsLoadPromiseRef.current.promise)
    ) {
      detailsLoadPromiseRef.current = {
        id: normalizedLeaveTypeId,
        promise: getLeaveTypeDetails(decodedLeaveTypeId),
      };
    }

    const detailsPromise = isAddMode
      ? Promise.resolve(null)
      : detailsLoadPromiseRef.current.promise;

    detailsPromise
      .then((detailsResult) => {
        if (!active) return;

        if (!detailsResult?.leaveType) {
          setForm(blankLeaveType);
          originalSnapshotRef.current =
            createComparableSnapshot(blankLeaveType);
          return;
        }

        const nextForm = {
          ...blankLeaveType,
          ...detailsResult.leaveType,
          defaultLeaveCount: String(
            detailsResult.leaveType.defaultLeaveCount ?? 0,
          ),
        };

        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
      })
      .catch((error) => {
        if (!active) return;
        showApiError(error, "Unable to load Leave Type Details.");
        if (!isAddMode) {
          navigate(NavigateLeaveTypes, { replace: true });
        }
      })
      .finally(() => {
        if (active) setLoading(false);
        if (loaderRequestId !== null) hideFullPageLoader(loaderRequestId);
      });

    return () => {
      active = false;
    };
  }, [
    decodedLeaveTypeId,
    isAddMode,
    navigate,
    showFullPageLoader,
    hideFullPageLoader,
  ]);

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
    const result = await getLeaveTypeDetails(decodedLeaveTypeId);
    const nextForm = {
      ...blankLeaveType,
      ...result.leaveType,
      defaultLeaveCount: String(result.leaveType.defaultLeaveCount ?? 0),
    };

    setForm(nextForm);
    originalSnapshotRef.current = createComparableSnapshot(nextForm);
    return result;
  };

  const cancelEdit = async () => {
    clearErrors();

    if (isAddMode) {
      navigate(NavigateLeaveTypes);
      return;
    }
    setLoading(true);
    const loaderRequestId = showFullPageLoader("Loading leave type details...");
    try {
      await reloadDetails();
      setMode("view");
    } catch (error) {
      showApiError(error, "Unable to reload Leave Type Details.");
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

    const loaderRequestId = showFullPageLoader("Saving Leave Type Details...");
    setSaving(true);

    try {
      const result = await saveAndUpdateLeaveTypeDetails({
        id: isAddMode ? 0 : decodedLeaveTypeId,
        leaveTypeCode: form.leaveTypeCode,
        leaveType: form.leaveType,
        leaveTypeDescription: form.leaveTypeDescription,
        defaultLeaveCount: form.defaultLeaveCount,
        isPaidLeave: form.isPaidLeave,
        allowHalfDay: form.allowHalfDay,
        isDeleted: mode === "edit" ? form.isDeleted : false,
        createdBy: isAddMode ? loggedInUserId : 0,
        updatedBy: isAddMode ? 0 : loggedInUserId,
      });

      ToastService.success(
        result.message || "Leave Type Details Saved Successfully.",
      );

      const savedId = result.id || decodedLeaveTypeId;

      if (!isAddMode && form.isDeleted) {
        navigate(NavigateLeaveTypes);
        return;
      }

      if (savedId > 0) {
        const detailsResult = await getLeaveTypeDetails(savedId);
        const nextForm = {
          ...blankLeaveType,
          ...detailsResult.leaveType,
          defaultLeaveCount: String(
            detailsResult.leaveType.defaultLeaveCount ?? 0,
          ),
        };

        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
        setMode("view");

        if (isAddMode) {
          detailsLoadPromiseRef.current = {
            id: String(savedId),
            promise: Promise.resolve(detailsResult),
          };
          navigate(NavigateLeaveTypes);
        } else {
          navigate(NavigateLeaveTypes);
        }
      } else {
        navigate(NavigateLeaveTypes);
      }
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors || [],
          LEAVE_TYPE_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to save Leave Type Details.");
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
        message="You don't have permission to access this Leave Type action."
        returnPath={NavigateLeaveTypes}
        returnLabel="Back to Leave Types"
      />
    );
  }

  if (loading) return null;

  return (
    <div className="tp-employee-form-page tp-leave-type-form-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="leaveType" size={16} />
          <h1>{title}</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          {hasEditPermission && !isAddMode && (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={() => setMode("edit")}
              disabled={!canEdit}
              title="Edit Leave Type"
              aria-label="Edit Leave Type"
            >
              <SvgIcon name="edit" size={18} />
            </button>
          )}

          <button
            type="submit"
            form="leaveType-form"
            className="tp-form-page-icon-button"
            disabled={!canSave || saving}
            title="Save Leave Type"
            aria-label="Save Leave Type"
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
              runWithDirtyGuard(() => navigate(NavigateLeaveTypes))
            }
            title="Back to Leave Types"
            aria-label="Back to Leave Types"
          >
            <SvgIcon name="back" size={18} />
          </button>
        </div>
      </div>

      <form
        id="leaveType-form"
        className="tp-card tp-employee-form-card tp-employee-form-card-attached"
        onSubmit={save}
        noValidate
      >
        <div className="row g-3">
          {/* First Row: Leave Type Code, Leave Type and Description */}
          <div className="col-md-4">
            <TextBox
              name="leaveTypeCode"
              label="Leave Type Code"
              required
              value={form.leaveTypeCode}
              onChange={update}
              error={errors.leaveTypeCode}
              placeholder="Enter Leave Type Code"
              disabled={readOnly}
              maxLength={100}
            />
          </div>

          <div className="col-md-4">
            <TextBox
              name="leaveType"
              label="Leave Type"
              required
              value={form.leaveType}
              onChange={update}
              error={errors.leaveType}
              placeholder="Enter Leave Type"
              disabled={readOnly}
              maxLength={200}
            />
          </div>

          <div className="col-md-4">
            <TextArea
              name="leaveTypeDescription"
              label="Leave Type Description"
              required
              value={form.leaveTypeDescription}
              onChange={update}
              error={errors.leaveTypeDescription}
              placeholder="Enter Leave Type Description"
              disabled={readOnly}
              rows={3}
              maxLength={2000}
            />
          </div>

          {/* Second Row: Leave Count, Leave Options and Obsolete */}
          <div className="col-md-3">
            <TextBox
              name="defaultLeaveCount"
              label="Default Leave Count"
              type="number"
              required
              value={form.defaultLeaveCount}
              onChange={update}
              error={errors.defaultLeaveCount}
              placeholder="Enter Default Leave Count"
              disabled={readOnly}
              min="0"
              step="any"
            />
          </div>

          <div className="col-md-3 d-flex align-items-end pb-2">
            <Checkbox
              name="isPaidLeave"
              label="Is Paid Leave"
              checked={form.isPaidLeave}
              onChange={update}
              switchStyle
              disabled={readOnly || saving}
            />
          </div>

          <div className="col-md-3 d-flex align-items-end pb-2">
            <Checkbox
              name="allowHalfDay"
              label="Allow Half Day"
              checked={form.allowHalfDay}
              onChange={update}
              switchStyle
              disabled={readOnly || saving}
            />
          </div>

          {/* Obsolete is available only in Edit Mode */}
          {mode === "edit" && (
            <div className="col-md-3 d-flex align-items-end pb-2">
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
