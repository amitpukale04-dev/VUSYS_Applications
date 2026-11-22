export default function FormLabel({
  htmlFor,
  children,
  required = false,
  className = "",
}) {
  return (
    <label htmlFor={htmlFor} className={`tp-form-label ${className}`}>
      {children}
      {required && <span className="tp-required-mark"> *</span>}
    </label>
  );
}
