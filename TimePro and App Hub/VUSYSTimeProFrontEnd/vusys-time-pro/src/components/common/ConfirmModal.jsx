import { useEffect } from "react";
import { createPortal } from "react-dom";
import SvgIcon from "./SvgIcon";
import { COMMON_CONSTANTS } from "../../constants/constants";

export default function ConfirmModal({
  show,
  title,
  message,
  onConfirm,
  onCancel,
  icon = COMMON_CONSTANTS.WARNING,
  confirmText = "Yes, Confirm",
  cancelText = "No",
  loading = false,
}) {
  useEffect(() => {
    if (!show) return undefined;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const handleKeyDown = (event) => {
      if (event.key === "Escape" && !loading) onCancel?.();
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [loading, onCancel, show]);

  if (!show) return null;

  return createPortal(
    <div
      className="tp-confirm-layer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-title"
    >
      <div className="tp-confirm-backdrop" aria-hidden="true" />
      <div className="tp-confirm-card tp-confirm-card-titlebar">
        <div className="tp-confirm-titlebar">
          <div className="tp-confirm-titlebar-text">
            <SvgIcon name={icon} size={19} />
            <span id="confirm-title">{title}</span>
          </div>
          <button
            type="button"
            onClick={onCancel}
            disabled={loading}
            aria-label="Close confirmation"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>

        <div className="tp-confirm-body">
          <p>{message}</p>
          <div className="tp-confirm-actions">
            <button
              type="button"
              className="tp-confirm-no"
              onClick={onCancel}
              disabled={loading}
            >
              <SvgIcon name="no" size={17} /> {cancelText}
            </button>
            <button
              type="button"
              className="tp-confirm-yes"
              onClick={onConfirm}
              disabled={loading}
            >
              {loading ? (
                <>
                  <span
                    className="spinner-border spinner-border-sm"
                    aria-hidden="true"
                  />{" "}
                  Please wait...
                </>
              ) : (
                <>
                  <SvgIcon name="yes" size={17} /> {confirmText}
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>,
    document.body,
  );
}
