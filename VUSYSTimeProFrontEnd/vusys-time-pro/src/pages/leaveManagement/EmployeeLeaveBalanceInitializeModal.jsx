import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import SvgIcon from "../../components/common/SvgIcon";
import { SearchableDropdown } from "../../components/forms/FormElements";

const currentYear = String(new Date().getFullYear());

export default function EmployeeLeaveBalanceInitializeModal({
  show,
  years = [],
  saving = false,
  onInitialize,
  onClose,
}) {
  const [leaveYear, setLeaveYear] = useState(currentYear);
  const [error, setError] = useState("");
  const [createdByError, setCreatedByError] = useState("");

  const setApiFieldError = (fieldName, message) => {
    if (fieldName === "leaveYear") setError(message);
    if (fieldName === "createdBy") setCreatedByError(message);
  };

  useEffect(() => {
    if (!show) return;
    setLeaveYear(currentYear);
    setError("");
    setCreatedByError("");
  }, [show]);

  useEffect(() => {
    if (!show) return undefined;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const handleKeyDown = (event) => {
      if (event.key === "Escape" && !saving) onClose?.();
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [onClose, saving, show]);

  if (!show) return null;

  const initialize = () => {
    if (Number(leaveYear) <= 0) {
      setError("Select The Year.");
      return;
    }

    onInitialize?.(leaveYear, setApiFieldError);
  };

  return createPortal(
    <div
      className="tp-confirm-layer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="employee-leave-balance-initialize-title"
    >
      <div className="tp-confirm-backdrop" aria-hidden="true" />

      <div
        className="tp-confirm-card tp-confirm-card-titlebar tp-employee-leave-balance-initialize-modal"
        style={{ maxWidth: 520 }}
      >
        <div className="tp-confirm-titlebar">
          <div className="tp-confirm-titlebar-text">
            <SvgIcon name="leaveBalance" size={19} />
            <span id="employee-leave-balance-initialize-title">
              Initialize Employee Leave Balance
            </span>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={saving}
            aria-label="Close Initialize Employee Leave Balance"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>

        <div className="p-4">
          <SearchableDropdown
            name="leaveYear"
            label="Year"
            required
            value={leaveYear}
            onChange={(event) => {
              setLeaveYear(event.target.value);
              setError("");
            }}
            options={
              years.some((item) => String(item.value) === String(leaveYear))
                ? years
                : [
                    { value: String(leaveYear), label: String(leaveYear) },
                    ...years,
                  ]
            }
            columns={[{ key: "label", label: "Year" }]}
            placeholder="Search and Select Year"
            error={error}
            disabled={saving}
          />
        </div>

        <div className="tp-timesheet-details-footer tp-confirm-actions">
          <button
            type="button"
            className="tp-confirm-no"
            onClick={onClose}
            disabled={saving}
          >
            <SvgIcon name="close" size={17} /> Cancel
          </button>
          <button
            type="button"
            className="tp-confirm-yes"
            onClick={initialize}
            disabled={saving}
          >
            {saving ? (
              <>
                <span
                  className="spinner-border spinner-border-sm"
                  role="status"
                  aria-hidden="true"
                />
                <span>Please wait...</span>
              </>
            ) : (
              <>
                <SvgIcon name="approval" size={17} />
                <span>Initialize</span>
              </>
            )}
          </button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
