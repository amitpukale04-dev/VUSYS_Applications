import FormLabel from "./FormLabel";

export default function RadioGroup({
  name,
  label,
  value,
  onChange,
  options = [],
  error,
  required,
  inline = true,
  disabled = false,
}) {
  return (
    <div className="tp-form-field tp-radio-field">
      {label && <FormLabel required={required}>{label}</FormLabel>}
      <div className={`tp-radio-control ${error ? "is-invalid" : ""}`}>
        <div className={`tp-radio-group ${inline ? "inline" : ""}`}>
          {options.map((option) => (
            <label key={option.value} className={disabled ? "disabled" : ""}>
              <input
                type="radio"
                name={name}
                value={option.value}
                checked={value === option.value}
                onChange={onChange}
                disabled={disabled}
              />
              <span />
              {option.label}
            </label>
          ))}
        </div>
        {error && <div className="tp-field-error tp-radio-error">{error}</div>}
      </div>
    </div>
  );
}
