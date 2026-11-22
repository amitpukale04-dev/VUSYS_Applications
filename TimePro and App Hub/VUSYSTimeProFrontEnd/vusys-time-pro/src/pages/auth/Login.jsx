import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import ToastService from "../../services/toastService";
import { useAuth } from "../../context/AuthContext";
import useFormValidation from "../../hooks/useFormValidation";
import { loginValidationRules } from "../../validation/authValidationRules";
import AuthShell from "./AuthShell";
import { STORAGE_KEYS } from "../../utils/storageKeys";
import { APP_CONFIG } from "../../utils/environmentConfig";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const LOGIN_API_FIELD_MAPPINGS = {
  email: { apiKeys: ["EmailID"] },
  password: { apiKeys: ["Password"] },
};

const getInitialLoginForm = () => {
  const remember = localStorage.getItem(STORAGE_KEYS.rememberLogin) === "true";
  return {
    email: remember
      ? localStorage.getItem(STORAGE_KEYS.rememberedEmail) || ""
      : "",
    password: remember
      ? localStorage.getItem(STORAGE_KEYS.rememberedPassword) || ""
      : "",
    remember,
  };
};

const NavigateResetPassword = ROUTE_CONSTANTS.AUTH.RESET_PASSWORD;
const NavigateOTPAuthentication = ROUTE_CONSTANTS.AUTH.OTP_AUTHENTICATION;
const NavigateForgotPassword = ROUTE_CONSTANTS.AUTH.FORGOT_PASSWORD;

export default function Login() {
  const [form, setForm] = useState(getInitialLoginForm);
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [apiFieldErrors, setApiFieldErrors] = useState({
    email: false,
    password: false,
  });
  const { errors, validate, clearError, setFieldError } =
    useFormValidation(loginValidationRules);
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  // ProtectedRoute supplies the originally requested URL, including its query string.
  const requestedLocation = location.state?.from;
  const destination =
    requestedLocation?.pathname?.startsWith("/") &&
    !requestedLocation.pathname.startsWith("//")
      ? `${requestedLocation.pathname}${requestedLocation.search || ""}${requestedLocation.hash || ""}`
      : "/";

  const updateField = (name, value) => {
    setForm((current) => ({ ...current, [name]: value }));
    clearError(name);

    if (name === "email" || name === "password") {
      setApiFieldErrors((current) => ({
        ...current,
        [name]: false,
      }));
    }
  };

  // Preserve business-error highlighting when the API does not return
  // structured field validation errors.
  const applyBusinessCredentialError = (error) => {
    const normalizedMessage = String(error?.message || "")
      .toLowerCase()
      .replace(/\s+/g, "");

    setApiFieldErrors({
      email: normalizedMessage.includes("emailid"),
      password: normalizedMessage.includes("password"),
    });
  };

  const submit = async (event) => {
    event.preventDefault();
    if (loading || !validate(form)) return;

    setApiFieldErrors({ email: false, password: false });
    setLoading(true);
    try {
      const result = await login(form);

      if (form.remember) {
        localStorage.setItem(STORAGE_KEYS.rememberLogin, "true");
        localStorage.setItem(STORAGE_KEYS.rememberedEmail, form.email);
        localStorage.setItem(STORAGE_KEYS.rememberedPassword, form.password);
      } else {
        localStorage.removeItem(STORAGE_KEYS.rememberLogin);
        localStorage.removeItem(STORAGE_KEYS.rememberedEmail);
        localStorage.removeItem(STORAGE_KEYS.rememberedPassword);
      }

      if (result.requiresPasswordReset) {
        ToastService.information("Please reset your password to continue");
        navigate(NavigateResetPassword, {
          replace: true,
          state: { forcedReset: true, from: requestedLocation },
        });
        return;
      }

      if (result.requiresTwoFactor) {
        ToastService.information("Enter the 4-digit OTP to continue");
        navigate(NavigateOTPAuthentication, {
          replace: true,
          state: { from: requestedLocation },
        });
        return;
      }
      const redirectHomeAfterLogout =
        sessionStorage.getItem(STORAGE_KEYS.redirectHomeAfterLogout) === "true";
      sessionStorage.removeItem(STORAGE_KEYS.redirectHomeAfterLogout);
      navigate(redirectHomeAfterLogout ? "/" : destination, { replace: true });
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationErrors(
          error.validationErrors,
          LOGIN_API_FIELD_MAPPINGS,
          setFieldError,
        );
        return;
      }

      // Preserve existing highlighting for login business errors.
      applyBusinessCredentialError(error);

      // Validation, network/CORS, and global API errors are already handled
      // by the global interceptor; show only unhandled business errors here.
      showApiError(error, "Unable to sign in.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthShell
      title="Welcome back"
      subtitle="Sign in to continue to your Time-Pro Account"
    >
      <form onSubmit={submit} noValidate>
        <div className="tp-form-group">
          <label htmlFor="login-email" className="tp-mandatory-mark">
            Email ID
          </label>
          <div
            className={`tp-input-wrap ${errors.email || apiFieldErrors.email ? "is-invalid" : ""}`}
          >
            <i className="bi bi-envelope-fill" />
            <input
              id="login-email"
              type="email"
              value={form.email}
              onChange={(event) => updateField("email", event.target.value)}
              placeholder="Enter Your Email ID"
              disabled={loading}
              aria-invalid={Boolean(errors.email || apiFieldErrors.email)}
              aria-describedby={errors.email ? "login-email-error" : undefined}
            />
          </div>
          {errors.email && (
            <div id="login-email-error" className="tp-field-error">
              {errors.email}
            </div>
          )}
        </div>

        <div className="tp-form-group">
          <div className="d-flex justify-content-between">
            <label htmlFor="login-password" className="tp-mandatory-mark">
              Password
            </label>
            <Link to={NavigateForgotPassword}>Forgot Password?</Link>
          </div>
          <div
            className={`tp-input-wrap ${errors.password || apiFieldErrors.password ? "is-invalid" : ""}`}
          >
            <i className="bi bi-lock-fill" />
            <input
              id="login-password"
              type="text"
              style={{ WebkitTextSecurity: showPassword ? "none" : "disc" }}
              data-lpignore="true"
              data-form-type="other"
              value={form.password}
              onChange={(event) => updateField("password", event.target.value)}
              placeholder="Enter Your Password"
              disabled={loading}
              aria-invalid={Boolean(errors.password || apiFieldErrors.password)}
              aria-describedby={
                errors.password ? "login-password-error" : undefined
              }
            />
            <button
              type="button"
              onClick={() => setShowPassword((current) => !current)}
              disabled={loading}
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              <i
                className={`bi ${showPassword ? "bi-eye-slash-fill" : "bi-eye-fill"}`}
              />
            </button>
          </div>
          {errors.password && (
            <div id="login-password-error" className="tp-field-error">
              {errors.password}
            </div>
          )}
        </div>

        <label className="tp-login-switch-row">
          <button
            type="button"
            role="switch"
            aria-checked={form.remember}
            className={`tp-login-switch ${form.remember ? "active" : ""}`}
            onClick={() => updateField("remember", !form.remember)}
            disabled={loading}
          >
            <span />
          </button>
          <span>Keep me signed in</span>
        </label>

        <button className="tp-primary-button w-100" disabled={loading}>
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
              Sign In <i className="bi bi-arrow-right" />
            </>
          )}
        </button>
        <div className="tp-login-version">
          {APP_CONFIG.environment} Version: {APP_CONFIG.versionNo}
        </div>
      </form>
    </AuthShell>
  );
}
