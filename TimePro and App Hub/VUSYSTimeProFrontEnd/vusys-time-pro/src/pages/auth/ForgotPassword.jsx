import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import ToastService from "../../services/toastService";
import { forgotPasswordRequest } from "../../services/authService";
import useFormValidation from "../../hooks/useFormValidation";
import { forgotPasswordValidationRules } from "../../validation/authValidationRules";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { showApiError } from "../../utils/showApiError";
import AuthShell from "./AuthShell";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

// ============================================================
// API VALIDATION FIELD MAPPINGS
// ============================================================

const FORGOT_PASSWORD_API_FIELD_MAPPINGS = {
  email: {
    apiKeys: ["EmailID"],
  },
};

const NavigateLogin = ROUTE_CONSTANTS.AUTH.LOGIN;

export default function ForgotPassword() {
  const [form, setForm] = useState({ email: "" });
  const [loading, setLoading] = useState(false);
  const [apiEmailError, setApiEmailError] = useState(false);
  const { errors, validate, clearError, setFieldError } = useFormValidation(
    forgotPasswordValidationRules,
  );
  const navigate = useNavigate();
  const updateEmail = (value) => {
    setForm({ email: value });
    clearError("email");
    setApiEmailError(false);
  };

  const submit = async (event) => {
    event.preventDefault();
    if (loading || !validate(form)) {
      return;
    }
    setApiEmailError(false);
    setLoading(true);
    try {
      const result = await forgotPasswordRequest(form);
      ToastService.success(result.message);
      navigate(NavigateLogin, {
        replace: true,
      });
    } catch (error) {
      /*
       * ValidationErrors are already displayed globally by axiosSetup.js.
       * Apply each API field error locally to highlight the corresponding
       * control and display its validation message.
       */
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          FORGOT_PASSWORD_API_FIELD_MAPPINGS,
          setFieldError,
        );
        return;
      }

      /*
       * Network/CORS/GLOBAL_ERROR_RESPONSE errors may already be displayed
       * globally. Business errors should highlight the Email textbox.
       */
      if (error?._globalToastHandled || error?.__globalToastHandled) {
        return;
      }

      setApiEmailError(true);
      showApiError(error, "Unable to process forgot password request.");
    } finally {
      setLoading(false);
    }
  };

  const hasEmailError = Boolean(errors.email || apiEmailError);

  return (
    <AuthShell
      title="Reset Your Password"
      subtitle="We will send a secure reset link to your email id"
    >
      <form onSubmit={submit} noValidate>
        <div className="tp-form-group">
          <label htmlFor="forgot-email" className="tp-mandatory-mark">
            Email ID
          </label>

          <div className={`tp-input-wrap ${hasEmailError ? "is-invalid" : ""}`}>
            <i className="bi bi-envelope-fill" />

            <input
              id="forgot-email"
              type="email"
              value={form.email}
              onChange={(event) => updateEmail(event.target.value)}
              placeholder="Enter Your Email ID"
              disabled={loading}
              aria-invalid={hasEmailError}
              aria-describedby={errors.email ? "forgot-email-error" : undefined}
            />
          </div>

          {errors.email && (
            <div id="forgot-email-error" className="tp-field-error">
              {errors.email}
            </div>
          )}
        </div>

        <button
          type="submit"
          className="tp-primary-button w-100"
          disabled={loading}
        >
          {loading ? (
            <>
              <span
                className="spinner-border spinner-border-sm"
                aria-hidden="true"
              />{" "}
              Please wait...
            </>
          ) : (
            <>
              Send Reset Link <i className="bi bi-send-fill" />
            </>
          )}
        </button>
      </form>

      <Link className="tp-back-link" to={NavigateLogin}>
        <i className="bi bi-arrow-left" /> Back to sign in
      </Link>
    </AuthShell>
  );
}
