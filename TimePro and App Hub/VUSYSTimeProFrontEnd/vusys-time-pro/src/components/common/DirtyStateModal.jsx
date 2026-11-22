import { createPortal } from "react-dom";
import SvgIcon from "./SvgIcon";

export default function DirtyStateModal({
  open,
  onStay,
  onDiscard,
  title = "Unsaved Changes",
  message = "You have unsaved changes. Do you want to close without saving?",
  stayText = "No, Stay",
  discardText = "Yes, Close",
}) {
  if (!open) return null;

  return createPortal(
    <div
      className="tp-dirty-layer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="tp-dirty-title"
    >
      <div className="tp-dirty-backdrop" />
      <div className="tp-dirty-card">
        <div className="tp-dirty-titlebar">
          <div className="tp-dirty-title">
            <SvgIcon name="warning" size={19} />
            <span id="tp-dirty-title">{title}</span>
          </div>
          <button
            type="button"
            onClick={onStay}
            aria-label="Close confirmation"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
        <div className="tp-dirty-body">
          <p>{message}</p>
          <div className="tp-dirty-actions">
            <button type="button" className="tp-dirty-stay" onClick={onStay}>
              <SvgIcon name="no" size={17} /> {stayText}
            </button>
            <button
              type="button"
              className="tp-dirty-discard"
              onClick={onDiscard}
            >
              <SvgIcon name="yes" size={17} /> {discardText}
            </button>
          </div>
        </div>
      </div>
    </div>,
    document.body,
  );
}
