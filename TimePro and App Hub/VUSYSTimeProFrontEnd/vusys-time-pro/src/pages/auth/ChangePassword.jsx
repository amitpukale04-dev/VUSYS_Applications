import { useEffect, useState } from "react";
import ToastService from "../../services/toastService";
import { changePasswordRequest } from "../../services/authService";
import useFormValidation from "../../hooks/useFormValidation";
import { changePasswordValidationRules } from "../../validation/authValidationRules";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { useTabs } from "../../context/TabsContext";
import SvgIcon from "../../components/common/SvgIcon";
import TextBox from "../../components/forms/TextBox";
import PasswordTextbox from "../../components/forms/PasswordTextbox";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import { showApiError } from "../../utils/showApiError";
import { getUserEmailID } from "../../utils/userSession";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

// ============================================================
// CHANGE PASSWORD API VALIDATION FIELD MAPPINGS
// ============================================================

const CHANGE_PASSWORD_API_FIELD_MAPPINGS = {
  current: {
    apiKeys: ["OldPassword"],
  },
  next: {
    apiKeys: ["NewPassword"],
  },
  confirm: {
    apiKeys: ["ConfirmPassword"],
  },
};

const NavigateChangePassword = ROUTE_CONSTANTS.AUTH.CHANGE_PASSWORD;

export default function ChangePassword() {
  const { closeTab, registerTabDirtyState } = useTabs();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const [form, setForm] = useState({
    current: "",
    next: "",
    confirm: "",
  });
  const [loading, setLoading] = useState(false);
  const [apiCurrentPasswordError, setApiCurrentPasswordError] = useState(false);
  const { errors, validate, clearError, setFieldError } = useFormValidation(
    changePasswordValidationRules,
  );
  const emailID = getUserEmailID();

  const isDirty = Boolean(
    String(form.current || "").trim() ||
    String(form.next || "").trim() ||
    String(form.confirm || "").trim(),
  );

  useEffect(() => {
    registerTabDirtyState(NavigateChangePassword, isDirty);

    return () => {
      registerTabDirtyState(NavigateChangePassword, false);
    };
  }, [isDirty, registerTabDirtyState]);

  const resetForm = () => {
    setForm({
      current: "",
      next: "",
      confirm: "",
    });
    clearError("current");
    clearError("next");
    clearError("confirm");
    setApiCurrentPasswordError(false);
  };
  const update = (key, value) => {
    setForm((current) => ({
      ...current,
      [key]: value,
    }));
    clearError(key);
    if (key === "next") {
      clearError("confirm");
    }
    if (key === "current") {
      setApiCurrentPasswordError(false);
    }
  };

  const submit = async (event) => {
    event.preventDefault();
    if (loading) {
      return;
    }
    if (!validate(form)) {
      return;
    }
    if (!emailID) {
      ToastService.error("Email ID is not available.");
      return;
    }
    setApiCurrentPasswordError(false);
    const loaderRequestId = showFullPageLoader("Updating Password...");
    setLoading(true);

    try {
      const result = await changePasswordRequest({
        email: emailID,
        oldPassword: form.current,
        newPassword: form.next,
        confirmPassword: form.confirm,
      });
      ToastService.success(
        result?.message || "Password Changed Successfully !!!",
      );
      resetForm();
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          CHANGE_PASSWORD_API_FIELD_MAPPINGS,
          setFieldError,
        );
        return;
      }
      if (error?._globalToastHandled || error?.__globalToastHandled) {
        return;
      }
      setApiCurrentPasswordError(true);
      showApiError(error, "Unable to change the password.");
    } finally {
      hideFullPageLoader(loaderRequestId);
      setLoading(false);
    }
  };

  const handleCancel = () => {
    /*
     * Change Password is represented by its own closable tab.
     *
     * Closing the screen must remove the tab as well as navigate
     * to the previously active tab. Using closeTab keeps the tab
     * state and browser route synchronized.
     */
    closeTab(NavigateChangePassword);
  };

  return (
    <div className="tp-change-password-page tp-form-shell-page">
      <div className="tp-form-page-heading tp-form-titlebar tp-change-password-titlebar">
        <div className="tp-form-titlebar-title">
          <SvgIcon name="role" size={16} />
          <h1>Change Password</h1>
        </div>
        <div className="tp-form-page-heading-actions">
          <button
            type="submit"
            form="change-password-form"
            className="tp-form-page-icon-button"
            disabled={loading}
            title="Update Password"
            aria-label="Update Password"
          >
            {loading ? (
              <span
                className="spinner-border spinner-border-sm"
                aria-hidden="true"
              />
            ) : (
              <SvgIcon name="save" size={18} />
            )}
          </button>
          <button
            type="button"
            className="tp-form-page-icon-button"
            onClick={handleCancel}
            disabled={loading}
            title="Cancel"
            aria-label="Cancel"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
      </div>
      <form
        id="change-password-form"
        className="tp-card tp-change-password-card-attached"
        onSubmit={submit}
        noValidate
      >
        <div className="row g-3 tp-change-password-row">
          <div className="col-md-4">
            <PasswordTextbox
              name="current"
              label="Current Password"
              required
              type="password"
              icon="bi-key-fill"
              showPasswordToggle
              value={form.current}
              onChange={(event) => update("current", event.target.value)}
              error={errors.current || apiCurrentPasswordError}
              placeholder="Enter Current Password"
              disabled={loading}
            />
          </div>
          <div className="col-md-4">
            <PasswordTextbox
              name="next"
              label="New Password"
              required
              type="password"
              icon="bi-key-fill"
              showPasswordToggle
              value={form.next}
              onChange={(event) => update("next", event.target.value)}
              error={errors.next}
              placeholder="Enter New Password"
              disabled={loading}
            />
          </div>
          <div className="col-md-4">
            <PasswordTextbox
              name="confirm"
              label="Confirm New Password"
              required
              type="password"
              icon="bi-key-fill"
              showPasswordToggle
              value={form.confirm}
              onChange={(event) => update("confirm", event.target.value)}
              error={errors.confirm}
              placeholder="Re-enter New Password"
              disabled={loading}
            />
          </div>
        </div>
      </form>
    </div>
  );
}
