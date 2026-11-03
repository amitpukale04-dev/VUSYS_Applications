import { useEffect } from "react";
import { createPortal } from "react-dom";
import SvgIcon from "../../components/common/SvgIcon";
import { TextArea, TextBox } from "../../components/forms/FormElements";

// ============================================================
// CANCEL LEAVE REQUEST MODAL
// ============================================================

export default function LeaveRequestCancelModal({
  show,
  row,
  reason,
  reasonError,
  apiFieldErrors = {},
  loading,
  onReasonChange,
  onCancel,
  onSubmit,
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
  }, [show, loading, onCancel]);

  if (!show || !row) return null;

  return createPortal(
    <div
      className="tp-confirm-layer tp-leave-details-layer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="cancel-leave-request-title"
    >
      <div className="tp-confirm-backdrop" aria-hidden="true" />
      <div className="tp-confirm-card tp-confirm-card-titlebar tp-leave-details-popup">
        <div className="tp-confirm-titlebar">
          <div className="tp-confirm-titlebar-text">
            <SvgIcon name="leaveType" size={19} />
            <span id="cancel-leave-request-title">Cancel Leave Request</span>
          </div>
          <button
            type="button"
            disabled={loading}
            onClick={onCancel}
            aria-label="Close Cancel Leave Request"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>

        <div className="tp-leave-details-body">
          {[
            apiFieldErrors.employeeCode,
            apiFieldErrors.actionType,
            apiFieldErrors.actionBy,
          ].filter(Boolean).length > 0 && (
            <div className="alert alert-danger" role="alert">
              {[
                apiFieldErrors.employeeCode,
                apiFieldErrors.actionType,
                apiFieldErrors.actionBy,
              ]
                .filter(Boolean)
                .map((message, index) => (
                  <div key={index}>{message}</div>
                ))}
            </div>
          )}
          <div className="row g-3">
            <div className="col-md-6">
              <TextBox
                name="cancelLeaveType"
                label="Leave Type"
                value={row.leaveType || ""}
                readOnly
              />
            </div>
            <div className="col-md-6">
              <TextBox
                name="cancelTotalLeaveDays"
                label="Total No of Leave Days"
                value={String(row.totalLeaveDays ?? "")}
                readOnly
              />
            </div>
            <div className="col-md-6">
              <TextBox
                name="cancelFromDate"
                label="From Date"
                value={row.fromDate || ""}
                readOnly
              />
            </div>
            <div className="col-md-6">
              <TextBox
                name="cancelToDate"
                label="To Date"
                value={row.toDate || ""}
                readOnly
              />
            </div>
            <div className="col-12">
              <TextArea
                name="cancellationReason"
                label="Cancellation Reason"
                required
                rows={3}
                value={reason}
                onChange={(event) => onReasonChange?.(event.target.value)}
                error={reasonError}
                disabled={loading}
                placeholder="Enter Cancellation Reason"
              />
            </div>
            <div className="col-12">
              <div className="d-flex align-items-start gap-2 text-muted">
                <small>
                  <strong>Note:</strong> Total No of Leave Days excludes weekends
                  (Saturdays and Sundays) and holidays listed in the Holiday
                  Calendar.
                </small>
              </div>
            </div>
          </div>
        </div>

        <div className="tp-leave-details-footer tp-confirm-actions">
          <button
            type="button"
            className="tp-confirm-no"
            onClick={onCancel}
            disabled={loading}
          >
            <SvgIcon name="close" size={17} /> Cancel
          </button>
          <button
            type="button"
            className="tp-confirm-yes"
            onClick={onSubmit}
            disabled={loading}
          >
            {loading ? (
              <>
                <span
                  className="spinner-border spinner-border-sm"
                  aria-hidden="true"
                />
                Please wait...
              </>
            ) : (
              <>
                <SvgIcon name="yes" size={17} /> Cancel Leave
              </>
            )}
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
