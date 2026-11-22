import { useEffect } from "react";
import { createPortal } from "react-dom";
import SvgIcon from "./SvgIcon";

export default function ImagePopup({
  show,
  src = "",
  title = "Image Preview",
  fileName = "",
  alt = "Preview",
  onClose,
}) {
  useEffect(() => {
    if (!show) return undefined;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const handleKeyDown = (event) => {
      if (event.key === "Escape") onClose?.();
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [onClose, show]);

  if (!show) return null;

  return createPortal(
    <div
      className="tp-image-popup-layer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="tp-image-popup-title"
    >
      <button
        type="button"
        className="tp-image-popup-backdrop"
        aria-label="Close image preview"
        onClick={onClose}
      />

      <div className="tp-image-popup-card">
        <div className="tp-image-popup-titlebar">
          <div className="tp-image-popup-titlebar-text">
            <SvgIcon name="eye" size={18} />
            <span id="tp-image-popup-title">{title}</span>
          </div>

          <button
            type="button"
            onClick={onClose}
            aria-label="Close image preview"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>

        <div className="tp-image-popup-body">
          {src ? (
            <img src={src} alt={alt} className="tp-image-popup-image" />
          ) : (
            <div className="tp-image-popup-empty">No image available.</div>
          )}

          {fileName && (
            <div className="tp-image-popup-file-name">{fileName}</div>
          )}
        </div>
      </div>
    </div>,
    document.body,
  );
}
