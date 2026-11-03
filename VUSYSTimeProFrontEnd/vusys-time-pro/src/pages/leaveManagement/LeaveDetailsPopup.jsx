import { useEffect } from "react";
import { createPortal } from "react-dom";
import SvgIcon from "../../components/common/SvgIcon";
import { TextArea, TextBox } from "../../components/forms/FormElements";

// ============================================================
// REUSABLE LEAVE DETAILS POPUP
// ============================================================

export default function LeaveDetailsPopup({ show, row, onClose }) {
  useEffect(() => {
    if (!show) return undefined;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const onKeyDown = (event) => {
      if (event.key === "Escape") onClose?.();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [show, onClose]);

  if (!show || !row) return null;

  return createPortal(
    <div
      className="tp-confirm-layer tp-leave-details-layer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="leave-details-popup-title"
    >
      <div className="tp-confirm-backdrop" aria-hidden="true" />
      <div className="tp-confirm-card tp-confirm-card-titlebar tp-leave-details-popup">
        <div className="tp-confirm-titlebar">
          <div className="tp-confirm-titlebar-text">
            <SvgIcon name="leaveType" size={19} />
            <span id="leave-details-popup-title">Leave Details</span>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close Leave Details"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>
        <div className="tp-leave-details-body">
          <div className="row g-3">
            <div className="col-md-6">
              <TextBox
                name="detailsLeaveType"
                label="Leave Type"
                value={row.leaveType || ""}
                readOnly
              />
            </div>
            <div className="col-md-6">
              <TextBox
                name="detailsTotalLeaveDays"
                label="Total No of Leave Days"
                value={String(row.totalLeaveDays ?? "")}
                readOnly
              />
            </div>
            <div className="col-md-6">
              <TextBox
                name="detailsFromDate"
                label="From Date"
                value={row.fromDate || ""}
                readOnly
              />
            </div>
            <div className="col-md-6">
              <TextBox
                name="detailsToDate"
                label="To Date"
                value={row.toDate || ""}
                readOnly
              />
            </div>
            <div className="col-12">
              <TextArea
                name="detailsLeaveReason"
                label="Leave Reason"
                value={row.reason || ""}
                rows={4}
                readOnly
              />
            </div>

            <div className="col-12">
              <div className="d-flex align-items-start gap-2 text-muted">
                <small>
                  <strong>Note:</strong> Total No of Leave Days excludes
                  weekends (Saturdays and Sundays) and holidays listed in the
                  Holiday Calendar.
                </small>
              </div>
            </div>
          </div>
        </div>
        <div className="tp-leave-details-footer tp-confirm-actions">
          <button type="button" className="tp-confirm-no" onClick={onClose}>
            <SvgIcon name="close" size={17} /> Close
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
