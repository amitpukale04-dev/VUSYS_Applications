import FormLabel from "./FormLabel";
import { getCurrentDate } from "../../utils/dateUtils";

const normalizeDateValue = (value = "") => {
  const normalized = String(value || "").trim();
  return normalized.length >= 10 ? normalized.slice(0, 10) : normalized;
};

export default function DatePicker({
  id,
  name,
  label,
  value = "",
  onChange,
  error,
  required = false,
  disabled = false,
  readOnly = false,
  allowFutureDates = true,
  allowPastDates = true,
  minDate = "",
  maxDate = "",
  onBlur,
  className = "",
}) {
  const inputId = id || name;
  const today = getCurrentDate();

  const resolvedMinDate = normalizeDateValue(
    minDate || (!allowPastDates ? today : ""),
  );
  const resolvedMaxDate = normalizeDateValue(
    maxDate || (!allowFutureDates ? today : ""),
  );

  return (
    <div className={`tp-form-field ${className}`}>
      {label && (
        <FormLabel htmlFor={inputId} required={required}>
          {label}
        </FormLabel>
      )}

      <div className={`tp-form-control-wrap ${error ? "is-invalid" : ""}`}>
        <input
          id={inputId}
          name={name}
          type="date"
          value={normalizeDateValue(value)}
          onChange={onChange}
          onBlur={onBlur}
          min={resolvedMinDate || undefined}
          max={resolvedMaxDate || undefined}
          disabled={disabled}
          readOnly={readOnly}
          required={required}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? `${inputId}-error` : undefined}
        />
      </div>

      {error && (
        <div id={`${inputId}-error`} className="tp-field-error" role="alert">
          {typeof error === "string" ? error : ""}
        </div>
      )}
    </div>
  );
}
