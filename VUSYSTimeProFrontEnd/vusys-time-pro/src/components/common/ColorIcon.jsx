export default function ColorIcon({
  icon,
  tone = "indigo",
  size = "md",
  className = "",
}) {
  return (
    <span
      className={`tp-color-icon tp-icon-${tone} tp-icon-${size} ${className}`}
    >
      <i className={`bi ${icon}`} />
    </span>
  );
}
