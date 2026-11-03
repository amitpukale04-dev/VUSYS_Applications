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
import { TIMESHEET_APPROVAL_STATUS_OPTIONS } from "../../constants/constants";
import { timesheetApproveRejectValidationRules } from "../../validation/timesheetApprovalRejectionValidationRules";

const createForm = (row) => ({
  employeeCode: row?.employeeCode || "",
  timesheetMonth: row?.timeSheetMonth || "",
  timesheetYear: String(row?.timeSheetYear || ""),
  approvalStatus: "",
  remarks: "",
});

export default function TimesheetApproveRejectModal({
  show,
  row,
  saving = false,
  onViewTimesheet,
  onSave,
  onClose,
  apiFieldErrors = {},
}) {
  const [form, setForm] = useState(() => createForm(row));
  const [dirtyOpen, setDirtyOpen] = useState(false);

  const { errors, validate, clearError, clearErrors, setErrors } =
    useFormValidation(timesheetApproveRejectValidationRules);

  useEffect(() => {
    if (!show) return;
    setForm(createForm(row));
    setDirtyOpen(false);
    clearErrors();
  }, [clearErrors, row, show]);

  useEffect(() => {
    if (!show) return;
    if (apiFieldErrors && Object.keys(apiFieldErrors).length) {
      setErrors((current) => ({ ...current, ...apiFieldErrors }));
    }
  }, [apiFieldErrors, setErrors, show]);

  const isDirty = Boolean(
    String(form.approvalStatus || "").trim() ||
    String(form.remarks || "").trim(),
  );

  const requestClose = () => {
    if (saving) return;

    if (isDirty) {
      setDirtyOpen(true);
      return;
    }

    onClose?.();
  };

  useEffect(() => {
    if (!show) return undefined;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const handleKeyDown = (event) => {
      if (event.key === "Escape" && !saving) requestClose();
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [isDirty, onClose, saving, show]);

  if (!show || !row) return null;

  const updateForm = (event) => {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
    clearError(name);
  };

  const handleSave = () => {
    if (!validate(form)) return;

    onSave?.({
      ...form,
      approveStatus: form.approvalStatus === "1" ? "Approve" : "Reject",
    });
  };

  return createPortal(
    <div
      className="tp-confirm-layer"
      role="dialog"
      aria-modal="true"
      aria-labelledby="timesheet-approve-reject-title"
    >
      <div className="tp-confirm-backdrop" aria-hidden="true" />

      <div className="tp-confirm-card tp-confirm-card-titlebar tp-timesheet-approve-reject-modal">
        <div className="tp-confirm-titlebar">
          <div className="tp-confirm-titlebar-text">
            <SvgIcon name="timesheetApproval" size={19} />
            <span id="timesheet-approve-reject-title">
              Timesheet Approve/Reject
            </span>
          </div>

          <button
            type="button"
            onClick={requestClose}
            disabled={saving}
            aria-label="Close Timesheet Approve/Reject"
          >
            <SvgIcon name="close" size={18} />
          </button>
        </div>

        <div className="tp-timesheet-approve-reject-body">
          <div className="row g-3">
            <div className="col-md-4">
              <TextBox
                name="employeeCode"
                label="Employee Code"
                required
                value={form.employeeCode}
                error={errors.employeeCode}
                readOnly
              />
            </div>

            <div className="col-md-4">
              <TextBox
                name="timesheetMonth"
                label="Timesheet Month"
                required
                value={form.timesheetMonth}
                error={errors.timesheetMonth}
                readOnly
              />
            </div>

            <div className="col-md-4">
              <TextBox
                name="timesheetYear"
                label="Timesheet Year"
                required
                value={form.timesheetYear}
                error={errors.timesheetYear}
                readOnly
              />
            </div>

            <div className="col-md-4">
              <Dropdown
                name="approvalStatus"
                label="Approval Status"
                required
                value={form.approvalStatus}
                onChange={updateForm}
                options={TIMESHEET_APPROVAL_STATUS_OPTIONS}
                error={errors.approvalStatus}
                placeholder="Select approval status"
                disabled={saving}
              />
            </div>

            <div className="col-md-8">
              <TextArea
                name="remarks"
                label="Remarks"
                required
                value={form.remarks}
                onChange={updateForm}
                error={errors.remarks}
                placeholder="Enter remarks"
                rows={4}
                disabled={saving}
              />
            </div>

            <div className="col-12">
              <button
                type="button"
                className="tp-report-link-button tp-timesheet-view-link"
                onClick={() => onViewTimesheet?.(row)}
                disabled={saving}
                title="View Timesheet"
              >
                View Timesheet
              </button>
            </div>
          </div>
        </div>

        <div className="tp-timesheet-approve-reject-footer tp-confirm-actions">
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
