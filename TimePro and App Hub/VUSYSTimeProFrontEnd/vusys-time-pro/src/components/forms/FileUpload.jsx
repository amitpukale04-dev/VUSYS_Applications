import { useRef } from "react";
import FormLabel from "./FormLabel";
export default function FileUpload({
  id,
  name,
  label,
  onChange,
  accept,
  multiple = false,
  error,
  required,
}) {
  const inputRef = useRef(null);
  return (
    <div className="tp-form-field">
      <FormLabel htmlFor={id || name} required={required}>
        {label}
      </FormLabel>
      <input
        ref={inputRef}
        id={id || name}
        name={name}
        type="file"
        accept={accept}
        multiple={multiple}
        onChange={onChange}
        hidden
      />
      <button
        type="button"
        className={`tp-file-upload ${error ? "is-invalid" : ""}`}
        onClick={() => inputRef.current?.click()}
      >
        <i className="bi bi-cloud-arrow-up-fill" />
        <span>
          <strong>Choose file</strong>
          <small>Click to browse from your device</small>
        </span>
      </button>
      {error && <div className="tp-field-error">{error}</div>}
    </div>
  );
}
