import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import SvgIcon from "../../components/common/SvgIcon";
import DirtyStateModal from "../../components/common/DirtyStateModal";
import {
  Dropdown,
  TextArea,
  TextBox,
} from "../../components/forms/FormElements";
import useFormValidation from "../../hooks/useFormValidation";
import { leaveApproveRejectValidationRules } from "../../validation/leaveApprovalRejectionValidationRules";

const createForm = (row) => ({
  employeeCode: row?.employeeCode || "",
  approvalStatus: "",
  remarks: "",
});

// ============================================================
// LEAVE APPROVE / REJECT MODAL
// ============================================================

export default function LeaveApproveRejectModal({
  show,
  row,
  approvalOptions = [],
  saving = false,
  apiFieldErrors = {},
  onViewLeaveDetails,
  onSave,
  onClose,
}) {
  const [form, setForm] = useState(() => createForm(row));
  const [dirtyOpen, setDirtyOpen] = useState(false);
  const { errors, validate, clearError, clearErrors, setErrors } =
    useFormValidation(leaveApproveRejectValidationRules);

  useEffect(() => {
    if (!show) return;
    setForm(createForm(row));
    setDirtyOpen(false);
    clearErrors();
  }, [show, row, clearErrors]);

  useEffect(() => {
    if (show && Object.keys(apiFieldErrors).length) {
      setErrors((current) => ({ ...current, ...apiFieldErrors }));
    }
  }, [show, apiFieldErrors, setErrors]);

  const isDirty = Boolean(form.approvalStatus || form.remarks.trim());
  const requestClose = () => {
    if (saving) return;
    if (isDirty) setDirtyOpen(true);
    else onClose?.();
  };

  useEffect(() => {
    if (!show) return undefined;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const onKeyDown = (event) => {
      if (event.key === "Escape" && !saving) requestClose();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [show, saving, isDirty, onClose]);

  if (!show || !row) return null;

  const updateForm = (event) => {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
    clearError(name);
  };

  const handleSave = () => {
    if (!validate(form)) return;
    onSave?.(form);
  };

  return createPortal(
    <div
      className="tp-confirm-layer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="leave-approve-reject-title"
    >
      <div className="tp-confirm-backdrop" aria-hidden="true" />
      <div className="tp-confirm-card tp-confirm-card-titlebar tp-leave-approve-reject-modal">
        <div className="tp-confirm-titlebar">
          <div className="tp-confirm-titlebar-text">
            <SvgIcon name="approval" size={19} />
            <span id="leave-approve-reject-title">Leave Approve/Reject</span>
          </div>
          <button
            type="button"
            onClick={requestClose}
            disabled={saving}
            aria-label="Close Leave Approve/Reject"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>

        <div className="tp-leave-approve-reject-body">
          <div className="row g-3">
            <div className="col-sm-6">
              <TextBox
                name="employeeCode"
                label="Employee Code"
                required
                value={form.employeeCode}
                error={errors.employeeCode}
                readOnly
              />
            </div>
            <div className="col-sm-6">
              <Dropdown
                name="approvalStatus"
                label="Approval Status"
                required
                value={form.approvalStatus}
                onChange={updateForm}
                options={approvalOptions}
                error={errors.approvalStatus}
                placeholder="Select approval status"
                disabled={saving}
              />
            </div>
            <div className="col-12">
              <TextArea
                name="remarks"
                label="Remarks"
                required
                rows={4}
                value={form.remarks}
                onChange={updateForm}
                error={errors.remarks}
                placeholder="Enter remarks"
                disabled={saving}
              />
            </div>
            {errors.actionBy && (
              <div className="col-12">
                <div className="text-danger small" role="alert">
                  {errors.actionBy}
                </div>
              </div>
            )}
            <div className="col-12">
              <button
                type="button"
                className="tp-report-link-button tp-leave-approve-reject-view-link"
                onClick={() => onViewLeaveDetails?.(row)}
                disabled={saving}
              >
                View Leave Details
              </button>
            </div>
          </div>
        </div>

        <div className="tp-leave-approve-reject-footer tp-confirm-actions">
          <button
            type="button"
            className="tp-confirm-no"
            onClick={requestClose}
            disabled={saving}
          >
            <SvgIcon name="close" size={17} /> Cancel
          </button>
          <button
            type="button"
            className="tp-confirm-yes"
            onClick={handleSave}
            disabled={saving}
          >
            <SvgIcon name="save" size={17} /> Save
          </button>
        </div>
      </div>
      <DirtyStateModal
        open={dirtyOpen}
        onStay={() => setDirtyOpen(false)}
        onDiscard={() => {
          setDirtyOpen(false);
          onClose?.();
        }}
      />
    </div>,
    document.body,
  );
}
