import { useState } from "react";
import FormLabel from "./FormLabel";

export default function TextBox({
  id,
  name,
  label,
  value = "",
  onChange,
  error,
  required,
  icon,
  type = "text",
  placeholder,
  disabled,
  readOnly,
  maxLength,
  min,
  max,
  step,
  inputMode,
  onBlur,
  className = "",
  showPasswordToggle = false,
  allowPasswordToggleWhenReadOnly = false,
}) {
  const [showPassword, setShowPassword] = useState(false);

  const inputId = id || name;

  const isPasswordField = type === "password";

  const inputType =
    isPasswordField && showPasswordToggle && showPassword ? "text" : type;

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
        <FormLabel htmlFor={inputId} required={required}>
          {label}
        </FormLabel>
      )}

      <div className={`tp-form-control-wrap ${error ? "is-invalid" : ""}`}>
        {icon && <i className={`bi ${icon}`} aria-hidden="true" />}

        <input
          id={inputId}
          name={name}
          type={inputType}
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
          aria-invalid={Boolean(error)}
          aria-describedby={error ? `${inputId}-error` : undefined}
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
