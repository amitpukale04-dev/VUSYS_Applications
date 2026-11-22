import { useEffect } from "react";
import { createPortal } from "react-dom";
import SvgIcon from "../../components/common/SvgIcon";

export default function TimesheetSubmissionDetailsModal({
  show,
  row,
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

  if (!show || !row) return null;

  return createPortal(
    <div
      className="tp-confirm-layer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="timesheet-submission-details-title"
    >
      <div className="tp-confirm-backdrop" aria-hidden="true" />

      <div className="tp-confirm-card tp-confirm-card-titlebar tp-timesheet-details-modal">
        <div className="tp-confirm-titlebar">
          <div className="tp-confirm-titlebar-text">
            <SvgIcon name="approval" size={19} />
            <span id="timesheet-submission-details-title">
              Approved/Rejected Details
            </span>
          </div>

          <button
            type="button"
            onClick={onClose}
            aria-label="Close Approved/Rejected Details"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>

        <div className="tp-timesheet-details-body">
          <div className="tp-timesheet-details-row">
            <span>Approval Status</span>
            <strong>{row.timesheetApprovalStatus || "-"}</strong>
          </div>

          <div className="tp-timesheet-details-row">
            <span>Approved/Rejected By</span>
            <strong>{row.approvedOrRejectedByUser || "-"}</strong>
          </div>

          <div className="tp-timesheet-details-row">
            <span>Approved/Rejected Date &amp; Time</span>
            <strong>{row.approvedOrRejectedDateTime || "-"}</strong>
          </div>

          <div className="tp-timesheet-details-row">
            <span>Approved/Rejected Reason</span>
            <strong>{row.approvedOrRejectedReason || "-"}</strong>
          </div>
        </div>

        <div className="tp-timesheet-details-footer tp-confirm-actions">
          <button type="button" className="tp-confirm-no" onClick={onClose}>
            <SvgIcon name="close" size={17} /> Close
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
