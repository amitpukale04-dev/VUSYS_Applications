import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate, useSearchParams } from "react-router-dom";
import AuthShell from "./AuthShell";
import ToastService from "../../services/toastService";
import {
  checkForPasswordLinkAvailable,
  resetPasswordRequest,
} from "../../services/authService";
import { useAuth } from "../../context/AuthContext";
import { useFullPageLoader } from "../../context/FullPageLoaderContext";
import useFormValidation from "../../hooks/useFormValidation";
import PasswordTextbox from "../../components/forms/PasswordTextbox";
import { resetPasswordValidationRules } from "../../validation/authValidationRules";
import { applyApiValidationErrors } from "../../utils/CustomAPIValidation";
import { b64Decode } from "../../utils/base64EncodeDecode";
import { formatDateTime } from "../../utils/dateUtils";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

// ============================================================
// MAP API VALIDATION KEYS TO RESET PASSWORD FIELDS
// ============================================================
const RESET_PASSWORD_API_FIELD_MAPPINGS = {
  password: {
    apiKeys: ["NewPassword"],
  },
  confirmPassword: {
    apiKeys: ["ConfirmPassword"],
  },
  email: {
    apiKeys: ["EmailID"],
  },
};

const NavigateLogin = ROUTE_CONSTANTS.AUTH.LOGIN;

const normalizeEncodedQueryValue = (value) =>
  String(value || "")
    .trim()
    .replace(/ /g, "+");

const decodeQueryValue = (value) => {
  const normalized = normalizeEncodedQueryValue(value);
  return normalized ? String(b64Decode(normalized) || "").trim() : "";
};

const parsePasswordExpiryDateTime = (value) => {
  const normalized = String(value || "").trim();
  const match = normalized.match(
    /^(\d{1,2})\/(\d{1,2})\/(\d{4})\s+(\d{1,2}):(\d{2}):(\d{2})$/,
  );

  if (!match) {
    return null;
  }

  const day = Number(match[1]);
  const month = Number(match[2]);
  const year = Number(match[3]);
  const hours = Number(match[4]);
  const minutes = Number(match[5]);
  const seconds = Number(match[6]);

  const parsed = new Date(year, month - 1, day, hours, minutes, seconds);

  if (
    Number.isNaN(parsed.getTime()) ||
    parsed.getFullYear() !== year ||
    parsed.getMonth() !== month - 1 ||
    parsed.getDate() !== day ||
    parsed.getHours() !== hours ||
    parsed.getMinutes() !== minutes ||
    parsed.getSeconds() !== seconds
  ) {
    return null;
  }

  return parsed;
};

const buildResetLinkInfo = (searchParams, forcedReset, user) => {
  if (forcedReset) {
    const email = String(
      user?.email || user?.EmailID || user?.emailID || "",
    ).trim();

    return {
      isForcedReset: true,
      isValid: Boolean(email),
      email,
      resetPasswordLinkInMinutes: "",
      passwordExpiryDateTime: "",
      expiryDate: null,
      invalidReason: email ? "" : "Email ID is not available.",
    };
  }

  const email = decodeQueryValue(searchParams.get("EmailID"));
  const resetPasswordLinkInMinutes = decodeQueryValue(
    searchParams.get("ResetPasswordLinkInMinutes"),
  );
  const passwordExpiryDateTime = decodeQueryValue(
    searchParams.get("PasswordExpiryDateTime"),
  );

  const expiryDate = parsePasswordExpiryDateTime(passwordExpiryDateTime);
  const resetMinutes = Number(resetPasswordLinkInMinutes);

  const isValid = Boolean(
    email &&
    resetPasswordLinkInMinutes &&
    Number.isFinite(resetMinutes) &&
    resetMinutes > 0 &&
    expiryDate,
  );

  return {
    isForcedReset: false,
    isValid,
    email,
    resetPasswordLinkInMinutes,
    passwordExpiryDateTime,
    expiryDate,
    invalidReason: isValid ? "" : "Reset Password Link is Invalid.",
  };
};

export default function ResetPassword() {
  const [form, setForm] = useState({
    password: "",
    confirmPassword: "",
  });

  const [loading, setLoading] = useState(false);
  const [checkingPasswordLink, setCheckingPasswordLink] = useState(false);
  const [passwordLinkVerified, setPasswordLinkVerified] = useState(false);
  const [passwordLinkCheckCompleted, setPasswordLinkCheckCompleted] =
    useState(false);
  const [apiFieldErrors, setApiFieldErrors] = useState({
    password: false,
    confirmPassword: false,
    email: false,
  });

  const [searchParams] = useSearchParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { showFullPageLoader, hideFullPageLoader } = useFullPageLoader();
  const forcedReset = Boolean(location.state?.forcedReset);

  const resetLinkInfo = useMemo(
    () => buildResetLinkInfo(searchParams, forcedReset, user),
    [searchParams, forcedReset, user],
  );

  const [isLinkExpired, setIsLinkExpired] = useState(() =>
    resetLinkInfo.expiryDate
      ? Date.now() >= resetLinkInfo.expiryDate.getTime()
      : false,
  );

  const { errors, validate, clearError, setFieldError } = useFormValidation(
    resetPasswordValidationRules,
  );

  useEffect(() => {
    let active = true;

    const verifyPasswordLink = async () => {
      /*
       * Forced password reset is an authenticated flow and does not use the
       * email reset-link availability API. Only the public URL flow is
       * verified here.
       */
      if (resetLinkInfo.isForcedReset) {
        setPasswordLinkVerified(true);
        setPasswordLinkCheckCompleted(true);
        return;
      }

      /*
       * The backend availability check must run whenever the EmailID can be
       * decoded from the URL. Do not block this API call because some other
       * reset-link query parameter is invalid or because the encoded expiry
       * date has already passed. The API is the authoritative availability
       * check requested for the public reset-password URL.
       */
      if (!resetLinkInfo.email) {
        setPasswordLinkVerified(false);
        setPasswordLinkCheckCompleted(true);
        return;
      }

      const loaderRequestId = showFullPageLoader(
        "Verifying Reset Password Link...",
      );
      setCheckingPasswordLink(true);
      setPasswordLinkVerified(false);
      setPasswordLinkCheckCompleted(false);

      try {
        await checkForPasswordLinkAvailable({
          email: resetLinkInfo.email,
        });

        if (!active) return;

        setPasswordLinkVerified(true);
        setPasswordLinkCheckCompleted(true);
      } catch (error) {
        if (!active) return;

        setPasswordLinkVerified(false);
        setPasswordLinkCheckCompleted(true);

        /*
         * Do not redirect to Login when the backend reports that the reset
         * password link is unavailable/expired. Keep the user on this page
         * and reuse the existing expired-link status UI below.
         */
        setIsLinkExpired(true);
        showApiError(error, "Reset Password Link is not available.");
      } finally {
        if (active) {
          setCheckingPasswordLink(false);
        }
        hideFullPageLoader(loaderRequestId);
      }
    };

    verifyPasswordLink();

    return () => {
      active = false;
    };
  }, [resetLinkInfo, navigate, showFullPageLoader, hideFullPageLoader]);

  useEffect(() => {
    if (resetLinkInfo.isForcedReset || !resetLinkInfo.expiryDate) {
      setIsLinkExpired(false);
      return undefined;
    }

    const expiryTime = resetLinkInfo.expiryDate.getTime();
    const remaining = expiryTime - Date.now();

    if (remaining <= 0) {
      setIsLinkExpired(true);
      return undefined;
    }

    setIsLinkExpired(false);

    const timer = window.setTimeout(() => {
      setIsLinkExpired(true);
    }, remaining);

    return () => window.clearTimeout(timer);
  }, [resetLinkInfo]);

  const update = (name, value) => {
    setForm((current) => ({
      ...current,
      [name]: value,
    }));

    clearError(name);

    setApiFieldErrors((current) => ({
      ...current,
      [name]: false,
    }));
  };

  const applyApiValidationToFields = (validationErrors) => {
    applyApiValidationErrors(
      validationErrors,
      RESET_PASSWORD_API_FIELD_MAPPINGS,
      (fieldName, message) => {
        if (fieldName === "email") {
          // Email ID is read-only; highlight the reset-link details instead.
          setApiFieldErrors((current) => ({
            ...current,
            email: true,
          }));
          return;
        }

        setFieldError(fieldName, message);
      },
    );
  };

  const submit = async (event) => {
    event.preventDefault();
    if (loading) {
      return;
    }
    if (!resetLinkInfo.isValid) {
      ToastService.error(
        resetLinkInfo.invalidReason || "Reset Password Link is Invalid.",
      );
      return;
    }

    if (
      !resetLinkInfo.isForcedReset &&
      resetLinkInfo.expiryDate &&
      Date.now() >= resetLinkInfo.expiryDate.getTime()
    ) {
      setIsLinkExpired(true);
      ToastService.error("Reset Password Link is Expired.");
      return;
    }

    if (!validate(form)) {
      return;
    }

    setApiFieldErrors({
      password: false,
      confirmPassword: false,
      email: false,
    });

    const loaderRequestId = showFullPageLoader("Resetting Password...");
    setLoading(true);
    try {
      const result = await resetPasswordRequest({
        email: resetLinkInfo.email,
        newPassword: form.password,
        confirmPassword: form.confirmPassword,
      });
      ToastService.success(result.message);
      logout();

      navigate(NavigateLogin, {
        replace: true,
      });
    } catch (error) {
      if (error?.isApiValidationError) {
        applyApiValidationToFields(error.validationErrors);
        return;
      }

      /*
       * A Reset Password business error such as "Email ID Not Found !!!"
       * is related to the decoded EmailID. Since EmailID is read-only on this
       * screen, highlight the reset-link details section in red.
       */
      if (!error?._globalToastHandled && !error?.__globalToastHandled) {
        setApiFieldErrors((current) => ({
          ...current,
          email: true,
        }));
        showApiError(error, "Unable To Reset The Password.");
      }
    } finally {
      hideFullPageLoader(loaderRequestId);
      setLoading(false);
    }
  };

  const hasValidLink =
    resetLinkInfo.isValid &&
    !isLinkExpired &&
    (resetLinkInfo.isForcedReset || passwordLinkVerified);

  if (
    !resetLinkInfo.isForcedReset &&
    resetLinkInfo.email &&
    (checkingPasswordLink || !passwordLinkCheckCompleted)
  ) {
    return null;
  }

  if (
    !resetLinkInfo.isForcedReset &&
    (!resetLinkInfo.isValid || isLinkExpired)
  ) {
    return (
      <AuthShell
        title="Set New Password"
        subtitle="Create a strong new password for your Time-Pro Account"
      >
        <div className="tp-reset-link-status is-expired" role="alert">
          <div className="tp-reset-link-status-icon">
            <i className="bi bi-link-45deg" />
          </div>

          <div className="tp-reset-link-status-content">
            <h3>
              {isLinkExpired
                ? "Reset Password Link is Expired."
                : "Reset Password Link is Invalid."}
            </h3>

            <p>
              {isLinkExpired
                ? "This reset password link is no longer valid. Please request a new reset password link."
                : "The reset password link is missing or contains invalid information."}
            </p>
          </div>
        </div>

        <div className="tp-reset-link-details is-error">
          <div className="tp-reset-link-detail-row">
            <span>Email ID</span>
            <strong>{resetLinkInfo.email || "Not available"}</strong>
          </div>

          <div className="tp-reset-link-detail-row">
            <span>Password Expiry Date Time</span>
            <strong>
              {resetLinkInfo.expiryDate
                ? formatDateTime(resetLinkInfo.expiryDate)
                : resetLinkInfo.passwordExpiryDateTime || "Not available"}
            </strong>
          </div>
        </div>
      </AuthShell>
    );
  }

  return (
    <AuthShell
      title="Set New Password"
      subtitle="Create a strong new password for your Time-Pro Account"
    >
      <form onSubmit={submit} noValidate>
        <div className="tp-form-group">
          <PasswordTextbox
            id="reset-password"
            name="password"
            label="New Password"
            required
            type="password"
            icon="bi-lock-fill"
            showPasswordToggle
            value={form.password}
            onChange={(event) => update("password", event.target.value)}
            error={errors.password}
            placeholder="Enter new password"
            disabled={loading || !hasValidLink}
            autoComplete="new-password"
          />
        </div>

        <div className="tp-form-group">
          <PasswordTextbox
            id="reset-confirmPassword"
            name="confirmPassword"
            label="Confirm Password"
            required
            type="password"
            icon="bi-lock-fill"
            showPasswordToggle
            value={form.confirmPassword}
            onChange={(event) => update("confirmPassword", event.target.value)}
            error={errors.confirmPassword}
            placeholder="Re-enter new password"
            disabled={loading || !hasValidLink}
            autoComplete="new-password"
          />
        </div>

        {!resetLinkInfo.isForcedReset && (
          <div
            className={`tp-reset-link-details ${
              apiFieldErrors.email ? "is-error" : ""
            }`}
          >
            <div className="tp-reset-link-details-title">
              <i className="bi bi-info-circle-fill" />
              <span>Reset Password Link Details</span>
            </div>

            <div className="tp-reset-link-detail-row">
              <span>Email ID</span>
              <strong>{resetLinkInfo.email}</strong>
            </div>

            <div className="tp-reset-link-detail-row">
              <span>Reset Password Link In Minutes</span>
              <strong>{resetLinkInfo.resetPasswordLinkInMinutes}</strong>
            </div>

            <div className="tp-reset-link-detail-row">
              <span>Password Expiry Date Time</span>
              <strong>{formatDateTime(resetLinkInfo.expiryDate)}</strong>
            </div>
          </div>
        )}

        <button
          type="submit"
          className="tp-primary-button w-100"
          disabled={loading || !hasValidLink}
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
              Change Password <i className="bi bi-shield-check" />
            </>
          )}
        </button>
      </form>
    </AuthShell>
  );
}
