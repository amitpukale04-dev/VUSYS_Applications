export default function FormButton({
  children,
  type = "button",
  variant = "primary",
  icon,
  loading = false,
  disabled,
  onClick,
  className = "",
}) {
  return (
    <button
      type={type}
      className={`tp-form-button tp-form-button-${variant} ${className}`}
      disabled={disabled || loading}
      onClick={onClick}
    >
      {loading ? (
        <>
          <span className="spinner-border spinner-border-sm" /> Please wait...
        </>
      ) : (
        <>
          {icon && <i className={`bi ${icon}`} />}
          {children}
        </>
      )}
    </button>
  );
}
