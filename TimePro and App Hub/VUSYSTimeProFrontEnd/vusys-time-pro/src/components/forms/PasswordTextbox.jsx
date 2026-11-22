import { useState } from "react";
import FormLabel from "./FormLabel";

export default function PasswordTextbox({
  id,
  name,
  label,
  labelRightContent,
  value = "",
  onChange,
  error,
  required,
  icon,
  type = "password",
  placeholder,
  disabled,
  readOnly,
  maxLength,
  min,
  max,
  step,
  inputMode,
  onBlur,
  autoComplete,
  className = "",
  showPasswordToggle = false,
  allowPasswordToggleWhenReadOnly = false,
}) {
  const [showPassword, setShowPassword] = useState(false);

  const inputId = id || name;

  const isPasswordField = type === "password";

  // ============================================================
  // TOGGLE PASSWORD VISIBILITY
  // ============================================================

  const togglePasswordVisibility = () => {
    if (disabled || (readOnly && !allowPasswordToggleWhenReadOnly)) {
      return;
    }

    setShowPassword((current) => !current);
  };

  // ============================================================
  // UI
  // ============================================================

  return (
    <div className={`tp-form-field ${className}`}>
      {label && (
        <div className="d-flex justify-content-between align-items-center">
          <FormLabel htmlFor={inputId} required={required}>
            {label}
          </FormLabel>

          {labelRightContent && <div>{labelRightContent}</div>}
        </div>
      )}

      <div className={`tp-form-control-wrap ${error ? "is-invalid" : ""}`}>
        {icon && <i className={`bi ${icon}`} aria-hidden="true" />}

        <input
          id={inputId}
          name={name}
          type={isPasswordField ? "text" : type}
          value={value}
          onChange={onChange}
          onBlur={onBlur}
          placeholder={placeholder}
          disabled={disabled}
          readOnly={readOnly}
          maxLength={maxLength}
          min={min}
          max={max}
          step={step}
          inputMode={inputMode}
          required={required}
          autoComplete={autoComplete}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? `${inputId}-error` : undefined}
          style={
            isPasswordField
              ? { WebkitTextSecurity: showPassword ? "none" : "disc" }
              : undefined
          }
          data-lpignore={isPasswordField ? "true" : undefined}
          data-form-type="other"
        />

        {isPasswordField && showPasswordToggle && (
          <button
            type="button"
            className="tp-password-toggle"
            onClick={togglePasswordVisibility}
            disabled={
              disabled || (readOnly && !allowPasswordToggleWhenReadOnly)
            }
            tabIndex={-1}
            title={showPassword ? "Hide Password" : "Show Password"}
            aria-label={showPassword ? "Hide Password" : "Show Password"}
            style={{
              border: "0",
              outline: "none",
              background: "transparent",
              boxShadow: "none",
              padding: "0",
              margin: "0",
              width: "32px",
              height: "32px",
              minWidth: "32px",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              flexShrink: 0,
              cursor:
                disabled || (readOnly && !allowPasswordToggleWhenReadOnly)
                  ? "default"
                  : "pointer",
            }}
          >
            <i
              className={`bi ${
                showPassword ? "bi-eye-slash-fill" : "bi-eye-fill"
              }`}
              aria-hidden="true"
              style={{
                color: "#718096",
                fontSize: "16px",
                lineHeight: 1,
              }}
            />
          </button>
        )}
      </div>

      {error && (
        <div id={`${inputId}-error`} className="tp-field-error" role="alert">
          {typeof error === "string" ? error : ""}
        </div>
      )}
    </div>
  );
}
