import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { Checkbox, TextBox } from "../../components/forms/FormElements";
import SvgIcon from "../../components/common/SvgIcon";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import AccessDenied from "../../components/common/AccessDenied";
import ToastService from "../../services/toastService";
import {
  getEmailConfigurationDetails,
  saveAndUpdateEmailConfigurationDetails,
} from "../../services/emailConfigurationService";
import useFormValidation from "../../hooks/useFormValidation";
import { emailConfigurationValidationRules } from "../../validation/emailConfigurationValidationRules";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { useTabs } from "../../context/TabsContext";
import { decodeRouteId, encodeRouteId } from "../../utils/routeIdUtils";
import { COMMON_MESSAGE } from "../../constants/constants";
import { getCreatedOrUpdatedBy } from "../../utils/userSession";
import { showApiError } from "../../utils/showApiError";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { normalizeNumber } from "../../utils/valueUtils";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const EMAIL_CONFIGURATION_API_FIELD_MAPPINGS = {
  fromEmail: { apiKeys: ["FromEmail"] },
  smtpHost: { apiKeys: ["SmtpHost"] },
  portNo: { apiKeys: ["PortNo"] },
};

const blankConfiguration = {
  fromEmail: "",
  password: "",
  smtpHost: "",
  portNo: "",
  isDeleted: false,
};

const loggedInUserId = normalizeNumber(getCreatedOrUpdatedBy());

const NavigateEmailConfiguration =
  ROUTE_CONSTANTS.CONFIGURATION.EMAIL_CONFIGURATION;

const createComparableSnapshot = (value) =>
  JSON.stringify({
    fromEmail: value.fromEmail,
    password: value.password,
    smtpHost: value.smtpHost,
    portNo: String(value.portNo ?? ""),
    isDeleted: Boolean(value.isDeleted),
  });

export default function EmailConfigurationFormPage() {
  const { registerTabDirtyState } = useTabs();
  const { emailConfigurationId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const { hasPermission, isLoading: securityAccessLoading } =
    useUserSecurityPermission();

  const isAddMode = !emailConfigurationId || emailConfigurationId === "new";
  const decodedEmailConfigurationId = isAddMode
    ? 0
    : decodeRouteId(emailConfigurationId);
  const requestedMode =
    location.state?.mode ||
    new URLSearchParams(location.search).get("mode") ||
    "view";

  const hasCreatePermission = hasPermission(
    "Email Configuration Master",
    "Create",
  );
  const hasEditPermission = hasPermission("Email Configuration Master", "Edit");
  const hasViewPermission = hasPermission("Email Configuration Master", "View");

  const [mode, setMode] = useState(isAddMode ? "add" : requestedMode);
  const [form, setForm] = useState(blankConfiguration);
  const [loading, setLoading] = useState(!isAddMode);
  const [saving, setSaving] = useState(false);
  const [dirtyOpen, setDirtyOpen] = useState(false);
  const originalSnapshotRef = useRef(
    createComparableSnapshot(blankConfiguration),
  );
  const detailsLoadPromiseRef = useRef({ id: "", promise: null });
  const pendingActionRef = useRef(null);

  const { errors, validate, clearError, clearErrors, setFieldError } =
    useFormValidation(emailConfigurationValidationRules);

  const readOnly = mode === "view";
  const canSave = mode === "add" || mode === "edit";
  const canCancel = mode === "add" || mode === "edit";
  const canEdit = !isAddMode && mode !== "edit" && hasEditPermission;
  const title = isAddMode
    ? "Add Email Configuration"
    : mode === "edit"
      ? "Edit Email Configuration"
      : "View Email Configuration";

  const isDirty = useMemo(
    () => createComparableSnapshot(form) !== originalSnapshotRef.current,
    [form],
  );

  useEffect(() => {
    registerTabDirtyState(NavigateEmailConfiguration, isDirty);

    return () => {
      registerTabDirtyState(NavigateEmailConfiguration, false);
    };
  }, [isDirty, registerTabDirtyState]);

  useEffect(() => {
    if (isAddMode) {
      originalSnapshotRef.current =
        createComparableSnapshot(blankConfiguration);
      return undefined;
    }

    const normalizedId = String(decodedEmailConfigurationId || "");

    if (!decodedEmailConfigurationId) {
      navigate(NavigateEmailConfiguration, { replace: true });
      return undefined;
    }
    let active = true;
    setLoading(true);
    const loaderRequestId = showFullPageLoader(
      "Loading Email Configuration Details...",
    );

    if (
      detailsLoadPromiseRef.current.id !== normalizedId ||
      !detailsLoadPromiseRef.current.promise
    ) {
      detailsLoadPromiseRef.current = {
        id: normalizedId,
        promise: getEmailConfigurationDetails(decodedEmailConfigurationId),
      };
    }

    detailsLoadPromiseRef.current.promise
      .then((result) => {
        if (!active) return;

        const nextForm = {
          ...blankConfiguration,
          ...result.item,
          password: result.item.plainPassword || "",
          portNo: result.item.portNo ? String(result.item.portNo) : "",
        };
        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
      })
      .catch((error) => {
        if (!active) return;
        showApiError(error, "Unable to load Email Configuration Details.");
        navigate(NavigateEmailConfiguration, { replace: true });
      })
      .finally(() => {
        if (active) setLoading(false);
        hideFullPageLoader(loaderRequestId);
      });

    return () => {
      active = false;
      hideFullPageLoader(loaderRequestId);
    };
  }, [
    decodedEmailConfigurationId,
    isAddMode,
    navigate,
    showFullPageLoader,
    hideFullPageLoader,
  ]);

  const update = (event) => {
    const { name, type, checked, value } = event.target;
    setForm((current) => ({
      ...current,
      [name]: type === "checkbox" ? checked : value,
    }));
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
    const result = await getEmailConfigurationDetails(
      decodedEmailConfigurationId,
    );
    const nextForm = {
      ...blankConfiguration,
      ...result.item,
      password: result.item.plainPassword || "",
      portNo: result.item.portNo ? String(result.item.portNo) : "",
    };
    setForm(nextForm);
    originalSnapshotRef.current = createComparableSnapshot(nextForm);
    return result;
  };

  const cancelEdit = async () => {
    clearErrors();

    if (isAddMode) {
      navigate(NavigateEmailConfiguration);
      return;
    }

    setLoading(true);
    const loaderRequestId = showFullPageLoader(
      "Loading Email Configuration Details...",
    );

    try {
      await reloadDetails();
      setMode("view");
    } catch (error) {
      showApiError(error, "Unable to reload Email Configuration Details.");
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
      "Saving Email Configuration Details...",
    );
    setSaving(true);

    try {
      const result = await saveAndUpdateEmailConfigurationDetails({
        id: isAddMode ? 0 : decodedEmailConfigurationId,
        fromEmail: form.fromEmail,
        password: form.password,
        smtpHost: form.smtpHost,
        portNo: form.portNo,
        isDeleted: mode === "edit" ? form.isDeleted : false,
        createdBy: isAddMode ? loggedInUserId : 0,
        updatedBy: isAddMode ? 0 : loggedInUserId,
      });

      ToastService.success(
        result.message || "Email Configuration Details Saved Successfully.",
      );

      const savedId = result.id || decodedEmailConfigurationId;

      if (!isAddMode && form.isDeleted) {
        navigate(NavigateEmailConfiguration);
        return;
      }

      if (savedId > 0) {
        const detailsResult = await getEmailConfigurationDetails(savedId);
        const nextForm = {
          ...blankConfiguration,
          ...detailsResult.item,
          password: detailsResult.item.plainPassword || "",
          portNo: detailsResult.item.portNo
            ? String(detailsResult.item.portNo)
            : "",
        };
        setForm(nextForm);
        originalSnapshotRef.current = createComparableSnapshot(nextForm);
        setMode("view");

        if (isAddMode) {
          detailsLoadPromiseRef.current = {
            id: String(savedId),
            promise: Promise.resolve(detailsResult),
          };
          navigate(NavigateEmailConfiguration);
        } else {
          navigate(NavigateEmailConfiguration);
        }
      } else {
        navigate(NavigateEmailConfiguration);
      }
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          EMAIL_CONFIGURATION_API_FIELD_MAPPINGS,
          setFieldError,
        );
      } else {
        showApiError(error, "Unable to save Email Configuration Details.");
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
        message="You don't have permission to access this Email Configuration action."
        returnPath={NavigateEmailConfiguration}
        returnLabel="Back to Email Configuration"
      />
    );
  }

  if (loading) {
    return null;
  }

  return (
    <div className="tp-screen-form-page tp-email-configuration-form-page">
      <div className="tp-form-page-heading tp-form-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="settings" size={16} />
          <h1>{title}</h1>
        </div>

        <div className="tp-form-page-heading-actions">
          {hasEditPermission && !isAddMode && (
            <button
              type="button"
              className="tp-form-page-icon-button"
              onClick={() => setMode("edit")}
              disabled={!canEdit}
              title="Edit Email Configuration"
              aria-label="Edit Email Configuration"
            >
              <SvgIcon name="edit" size={18} />
            </button>
          )}

          <button
            type="submit"
            form="email-configuration-form"
            className="tp-form-page-icon-button"
            disabled={!canSave || saving}
            title="Save Email Configuration"
            aria-label="Save Email Configuration"
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
              runWithDirtyGuard(() => navigate(NavigateEmailConfiguration))
            }
            title="Back to Email Configuration"
            aria-label="Back to Email Configuration"
          >
            <SvgIcon name="back" size={18} />
          </button>
        </div>
      </div>

      <form
        id="email-configuration-form"
        className="tp-card tp-form-card tp-form-card-attached"
        onSubmit={save}
        noValidate
      >
        <div className="row g-3">
          <div className="col-md-6">
            <TextBox
              name="fromEmail"
              label="Email ID"
              required
              type="email"
              icon="bi-envelope-fill"
              value={form.fromEmail}
              onChange={update}
              error={errors.fromEmail}
              placeholder="Enter Email ID"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-6">
            <TextBox
              name="password"
              label="Password"
              required
              type="password"
              icon="bi-key-fill"
              showPasswordToggle
              allowPasswordToggleWhenReadOnly
              value={form.password}
              onChange={update}
              error={errors.password}
              placeholder="Enter Password"
              readOnly={readOnly}
            />
          </div>

          <div className="col-md-6">
            <TextBox
              name="smtpHost"
              label="SMTP Host"
              required
              value={form.smtpHost}
              onChange={update}
              error={errors.smtpHost}
              placeholder="smtp.gmail.com"
              disabled={readOnly}
            />
          </div>

          <div className="col-md-6">
            <TextBox
              name="portNo"
              label="Port No"
              required
              type="number"
              value={form.portNo}
              onChange={update}
              error={errors.portNo}
              placeholder="Enter Port Number"
              disabled={readOnly}
            />
          </div>

          {mode === "edit" && (
            <div className="col-12 pt-1">
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
