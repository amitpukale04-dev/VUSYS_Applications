import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import AuthShell from "./AuthShell";
import { useAuth } from "../../context/AuthContext";
import ToastService from "../../services/toastService";
import { generateOrResendOtp } from "../../services/otpService";
import { showApiError } from "../../utils/showApiError";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";
import { STORAGE_KEYS } from "../../utils/storageKeys";

const EMPTY_OTP = ["", "", "", ""];
const RESEND_OTP_SECONDS = 20;
const NavigateLogin = ROUTE_CONSTANTS.AUTH.LOGIN;

export default function OtpAuthentication() {
  const [digits, setDigits] = useState(EMPTY_OTP);
  const [loading, setLoading] = useState(false);
  const [resendLoading, setResendLoading] = useState(false);
  const [otpError, setOtpError] = useState("");
  const [resendSeconds, setResendSeconds] = useState(RESEND_OTP_SECONDS);

  const inputsRef = useRef([]);
  const autoSubmittedRef = useRef("");
  const verificationInProgressRef = useRef(false);

  const navigate = useNavigate();
  const location = useLocation();
  const requestedLocation = location.state?.from;
  const destination =
    requestedLocation?.pathname?.startsWith("/") &&
    !requestedLocation.pathname.startsWith("//")
      ? `${requestedLocation.pathname}${requestedLocation.search || ""}${requestedLocation.hash || ""}`
      : "/";

  const { pendingTwoFactorUser, verifyOtp, cancelOtp } = useAuth();

  const otp = useMemo(() => digits.join(""), [digits]);

  const emailID =
    pendingTwoFactorUser?.email ||
    pendingTwoFactorUser?.EmailID ||
    pendingTwoFactorUser?.emailID ||
    "";

  // ============================================================
  // REDIRECT TO LOGIN WHEN OTP SESSION DOES NOT EXIST
  // ============================================================

  useEffect(() => {
    if (!pendingTwoFactorUser && !verificationInProgressRef.current) {
      navigate(NavigateLogin, {
        replace: true,
        state: { from: requestedLocation },
      });
    }
  }, [navigate, pendingTwoFactorUser]);

  // ============================================================
  // RESEND OTP COUNTDOWN
  // ============================================================

  useEffect(() => {
    if (resendSeconds <= 0) {
      return undefined;
    }

    const timer = window.setInterval(() => {
      setResendSeconds((currentValue) => {
        if (currentValue <= 1) {
          window.clearInterval(timer);

          return 0;
        }
        return currentValue - 1;
      });
    }, 1000);

    return () => {
      window.clearInterval(timer);
    };
  }, [resendSeconds]);

  // ============================================================
  // FORMAT COUNTDOWN
  // ============================================================

  const formatCountdown = (totalSeconds) => {
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(
      2,
      "0",
    )}`;
  };

  // ============================================================
  // FOCUS FIRST EMPTY OTP BOX
  // ============================================================

  const focusFirstEmptyInput = (values = digits) => {
    const emptyIndex = values.findIndex((digit) => !digit);
    const targetIndex = emptyIndex >= 0 ? emptyIndex : 0;
    requestAnimationFrame(() => {
      inputsRef.current[targetIndex]?.focus();
    });
  };

  // ============================================================
  // CLEAR OTP
  // ============================================================

  const clearOtp = () => {
    setDigits([...EMPTY_OTP]);
    setOtpError("");
    autoSubmittedRef.current = "";
    requestAnimationFrame(() => {
      inputsRef.current[0]?.focus();
    });
  };

  // ============================================================
  // VERIFY OTP
  // ============================================================

  const submitOtp = async (value = otp) => {
    if (loading || resendLoading) {
      return;
    }

    if (!/^\d{4}$/.test(value)) {
      setOtpError("Enter the Valid OTP");

      autoSubmittedRef.current = "";

      focusFirstEmptyInput();

      return;
    }

    setOtpError("");

    setLoading(true);

    verificationInProgressRef.current = true;

    try {
      await verifyOtp(value);
      const redirectHomeAfterLogout =
        sessionStorage.getItem(STORAGE_KEYS.redirectHomeAfterLogout) === "true";
      sessionStorage.removeItem(STORAGE_KEYS.redirectHomeAfterLogout);
      navigate(redirectHomeAfterLogout ? "/" : destination, {
        replace: true,
      });
    } catch (error) {
      verificationInProgressRef.current = false;
      setOtpError("Enter The Valid OTP.");
      autoSubmittedRef.current = value;
      ToastService.error(error?.message || "Enter The Valid OTP.");
      requestAnimationFrame(() => {
        inputsRef.current[0]?.focus();
        inputsRef.current[0]?.select();
      });
    } finally {
      setLoading(false);
    }
  };

  // ============================================================
  // AUTOMATIC OTP VERIFY
  // ============================================================

  useEffect(() => {
    if (otp.length === 4 && otp !== autoSubmittedRef.current) {
      autoSubmittedRef.current = otp;
      submitOtp(otp);
    }
  }, [otp]);

  // ============================================================
  // UPDATE OTP DIGIT
  // ============================================================

  const updateDigit = (index, value) => {
    const digit = value.replace(/\D/g, "").slice(-1);
    const nextDigits = digits.map((item, itemIndex) =>
      itemIndex === index ? digit : item,
    );
    setDigits(nextDigits);
    setOtpError("");
    autoSubmittedRef.current = "";
    if (digit && index < 3) {
      inputsRef.current[index + 1]?.focus();
    }
  };

  // ============================================================
  // PASTE OTP
  // ============================================================

  const handlePaste = (event) => {
    const pasted = event.clipboardData
      .getData("text")
      .replace(/\D/g, "")
      .slice(0, 4);

    if (!pasted) {
      return;
    }

    event.preventDefault();
    const nextDigits = Array.from(
      {
        length: 4,
      },
      (_, index) => pasted[index] || "",
    );

    setDigits(nextDigits);
    setOtpError("");
    autoSubmittedRef.current = "";
    const focusIndex = Math.max(Math.min(pasted.length, 4) - 1, 0);
    inputsRef.current[focusIndex]?.focus();
  };

  // ============================================================
  // RESEND OTP
  // ============================================================

  const resendOtp = async () => {
    if (resendLoading || loading || resendSeconds > 0) {
      return;
    }
    if (!emailID) {
      ToastService.error("Email ID is Not Available !!!");
      return;
    }
    setResendLoading(true);
    setOtpError("");

    try {
      const result = await generateOrResendOtp(emailID);
      const encodedOtp = result?.otp || "";
      if (encodedOtp && pendingTwoFactorUser) {
        pendingTwoFactorUser.otp = encodedOtp;
        if (Object.prototype.hasOwnProperty.call(pendingTwoFactorUser, "OTP")) {
          pendingTwoFactorUser.OTP = encodedOtp;
        }
      }
      clearOtp();
      setResendSeconds(RESEND_OTP_SECONDS);
      ToastService.success(
        result?.message || "OTP Has Been Sent To Your Email ID !!!",
      );
    } catch (error) {
      showApiError(error, "Unable To Resend OTP !!!");
    } finally {
      setResendLoading(false);
    }
  };

  // ============================================================
  // CANCEL OTP
  // ============================================================

  const cancel = () => {
    verificationInProgressRef.current = false;
    cancelOtp();
    setDigits([...EMPTY_OTP]);
    setOtpError("");
    setResendSeconds(RESEND_OTP_SECONDS);
    navigate(NavigateLogin, {
      replace: true,
      state: { from: requestedLocation },
    });
  };

  // ============================================================
  // RESEND STATE
  // ============================================================

  const resendDisabled = resendSeconds > 0 || resendLoading || loading;

  return (
    <AuthShell
      title="OTP Authentication"
      subtitle={`Enter the 4-digit OTP sent to ${emailID || "your email"}`}
    >
      <form
        noValidate
        onSubmit={(event) => {
          event.preventDefault();

          submitOtp();
        }}
      >
        <div className={`tp-otp-control${otpError ? " is-invalid" : ""}`}>
          <div className="tp-otp-inputs" onPaste={handlePaste}>
            {digits.map((digit, index) => (
              <input
                key={index}
                ref={(element) => {
                  inputsRef.current[index] = element;
                }}
                type="text"
                inputMode="numeric"
                autoComplete={index === 0 ? "one-time-code" : "off"}
                maxLength="1"
                value={digit}
                disabled={loading || resendLoading}
                aria-label={`OTP digit ${index + 1}`}
                aria-invalid={Boolean(otpError)}
                onChange={(event) => updateDigit(index, event.target.value)}
                onFocus={(event) => event.target.select()}
                onKeyDown={(event) => {
                  if (event.key === "Backspace" && !digit && index > 0) {
                    inputsRef.current[index - 1]?.focus();
                  }

                  if (event.key === "ArrowLeft" && index > 0) {
                    inputsRef.current[index - 1]?.focus();
                  }

                  if (event.key === "ArrowRight" && index < 3) {
                    inputsRef.current[index + 1]?.focus();
                  }
                }}
                autoFocus={index === 0}
              />
            ))}
          </div>

          {otpError && (
            <div className="tp-otp-error" role="alert">
              {otpError}
            </div>
          )}
        </div>

        <div
          className="tp-otp-resend"
          style={{
            display: "flex",
            justifyContent: "center",
            marginTop: "14px",
            marginBottom: "8px",
          }}
        >
          <div
            style={{
              display: "inline-flex",
              alignItems: "center",
              justifyContent: "center",
              gap: "8px",
              whiteSpace: "nowrap",
            }}
          >
            <button
              type="button"
              onClick={resendOtp}
              disabled={resendDisabled}
              className="tp-back-link"
              style={{
                display: "inline-flex",
                alignItems: "center",
                justifyContent: "center",
                gap: "4px",
                border: 0,
                padding: 0,
                margin: 0,
                background: "transparent",
                lineHeight: 1,
                cursor: resendDisabled ? "not-allowed" : "pointer",
                opacity: resendDisabled ? 0.55 : 1,
              }}
            >
              {resendLoading ? (
                <>
                  <span
                    className="spinner-border spinner-border-sm"
                    aria-hidden="true"
                  />
                  <span>Sending OTP...</span>
                </>
              ) : (
                <>
                  <i className="bi bi-arrow-clockwise" aria-hidden="true" />
                  <span>Resend OTP</span>
                </>
              )}
            </button>

            {resendSeconds > 0 && (
              <span
                className="tp-otp-resend-timer"
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  justifyContent: "center",
                  gap: "4px",
                  margin: 0,
                  padding: 0,
                  lineHeight: 1,
                  fontSize: "0.76rem",
                  fontWeight: 800,
                  whiteSpace: "nowrap",
                  color: "#6d5dfc",
                }}
              >
                <i
                  className="bi bi-clock-fill"
                  aria-hidden="true"
                  style={{
                    display: "inline-flex",
                    alignItems: "center",
                    color: "#ff9f1c",
                    fontSize: "0.82rem",
                    lineHeight: 1,
                  }}
                />

                <span
                  style={{
                    display: "inline-flex",
                    alignItems: "center",
                    lineHeight: 1,
                  }}
                >
                  {formatCountdown(resendSeconds)}
                </span>
              </span>
            )}
          </div>
        </div>

        <div className="tp-auth-button-row tp-otp-button-row">
          <button
            type="submit"
            className="tp-primary-button"
            disabled={loading || resendLoading}
          >
            {loading ? (
              <>
                <span
                  className="spinner-border spinner-border-sm"
                  aria-hidden="true"
                />{" "}
                Verifying...
              </>
            ) : (
              <>
                Verify <i className="bi bi-shield-check" aria-hidden="true" />
              </>
            )}
          </button>

          <button
            type="button"
            className="tp-primary-button"
            onClick={cancel}
            disabled={loading || resendLoading}
          >
            Cancel <i className="bi bi-x-circle" aria-hidden="true" />
          </button>
        </div>
      </form>
    </AuthShell>
  );
}
