import { useState } from "react";
import { exportBase64File } from "../../utils/fileExportUtils";
import ToastService from "../../services/toastService";
import SvgIcon from "../common/SvgIcon";
import EmployeeTimesheetDetailsModal from "../../pages/timesheet/EmployeeTimesheetDetailsModal";
import { convertDateToSpecifiedFormat } from "../../utils/dateUtils";

const text = (value) => String(value ?? "").trim();
const monthNames = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];
const labels = {
  SubmitTimesheet: "Submit For Approval",
  ResubmitTimesheet: "Resubmit For Approval",
  ApproveTimesheet: "Approve Timesheet",
  RejectTimesheet: "Reject Timesheet",
};

// Preserve the original backend date/time when its numeric format is ambiguous.
const formatDateTime = (value) => {
  const raw = text(value);
  const match = raw.match(/^(\d{4}-\d{2}-\d{2})(?:[T ](.*))?$/);
  if (!match) return raw;
  return [
    convertDateToSpecifiedFormat(match[1], "dd-MMM-yyyy") || match[1],
    match[2],
  ]
    .filter(Boolean)
    .join(" ");
};

export default function AITimesheetResult({
  result,
  active,
  busy,
  onConfirm,
  onCancel,
}) {
  const [showDetails, setShowDetails] = useState(false);
  const details = result.details || {};
  const downloadReviewFile = () => {
    const file = result.reviewFile;
    if (!file) return;
    try {
      exportBase64File({
        base64Content: file.FileBase64,
        contentType: file.ContentType,
        fileName: file.FileName,
      });
    } catch {
      ToastService.error("Unable to download the timesheet Excel file.");
    }
  };
  const status = text(details.TimesheetApprovalStatus);
  const statusKey = status.toLowerCase();
  const tone =
    {
      approved: "text-bg-success",
      rejected: "text-bg-danger",
      "pending for approval": "text-bg-warning",
    }[statusKey] || "text-bg-secondary";
  const period = [
    monthNames[Number(details.TimesheetMonth) - 1] ||
      text(details.TimesheetMonth),
    text(details.TimesheetYear),
  ]
    .filter(Boolean)
    .join(" ");
  const decision =
    statusKey === "approved"
      ? "Approved"
      : statusKey === "rejected"
        ? "Rejected"
        : "";
  const fields = [
    ["Employee Code", details.EmployeeCode],
    ["Employee Name", details.EmployeeName],
    ["Timesheet Period", period],
    ["Total Hours Worked", details.TotalHoursWorked],
    ...(decision
      ? [
          [`${decision} By`, details.ApprovedOrRejectedByUser],
          [
            `${decision} Date and Time`,
            formatDateTime(details.ApprovedOrRejectedDateTime),
          ],
        ]
      : []),
    [
      decision
        ? `${decision === "Approved" ? "Approval" : "Rejection"} Remarks`
        : "Remarks",
      (decision && text(details.ApprovedOrRejectedReason)) || details.Remarks,
    ],
  ].filter(([, value]) => text(value));
  if (
    !fields.length &&
    !status &&
    !result.rows &&
    !result.fileName &&
    !result.actionState
  )
    return null;
  return (
    <div className="tp-ai-confirm-card tp-ai-timesheet-card">
      <strong>
        <SvgIcon name="timesheet" size={18} />{" "}
        {labels[details.Intent] && result.actionState
          ? labels[details.Intent]
          : "Timesheet Details"}
      </strong>
      {fields.map(([label, value]) => (
        <div className="tp-ai-confirm-field" key={label}>
          <span>{label}</span>
          <strong>{text(value)}</strong>
        </div>
      ))}
      {status && (
        <div className="tp-ai-confirm-field">
          <span>Timesheet Status</span>
          <strong className={`badge ${tone}`}>{status}</strong>
        </div>
      )}
      {(result.reviewFile || result.reviewFileError) && (
        <div className="tp-ai-timesheet-excel">
          {result.reviewFile ? (
            <>
              <div className="tp-ai-timesheet-excel-info">
                <span className="tp-ai-timesheet-excel-symbol">
                  <SvgIcon name="aiExcelFile" size={22} />
                </span>
                <div className="tp-ai-timesheet-excel-name">
                  <strong>Employee Timesheet</strong>
                  <span>{result.reviewFile.FileName}</span>
                  <small>{period} · Excel</small>
                </div>
              </div>
              <div className="tp-ai-report-actions">
                <button
                  type="button"
                  className="btn btn-sm tp-ai-report-excel tp-ai-report-icon-button"
                  title="Download Timesheet"
                  aria-label="Download Timesheet Excel file"
                  onClick={downloadReviewFile}
                >
                  <SvgIcon name="aiExcelFile" size={20} />
                </button>
              </div>
            </>
          ) : (
            <span role="status">{result.reviewFileError}</span>
          )}
        </div>
      )}
      {result.rows && (
        <div className="tp-ai-confirm-actions">
          <span>{result.rows.length} records</span>
          <button
            type="button"
            className="btn tp-ai-confirm-accept btn-sm"
            disabled={!result.rows.length}
            onClick={() => setShowDetails(true)}
          >
            <SvgIcon name="viewTimesheet" size={16} /> View Timesheet
          </button>
          <EmployeeTimesheetDetailsModal
            show={showDetails}
            rows={result.rows}
            onClose={() => setShowDetails(false)}
          />
        </div>
      )}
      {result.fileName && (
        <div className="tp-ai-confirm-field">
          <span>Excel file</span>
          <strong>{result.fileName}</strong>
        </div>
      )}
      {active && (
        <div className="tp-ai-confirm-actions">
          <button
            type="button"
            className="btn tp-ai-confirm-accept btn-sm"
            disabled={busy}
            onClick={onConfirm}
          >
            <SvgIcon
              name={details.Intent === "RejectTimesheet" ? "close" : "approval"}
              size={16}
            />
            {labels[details.Intent] || "Confirm"}
          </button>
          <button
            type="button"
            className="btn tp-ai-confirm-cancel btn-sm"
            disabled={busy}
            onClick={onCancel}
          >
            <SvgIcon name="close" size={16} /> Cancel
          </button>
        </div>
      )}
      {!active && result.actionState && (
        <div className="tp-ai-confirm-field" role="status">
          <strong>{result.actionState}</strong>
        </div>
      )}
    </div>
  );
}
