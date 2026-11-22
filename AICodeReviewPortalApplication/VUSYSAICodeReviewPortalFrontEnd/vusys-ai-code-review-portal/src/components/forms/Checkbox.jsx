export default function Checkbox({
  id,
  name,
  label,
  checked = false,
  onChange,
  disabled,
  switchStyle = false,
}) {
  return (
    <label className={switchStyle ? "tp-form-switch" : "tp-form-checkbox"}>
      <input
        id={id || name}
        name={name}
        type="checkbox"
        checked={checked}
        onChange={onChange}
        disabled={disabled}
      />
      <span
        className={
          switchStyle ? "tp-form-switch-slider" : "tp-form-checkbox-box"
        }
      >
        {!switchStyle && checked && <i className="bi bi-check" />}
      </span>
      <span>{label}</span>
    </label>
  );
}
