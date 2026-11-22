import FormLabel from "./FormLabel";
export default function Dropdown({
  id,
  name,
  label,
  value = "",
  onChange,
  options = [],
  error,
  required,
  placeholder = "Select",
  disabled,
}) {
  const inputId = id || name;
  return (
    <div className="tp-form-field">
      {label && (
        <FormLabel htmlFor={inputId} required={required}>
          {label}
        </FormLabel>
      )}
      <select
        id={inputId}
        name={name}
        value={value}
        onChange={onChange}
        disabled={disabled}
        className={`tp-form-control ${error ? "is-invalid" : ""} ${value === "" ? "is-placeholder" : ""}`}
      >
        <option value="">{placeholder}</option>
        {options.map((option) => (
          <option key={String(option.value)} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {error && <div className="tp-field-error">{error}</div>}
    </div>
  );
}
