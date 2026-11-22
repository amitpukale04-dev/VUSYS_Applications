import FormLabel from "./FormLabel";
export default function TextArea({
  id,
  name,
  label,
  value = "",
  onChange,
  error,
  required,
  placeholder,
  disabled,
  readOnly = false,
  rows = 3,
  maxLength,
  onKeyDown,
  ariaLabel,
}) {
  const inputId = id || name;
  return (
    <div className="tp-form-field">
      {label && (
        <FormLabel htmlFor={inputId} required={required}>
          {label}
        </FormLabel>
      )}
      <textarea
        id={inputId}
        name={name}
        value={value}
        onChange={onChange}
        placeholder={placeholder}
        disabled={disabled}
        readOnly={readOnly}
        rows={rows}
        maxLength={maxLength}
        onKeyDown={onKeyDown}
        aria-label={ariaLabel}
        className={`tp-form-control ${error ? "is-invalid" : ""}`}
      />
      {error && <div className="tp-field-error">{error}</div>}
    </div>
  );
}
