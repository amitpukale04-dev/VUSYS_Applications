import { useEffect, useRef, useState } from "react";
import SvgIcon from "../common/SvgIcon";
import TextArea from "../forms/TextArea";
import { useAuth } from "../../context/AuthContext";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { aiAssistantModules } from "./aiAssistantModules";
import {
  processAILeaveRequest,
  confirmAILeaveAction,
  exportAILeaveReport,
} from "../../services/aiLeaveManagementService";
import "./AIAssistantWidget.css";
import AITimesheetResult from "./AITimesheetResult";
import {
  processAITimesheetRequest,
  confirmAITimesheetAction,
} from "../../services/aiTimesheetManagementService";
import ToastService from "../../services/toastService";
import { showApiError } from "../../utils/showApiError";
import { exportBase64File } from "../../utils/fileExportUtils";
import { convertDateToSpecifiedFormat } from "../../utils/dateUtils";

const initialConversation = () => ({
  messages: [],
  draft: "",
  pending: null,
  busy: false,
  selection: null,
  review: null,
  clarification: null,
  remarksStep: null,
});
const READ_ONLY_INTENTS = new Set([
  "GetLeaveBalance",
  "GetLeaveStatus",
  "GetLeaveHistory",
  "GetPendingLeaves",
  "GetLeaveDetails",
]);
const leaveStatusStyle = (status) =>
  ({
    1: "warning",
    2: "success",
    3: "danger",
    4: "secondary",
  })[Number(status)] || "secondary";
const displayNumber = (value) => {
  if (value === null || value === undefined || value === "") return "—";
  const numeric = Number(value);
  return Number.isFinite(numeric)
    ? numeric.toLocaleString(undefined, { maximumFractionDigits: 2 })
    : String(value);
};
const groupByEmployee = (rows) =>
  rows.reduce((groups, row) => {
    const key =
      readable(row.EmployeeCode) ||
      readable(row.EmployeeName) ||
      "Unknown employee";
    if (!groups[key]) groups[key] = [];
    groups[key].push(row);
    return groups;
  }, {});
const normalizeRole = (user) =>
  String(user?.role ?? user?.UserRole ?? "")
    .trim()
    .toLowerCase();
const readable = (value) => String(value ?? "").trim();
const requiresActionRemarks = (intent) =>
  ["ApproveLeave", "RejectLeave", "CancelLeave"].includes(readable(intent));
const actionName = (intent) =>
  ({ ApproveLeave: "Approve", RejectLeave: "Reject", CancelLeave: "Cancel" })[
    intent
  ] || "";
const extractRemarks = (message) => {
  const match = readable(message).match(/(?:Remarks|Reason)\s*:\s*(.+)$/i);
  return match ? match[1].trim() : "";
};

// Format leave details for display only; preserve original values for API calls.
const formatLeaveField = (field, value) => {
  if (field === "IsHalfDay") {
    const normalized = readable(value).toLowerCase();
    if (normalized === "true" || normalized === "1") return "Yes";
    if (normalized === "false" || normalized === "0") return "No";
  }

  if (field === "FromDate" || field === "ToDate") {
    return convertDateToSpecifiedFormat(value, "dd-MMM-yyyy");
  }

  if (field === "LeaveStatus") {
    const leaveStatusMap = {
      1: "Pending",
      2: "Approved",
      3: "Rejected",
      4: "Cancelled",
    };

    return (
      leaveStatusMap[Number(value)] ??
      (readable(value) === "0" ? "—" : String(value ?? ""))
    );
  }
  return String(value ?? "");
};

const formatTime = () =>
  new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
const newMessage = (type, text) => ({
  id: `${Date.now()}-${Math.random()}`,
  type,
  text,
  time: formatTime(),
});

// Persist only conversation text/drafts. Confirmation tokens and pending actions stay in memory.
const storageKey = (userKey) => `tp-ai-assistant-history:${userKey}`;
const restoreConversation = (userKey) => {
  try {
    const saved = JSON.parse(
      localStorage.getItem(storageKey(userKey)) || "null",
    );
    if (!saved || !Array.isArray(saved.messages)) return initialConversation();
    return {
      ...initialConversation(),
      messages: saved.messages
        .slice(-100)
        .filter(
          (m) =>
            ["user", "assistant", "success", "error"].includes(m?.type) &&
            typeof m?.text === "string",
        )
        .map(({ id, type, text, time }) => ({ id, type, text, time })),
      draft: typeof saved.draft === "string" ? saved.draft : "",
    };
  } catch {
    return initialConversation();
  }
};
// Reject clearly accidental/non-language input; the backend owns intent and business validation.
const isMeaningfulMessage = (message) => {
  const text = message.trim();
  if (text.length < 8 || text.length > 2000) return false;
  const words = text.match(/[\p{L}]+/gu) || [];
  return (
    words.length >= 2 &&
    words.some((word) => word.length >= 3) &&
    !/^(.)\1{5,}$/u.test(text.replace(/\s/g, ""))
  );
};

// Read-only cards never invoke the write-action selection or confirmation workflow.
function ReadOnlyLeaveResults({ result }) {
  const isBalance = result.intent === "GetLeaveBalance";
  const rows = isBalance ? result.balances : result.records;
  const groups = Object.entries(groupByEmployee(rows));
  if (!rows.length) {
    return (
      <div className="tp-ai-readonly-empty" role="status">
        No matching leave information was found.
      </div>
    );
  }
  return (
    <section className="tp-ai-readonly-results" aria-label="Leave information">
      <div className="tp-ai-readonly-heading">
        <SvgIcon name="calendar" size={17} />
        <strong>
          {
            {
              GetLeaveBalance: "Leave Balance",
              GetLeaveStatus: "Leave Status",
              GetLeaveHistory: "Leave History",
              GetPendingLeaves: "Pending Leaves",
              GetLeaveDetails: "Leave Request Details",
            }[result.intent]
          }
        </strong>
        <span>
          {rows.length} {rows.length === 1 ? "record" : "records"}
        </span>
      </div>
      {groups.map(([employee, items]) => (
        <div className="tp-ai-readonly-group" key={employee}>
          <div className="tp-ai-readonly-employee">
            <SvgIcon name="employees" size={15} />
            <span>{readable(items[0].EmployeeName) || "Employee"}</span>
            <small>{readable(items[0].EmployeeCode) || employee}</small>
          </div>
          {items.map((item, index) =>
            isBalance ? (
              <article
                className="tp-ai-readonly-card"
                key={`${item.Id ?? index}-${item.LeaveYear ?? ""}`}
              >
                <div className="tp-ai-readonly-card-head">
                  <strong>{readable(item.LeaveType) || "Leave"}</strong>
                  <span className="tp-ai-readonly-year">
                    {item.LeaveYear || "—"}
                  </span>
                </div>
                <div className="tp-ai-readonly-available">
                  <span>Available Balance</span>
                  <strong>{displayNumber(item.AvailableBalance)}</strong>
                </div>
                <div className="tp-ai-readonly-stats">
                  {[
                    ["Opening", "OpeningBalance"],
                    ["Allocated", "AllocatedLeave"],
                    ["Used", "UsedLeave"],
                    ["Pending", "PendingLeave"],
                  ].map(([label, field]) => (
                    <div key={field}>
                      <small>{label}</small>
                      <strong>{displayNumber(item[field])}</strong>
                    </div>
                  ))}
                </div>
              </article>
            ) : (
              <details
                className="tp-ai-readonly-card tp-ai-readonly-request"
                key={`${item.LeaveRequestID ?? index}-${index}`}
                open={items.length === 1}
              >
                <summary>
                  <span className="tp-ai-readonly-summary-text">
                    <strong>
                      {readable(item.LeaveType) || "Leave request"}
                    </strong>
                    <small>
                      #{readable(item.LeaveRequestID) || "—"} ·{" "}
                      {formatLeaveField("FromDate", item.FromDate) || "—"}
                      {readable(item.ToDate) && item.ToDate !== item.FromDate
                        ? ` – ${formatLeaveField("ToDate", item.ToDate)}`
                        : ""}
                    </small>
                  </span>
                  <span
                    className={`tp-ai-readonly-status tp-ai-readonly-status-${leaveStatusStyle(item.LeaveStatus)}`}
                  >
                    {formatLeaveField("LeaveStatus", item.LeaveStatus) ||
                      "Unknown"}
                  </span>
                </summary>
                <div className="tp-ai-readonly-detail-grid">
                  {[
                    ["Request ID", "LeaveRequestID"],
                    ["Employee Code", "EmployeeCode"],
                    ["From Date", "FromDate"],
                    ["To Date", "ToDate"],
                    ["Leave Days", "TotalLeaveDays"],
                    ["Half Day", "IsHalfDay"],
                    ["Half-Day Type", "HalfDayType"],
                    ["Reason", "Reason"],
                    ["Remarks", "Remarks"],
                  ]
                    .filter(
                      ([, key]) =>
                        item[key] !== null &&
                        item[key] !== undefined &&
                        String(item[key]).trim() !== "",
                    )
                    .map(([label, key]) => (
                      <div key={key}>
                        <span>{label}</span>
                        <strong>
                          {formatLeaveField(key, item[key]) || "—"}
                        </strong>
                      </div>
                    ))}
                </div>
              </details>
            ),
          )}
        </div>
      ))}
    </section>
  );
}

// Report state belongs to the individual assistant message, never to the conversation globally.
function AILeaveReport({ report, onExport, onRegenerate }) {
  const [page, setPage] = useState(1);
  const [exporting, setExporting] = useState("");
  const [error, setError] = useState("");
  const [expired, setExpired] = useState(false);
  const busyRef = useRef(false);
  const rows = report.rows;
  const columns = Array.from(
    rows.reduce((keys, row) => {
      Object.keys(row || {}).forEach((key) => keys.add(key));
      return keys;
    }, new Set()),
  );
  const pages = Math.max(1, Math.ceil(rows.length / 30));
  const currentPage = Math.min(page, pages);
  const dateField = (key) => /(?:date|^FromDate$|^ToDate$)/i.test(key);
  const formatCell = (key, value) => {
    if (value === null || value === undefined || value === "") return "—";
    if (dateField(key))
      return (
        convertDateToSpecifiedFormat(value, "dd-MMM-yyyy") || String(value)
      );
    return typeof value === "boolean" ? (value ? "Yes" : "No") : String(value);
  };
  const download = async (type) => {
    if (busyRef.current || !report.token) return;
    busyRef.current = true;
    setExporting(type);
    setError("");
    try {
      const file = await onExport(report, type);
      exportBase64File({
        base64Content: file.FileBase64,
        contentType: file.ContentType,
        fileName: file.FileName,
      });
    } catch (exception) {
      const serverMessage =
        exception?.response?.data?.data?.AI_LEAVE_MANAGEMENT_RESPONSE?.[0]
          ?.Message ||
        exception?.response?.data?.message ||
        exception?.message ||
        "";
      const tokenError =
        exception?.isReportTokenError || /token|expired/i.test(serverMessage);
      if (tokenError) setExpired(true);
      setError(
        tokenError
          ? "This report has expired. Regenerate it to download a fresh copy."
          : exception?.message || "Unable to download the report.",
      );
    } finally {
      busyRef.current = false;
      setExporting("");
    }
  };
  return (
    <section className="tp-ai-report" aria-label="AI Leave Report">
      <div className="tp-ai-report-heading">
        <SvgIcon name="calendar" size={16} />
        <strong>AI Leave Report</strong>
      </div>
      {rows.length ? (
        <div
          className="tp-ai-report-scroll"
          tabIndex={0}
          role="region"
          aria-label="Scrollable report table"
        >
          <table className="tp-ai-report-table">
            <thead>
              <tr>
                {columns.map((key) => (
                  <th key={key} scope="col">
                    {key}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows
                .slice((currentPage - 1) * 30, currentPage * 30)
                .map((row, index) => (
                  <tr key={index}>
                    {columns.map((key) => (
                      <td
                        key={key}
                        className={
                          typeof row?.[key] === "number" ||
                          (typeof row?.[key] === "string" &&
                            /^-?\d+(?:\.\d+)?$/.test(row[key]) &&
                            !/(?:code|id|year|phone|zip)/i.test(key))
                            ? "tp-ai-report-numeric"
                            : ""
                        }
                      >
                        {formatCell(key, row?.[key])}
                      </td>
                    ))}
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p className="tp-ai-report-empty">No records found for this report.</p>
      )}
      <div className="tp-ai-report-footer">
        <span>Total Records: {rows.length}</span>
        {pages > 1 && (
          <div className="tp-ai-report-pages">
            <button
              type="button"
              disabled={currentPage === 1}
              onClick={() => setPage(currentPage - 1)}
              aria-label="Previous page"
            >
              ‹
            </button>
            <span>
              Page {currentPage} of {pages}
            </span>
            <button
              type="button"
              disabled={currentPage === pages}
              onClick={() => setPage(currentPage + 1)}
              aria-label="Next page"
            >
              ›
            </button>
          </div>
        )}
      </div>
      {error && (
        <div className="tp-ai-report-error" role="alert">
          {error}
        </div>
      )}
      <div className="tp-ai-report-actions">
        {expired || !report.token ? (
          <button
            type="button"
            className="btn btn-sm tp-ai-report-regenerate"
            disabled={!!exporting}
            onClick={() => onRegenerate(report)}
          >
            Regenerate Report
          </button>
        ) : (
          <>
            <button
              type="button"
              className="btn btn-sm tp-ai-report-excel tp-ai-report-icon-button"
              disabled={!!exporting}
              title={
                exporting === "Excel" ? "Exporting Excel..." : "Download Excel"
              }
              aria-label={
                exporting === "Excel" ? "Exporting Excel" : "Download Excel"
              }
              onClick={() => void download("Excel")}
            >
              {exporting === "Excel" ? (
                <span
                  className="spinner-border spinner-border-sm"
                  aria-hidden="true"
                />
              ) : (
                <SvgIcon name="aiExcelFile" size={20} />
              )}
            </button>
            <button
              type="button"
              className="btn btn-sm tp-ai-report-pdf tp-ai-report-icon-button"
              disabled={!!exporting}
              title={exporting === "PDF" ? "Exporting PDF..." : "Download PDF"}
              aria-label={
                exporting === "PDF" ? "Exporting PDF" : "Download PDF"
              }
              onClick={() => void download("PDF")}
            >
              {exporting === "PDF" ? (
                <span
                  className="spinner-border spinner-border-sm"
                  aria-hidden="true"
                />
              ) : (
                <SvgIcon name="aiPdfFile" size={20} />
              )}
            </button>
          </>
        )}
      </div>
    </section>
  );
}

export default function AIAssistantWidget() {
  const { user, isAuthenticated, authStage } = useAuth();
  const { hasPermission, isLoading: permissionsLoading } =
    useUserSecurityPermission();
  const role = normalizeRole(user);
  const eligible =
    isAuthenticated &&
    authStage === "authenticated" &&
    user?.isAIAssistantEnabled === true &&
    (role === "admin" || role === "employee") &&
    !permissionsLoading;
  const isReportAdmin =
    Number(
      user?.roleId ??
        user?.RoleId ??
        user?.RoleID ??
        user?.roleID ??
        user?.Role?.RoleId,
    ) === 1 || role === "admin";
  const [regeneratingId, setRegeneratingId] = useState(null);
  const regenerationRef = useRef(false);
  const canApply = hasPermission("My Leave Requests", "Apply");
  const canApprove = hasPermission("Leave Approval/Rejection List", "Approve");
  const [open, setOpen] = useState(false);
  const [panelPosition, setPanelPosition] = useState(null);
  const [inputError, setInputError] = useState("");
  const [remarksDraft, setRemarksDraft] = useState("");
  const dragRef = useRef(null);
  const [moduleId, setModuleId] = useState(null);
  const [reportsSubmodule, setReportsSubmodule] = useState(null);
  const conversationKey =
    moduleId === "timesheet"
      ? "timesheet"
      : moduleId === "reports"
        ? "leaveReports"
        : "leaveManagement";
  const [conversations, setConversations] = useState({
    leaveManagement: initialConversation(),
    leaveReports: initialConversation(),
    timesheet: initialConversation(),
  });
  const current = conversations[conversationKey];
  const messagesEndRef = useRef(null);
  const requestRef = useRef(false);
  const selectionRef = useRef(null);
  const sessionRef = useRef(null);
  const epochRef = useRef(0);
  const userKey = eligible ? String(user?.userId ?? user?.id ?? "") : "";
  const sessionKey = eligible && userKey ? userKey : null;

  useEffect(() => {
    if (sessionRef.current !== sessionKey) {
      sessionRef.current = sessionKey;
      epochRef.current += 1;
      requestRef.current = false;
      selectionRef.current = null;
      setOpen(false);
      setModuleId(null);
      setReportsSubmodule(null);
      setConversations({
        leaveManagement: sessionKey
          ? restoreConversation(sessionKey)
          : initialConversation(),
        leaveReports: initialConversation(),
        timesheet: initialConversation(),
      });
      setInputError("");
      setPanelPosition(null);
    }
  }, [sessionKey]);

  useEffect(() => {
    if (!sessionKey || sessionRef.current !== sessionKey) return;
    try {
      localStorage.setItem(
        storageKey(sessionKey),
        JSON.stringify({
          messages: conversations.leaveManagement.messages
            .slice(-100)
            .map(({ id, type, text, time }) => ({ id, type, text, time })),
          draft: conversations.leaveManagement.draft,
        }),
      );
    } catch {
      /* Storage unavailable or quota exceeded. */
    }
  }, [sessionKey, conversations]);

  useEffect(() => {
    if (open && moduleId)
      messagesEndRef.current?.scrollIntoView({
        behavior: "smooth",
        block: "nearest",
      });
  }, [current.messages, current.pending, moduleId, open]);

  if (!eligible || !userKey) return null;

  const update = (change) =>
    setConversations((previous) => ({
      ...previous,
      [conversationKey]:
        typeof change === "function"
          ? change(previous[conversationKey])
          : { ...previous[conversationKey], ...change },
    }));
  const append = (type, text) =>
    update((previous) => ({
      ...previous,
      messages: [...previous.messages, newMessage(type, text)],
    }));
  const permittedSuggestions = [
    ...(canApply
      ? [
          "I am not feeling well and need sick leave tomorrow.",
          "Apply half-day leave tomorrow for personal work.",
          "I worked on Sunday and want comp off tomorrow.",
        ]
      : []),

    "Show my leave balance.",
    "Show my leave history for 2026.",
    "What is the status of my leave request?",
    "Show my pending leaves.",
    "Show my leave request details.",

    // Cancel Leave - No separate permission required
    "Cancel my leave request (enter request ID) because my plans changed.",

    // Approve and Reject Leave - Same Approve permission
    ...(role === "admin" && canApprove
      ? [
          "Approve leave request (enter request ID). Remarks: Approved as requested.",
          "Reject leave request (enter request ID) because of an upcoming project deadline.",
        ]
      : []),
  ];
  const welcome = `Hello! I'm your TimePro Leave Management Assistant. I can help you ${
    [
      canApply && "apply for leave",
      "cancel your leave requests",
      role === "admin" && canApprove && "approve employee leave requests",
      role === "admin" && canApprove && "reject employee leave requests",
      "check leave balances, status, history, pending requests, and details",
    ]
      .filter(Boolean)
      .join(", ") || "with leave-related questions"
  }. How can I help you today?`;

  const reportSuggestions = [
    "Generate employee-wise leave balance report for 2026",
    "Show distinct employees who have leave balances",
    "Generate leave applications report for 2026",
    "Generate holiday report for 2026",
  ];
  const timesheetSuggestions = [
    "Submit my timesheet.",
    "Resubmit my rejected timesheet.",
    "Check my timesheet status.",
    "Show my approval details.",
    "Show my rejection remarks.",
    ...([true, 1, "1", "true"].includes(user?.isApprover ?? user?.IsApprover)
      ? [
          "View employee timesheet.",
          "Download employee timesheet.",
          "Approve employee timesheet.",
          "Reject employee timesheet.",
          "Check employee timesheet status.",
        ]
      : []),
  ];

  // Timesheet tokens and result cards remain in the independent in-memory conversation.
  const finishTimesheetCard = (previous, id, actionState, approvalStatus) => ({
    ...previous,
    pending: null,
    messages: previous.messages.map((item) =>
      item.id === id
        ? {
            ...item,
            timesheetResult: {
              ...item.timesheetResult,
              actionState,
              details: {
                ...item.timesheetResult.details,
                ...(approvalStatus
                  ? { TimesheetApprovalStatus: approvalStatus }
                  : {}),
              },
            },
          }
        : item,
    ),
  });
  const sendTimesheet = async (message, fromRemarks = false) => {
    if (current.clarification?.requiresRemarks && !fromRemarks) return;
    if (!message || requestRef.current) return;
    if (message.length > 2000) {
      setInputError("Enter a maximum of 2000 characters.");
      return;
    }
    const token = current.clarification?.token || "";
    const epoch = epochRef.current;
    requestRef.current = true;
    setInputError("");
    update((previous) => {
      const next = previous.pending
        ? finishTimesheetCard(previous, previous.pending.id, "Cancelled")
        : previous;
      return {
        ...next,
        pending: null,
        clarification: null,
        busy: true,
        draft: "",
        messages: [...next.messages, newMessage("user", message)],
      };
    });
    try {
      const result = await processAITimesheetRequest(message, token);
      if (epoch !== epochRef.current) return;
      const details = result.details;
      const confirmation =
        result.status === "confirmationrequired" &&
        readable(details.ConfirmationToken) &&
        [
          "SubmitTimesheet",
          "ResubmitTimesheet",
          "ApproveTimesheet",
          "RejectTimesheet",
        ].includes(details.Intent);
      const clarification =
        result.status === "clarificationrequired" &&
        readable(details.ClarificationToken);
      const invalidStep =
        (result.status === "confirmationrequired" && !confirmation) ||
        (result.status === "clarificationrequired" && !clarification);
      const failed = invalidStep || result.status === "error";
      if (result.file) {
        exportBase64File({
          base64Content: result.file.FileBase64,
          contentType: result.file.ContentType,
          fileName: result.file.FileName,
        });
      }
      const item = newMessage(
        failed ? "error" : "assistant",
        invalidStep
          ? "The timesheet response is incomplete. Please repeat your request."
          : result.message ||
              (result.rows
                ? result.rows.length
                  ? "Timesheet details retrieved."
                  : "No timesheet records found."
                : result.file
                  ? "Timesheet download started."
                  : "Timesheet request completed."),
      );
      const { ConfirmationToken, ClarificationToken, ...safeDetails } = details;
      item.timesheetResult = {
        details: safeDetails,
        rows: result.rows,
        fileName: result.file?.FileName,
        reviewFile: result.reviewFile,
        reviewFileError: result.reviewFileError,
        actionState: confirmation ? "Awaiting confirmation" : "",
      };
      update((previous) => ({
        ...previous,
        messages: [...previous.messages, item],
        pending: confirmation
          ? { id: item.id, token: ConfirmationToken, Intent: details.Intent }
          : null,
        clarification: clarification
          ? {
              token: ClarificationToken,
              requiresRemarks:
                ["ApproveTimesheet", "RejectTimesheet"].includes(
                  details.Intent,
                ) &&
                !readable(details.Remarks) &&
                /\bremarks\b/i.test(result.message || ""),
              intent: details.Intent,
              remarksDraft: "",
            }
          : null,
      }));
    } catch (error) {
      if (epoch === epochRef.current) {
        const message =
          error?.response?.data?.data?.AI_TIMESHEET_MANAGEMENT_RESPONSE?.[0]
            ?.Message ||
          error?.message ||
          "Unable to process the timesheet request.";
        append("error", message);
        showApiError(error, message);
      }
    } finally {
      if (epoch === epochRef.current) {
        requestRef.current = false;
        update({ busy: false });
      }
    }
  };
  const confirmTimesheet = async () => {
    if (!current.pending?.token || requestRef.current) return;
    const pending = current.pending;
    const epoch = epochRef.current;
    requestRef.current = true;
    update((previous) => ({
      ...finishTimesheetCard(previous, pending.id, "Processing"),
      busy: true,
    }));
    try {
      const result = await confirmAITimesheetAction(pending.token);
      if (epoch !== epochRef.current) return;
      const success = result.status === "success";
      const status = {
        SubmitTimesheet: "Pending For Approval",
        ResubmitTimesheet: "Pending For Approval",
        ApproveTimesheet: "Approved",
        RejectTimesheet: "Rejected",
      }[pending.Intent];
      const message =
        result.message ||
        (success
          ? "Timesheet action completed."
          : "Confirmation could not be completed. Please repeat your request.");
      update((previous) => {
        const next = finishTimesheetCard(
          previous,
          pending.id,
          success ? "Completed" : "Not completed",
          success ? status : null,
        );
        return {
          ...next,
          messages: [
            ...next.messages,
            newMessage(success ? "success" : "error", message),
          ],
        };
      });
      if (success) ToastService.success(message);
    } catch (error) {
      if (epoch === epochRef.current) {
        update((previous) =>
          finishTimesheetCard(
            previous,
            pending.id,
            "Confirmation outcome unknown. Check timesheet status before repeating.",
          ),
        );
        append(
          "error",
          error?.response?.data?.data?.AI_TIMESHEET_MANAGEMENT_RESPONSE?.[0]
            ?.Message ||
            "Unable to confirm the timesheet action. Check its status before repeating.",
        );
        showApiError(error, "Unable to confirm the timesheet action.");
      }
    } finally {
      if (epoch === epochRef.current) {
        requestRef.current = false;
        update({ busy: false });
      }
    }
  };
  const cancelTimesheet = () => {
    if (requestRef.current) return;
    update((previous) =>
      previous.pending
        ? finishTimesheetCard(previous, previous.pending.id, "Cancelled")
        : { ...previous, clarification: null },
    );
    append(
      "assistant",
      "The pending request was cancelled. No changes were submitted.",
    );
  };
  const send = async (overrideMessage = null) => {
    const message = readable(
      typeof overrideMessage === "string" ? overrideMessage : current.draft,
    );
    if (moduleId === "timesheet") return sendTimesheet(message);
    const clarificationToken = readable(current.clarification?.token);
    if (
      !message ||
      requestRef.current ||
      current.pending ||
      current.remarksStep
    )
      return;
    if (!clarificationToken && !isMeaningfulMessage(message)) {
      setInputError(
        "Please enter a meaningful leave-related request (8–2000 characters). For example: Apply leave tomorrow for personal work.",
      );
      return;
    }
    setInputError("");
    requestRef.current = true;
    selectionRef.current = null;
    const epoch = epochRef.current;
    update((previous) => ({
      ...previous,
      selection: null,
      pending: null,
      review: null,
      remarksStep: null,
      clarification: null,
      draft: "",
      busy: true,
      messages: [...previous.messages, newMessage("user", message)],
    }));
    try {
      const details = await processAILeaveRequest(
        message,
        clarificationToken,
        !clarificationToken && isReportAdmin
          ? { action: "View", reportToken: "" }
          : null,
      );
      if (epoch !== epochRef.current) return;
      if (
        moduleId === "reports" &&
        !details.report &&
        !details.ClarificationToken
      ) {
        throw new Error(
          "Please enter a leave reporting request. Other leave actions are available under Leave Management.",
        );
      }
      const token = readable(details.ConfirmationToken);
      const nextClarificationToken = readable(details.ClarificationToken);
      const isReadOnly =
        details.readOnly === true ||
        READ_ONLY_INTENTS.has(readable(details.Intent));
      const selection =
        !isReadOnly && details.requiresSelection
          ? {
              originalMessage: message,
              intent: details.Intent,
              matches: details.matches,
              selectedId: null,
            }
          : null;
      selectionRef.current = selection;
      const reportResult =
        details.report === true && isReportAdmin
          ? {
              originalMessage: message,
              token: readable(details.ReportToken),
              rows: Array.isArray(details.reportRows) ? details.reportRows : [],
            }
          : null;
      const readOnlyResult =
        isReadOnly && !nextClarificationToken
          ? {
              id: `${Date.now()}-${Math.random()}`,
              intent: details.Intent,
              records: details.records || [],
              balances: details.balances || [],
            }
          : null;
      // The backend can return review details before generating a confirmation
      // token. Display these details without enabling Approve/Reject.
      const review =
        !token &&
        !selection &&
        ["ApproveLeave", "RejectLeave"].includes(readable(details.Intent)) &&
        [
          "LeaveRequestID",
          "EmployeeName",
          "EmployeeCode",
          "FromDate",
          "ToDate",
          "LeaveType",
          "Reason",
          "Remarks",
        ].some((field) => readable(details[field]))
          ? details
          : null;
      const remarksStep =
        !selection && requiresActionRemarks(details.Intent)
          ? {
              intent: details.Intent,
              requestId: readable(details.LeaveRequestID),
              originalMessage: message,
              details,
            }
          : null;
      if (remarksStep)
        setRemarksDraft(readable(details.Remarks) || extractRemarks(message));
      update((previous) => ({
        ...previous,
        busy: false,
        remarksStep,
        messages: [
          ...previous.messages,
          {
            ...newMessage(
              "assistant",
              details.Message ||
                (token
                  ? "Review and confirm this action."
                  : "Request processed."),
            ),
            ...(readOnlyResult ? { readOnlyResult } : {}),
            ...(reportResult ? { reportResult } : {}),
          },
        ],
        pending: token && !remarksStep ? { ...details, token } : null,
        selection,
        review: remarksStep ? null : review,
        clarification:
          !token && !selection && nextClarificationToken
            ? {
                token: nextClarificationToken,
                intent: details.Intent,
                kind: /leave request id or a leave year|request id.*leave year/i.test(
                  readable(details.Message),
                )
                  ? "requestOrYear"
                  : "halfDay",
              }
            : null,
      }));
    } catch (error) {
      if (epoch === epochRef.current) {
        if (clarificationToken) {
          update({ clarification: current.clarification });
        }
        append("error", error?.message || "Unable to process the request.");
      }
    } finally {
      if (epoch === epochRef.current) {
        requestRef.current = false;
        update({ busy: false });
      }
    }
  };

  const exportReport = (report, type) =>
    exportAILeaveReport(report.originalMessage, report.token, type);

  const regenerateReport = async (report, messageId) => {
    if (regenerationRef.current || requestRef.current) return;
    regenerationRef.current = true;
    setRegeneratingId(messageId);
    const epoch = epochRef.current;
    try {
      const details = await processAILeaveRequest(report.originalMessage, "", {
        action: "View",
        reportToken: "",
      });
      if (epoch !== epochRef.current) return;
      if (!details.report || !readable(details.ReportToken))
        throw new Error("The report could not be regenerated.");
      update((previous) => ({
        ...previous,
        messages: previous.messages.map((item) =>
          item.id === messageId
            ? {
                ...item,
                text: details.Message || item.text,
                reportResult: {
                  originalMessage: report.originalMessage,
                  token: readable(details.ReportToken),
                  rows: details.reportRows || [],
                },
              }
            : item,
        ),
      }));
    } catch (error) {
      if (epoch === epochRef.current)
        append("error", error?.message || "Unable to regenerate the report.");
    } finally {
      regenerationRef.current = false;
      if (epoch === epochRef.current) setRegeneratingId(null);
    }
  };

  // Selection is local until the user provides remarks; no action is submitted.
  const selectLeaveRequest = (leaveRequestId) => {
    const selection = selectionRef.current;
    if (!selection || requestRef.current || !readable(leaveRequestId)) return;
    const item = selection.matches.find(
      (row) => String(row.LeaveRequestID) === String(leaveRequestId),
    );
    if (!item || !requiresActionRemarks(selection.intent)) return;
    selectionRef.current = null;
    setRemarksDraft(extractRemarks(selection.originalMessage));
    setInputError("");
    update({
      selection: null,
      remarksStep: {
        intent: selection.intent,
        requestId: String(leaveRequestId),
        originalMessage: selection.originalMessage,
        details: item,
      },
    });
  };

  const submitRemarks = async () => {
    const step = current.remarksStep;
    const remarks = readable(remarksDraft);
    if (!step || requestRef.current) return;
    if (!remarks) {
      setInputError("Please enter remarks before continuing.");
      return;
    }
    if (remarks.length > 1000) {
      setInputError("Remarks cannot exceed 1000 characters.");
      return;
    }
    setInputError("");
    const action = actionName(step.intent);
    const idPart = step.requestId
      ? `leave request ID ${step.requestId}`
      : "the leave request";
    // The backend owns intent validation and issues a NEW confirmation token.
    // Never confirm a token issued before these remarks were supplied.
    const followUp = step.requestId
      ? `${action} ${idPart}. ${step.intent === "CancelLeave" ? "Reason" : "Remarks"}: ${remarks}`
      : `${step.originalMessage.replace(/(?:Remarks|Reason)\s*:\s*.+$/i, "").trim()} ${step.intent === "CancelLeave" ? "Reason" : "Remarks"}: ${remarks}`;
    requestRef.current = true;
    const epoch = epochRef.current;
    update({ busy: true });
    try {
      const details = await processAILeaveRequest(followUp);
      if (epoch !== epochRef.current) return;
      const token = readable(details.ConfirmationToken);
      if (
        details.requiresSelection ||
        !token ||
        (step.requestId && readable(details.LeaveRequestID) !== step.requestId)
      ) {
        throw new Error(
          details.Message ||
            "Unable to obtain confirmation for this leave request.",
        );
      }
      update((previous) => ({
        ...previous,
        busy: false,
        remarksStep: null,
        pending: { ...details, token },
        review: null,
        messages: [
          ...previous.messages,
          newMessage("user", `Remarks: ${remarks}`),
          newMessage(
            "assistant",
            details.Message || "Review and confirm this action.",
          ),
        ],
      }));
      setRemarksDraft("");
    } catch (error) {
      if (epoch === epochRef.current)
        append(
          "error",
          error?.message || "Unable to continue. Please review your remarks.",
        );
    } finally {
      if (epoch === epochRef.current) {
        requestRef.current = false;
        update({ busy: false });
      }
    }
  };

  const cancelRemarks = () => {
    if (requestRef.current) return;
    setRemarksDraft("");
    setInputError("");
    update({ remarksStep: null, review: null });
    append("assistant", "Leave action cancelled. No changes were submitted.");
  };

  const cancelSelection = () => {
    if (requestRef.current) return;
    selectionRef.current = null;
    update({ selection: null });
    append(
      "assistant",
      "Leave request selection cancelled. No action was submitted.",
    );
  };

  const confirm = async () => {
    if (!current.pending?.token || requestRef.current) return;
    const token = current.pending.token;
    requestRef.current = true;
    const epoch = epochRef.current;
    update({ busy: true, pending: null }); // A token is single-use, even after ambiguous failures.
    try {
      const result = await confirmAILeaveAction(token);
      if (epoch === epochRef.current)
        append("success", result.Message || "Leave action completed.");
    } catch (error) {
      if (epoch === epochRef.current)
        append(
          "error",
          `${error?.message || "Confirmation could not be completed."} Please start a new request if necessary.`,
        );
    } finally {
      if (epoch === epochRef.current) {
        requestRef.current = false;
        update({ busy: false });
      }
    }
  };

  const cancel = () => {
    if (requestRef.current) return;
    update({ pending: null });
    append(
      "assistant",
      "The pending action was cancelled. No changes were submitted.",
    );
  };
  const openModule = (id) => {
    if (requestRef.current || regenerationRef.current) return;
    if (id === "reports" && !isReportAdmin) return;
    setModuleId(id);
    setReportsSubmodule(null);
    setInputError("");
  };
  const goBackModule = () => {
    if (requestRef.current || regenerationRef.current) return;
    if (moduleId === "reports" && reportsSubmodule) {
      setReportsSubmodule(null);
    } else {
      setModuleId(null);
      setReportsSubmodule(null);
    }
    setInputError("");
  };
  const resetConversation = () => {
    if (requestRef.current) return;
    epochRef.current += 1;
    selectionRef.current = null;
    update(initialConversation());
    setRemarksDraft("");
    setInputError("");
  };
  const confirmationFields = [
    ["Intent", "Intent"],
    ["Employee Code", "EmployeeCode"],
    ["Employee Name", "EmployeeName"],
    ["Leave Type", "LeaveType"],
    ["From Date", "FromDate"],
    ["To Date", "ToDate"],
    ["Leave Request ID", "LeaveRequestID"],
    ["Reason", "Reason"],
    ["Remarks", "Remarks"],
    ["Half Day", "IsHalfDay"],
    ["Half-Day Type", "HalfDayType"],
    ["Number of Days", "TotalNoofLeaveDays"],
  ];
  const confirmLabels = {
    ApplyLeave: "Confirm Apply Leave",
    ApproveLeave: "Confirm Approval",
    RejectLeave: "Confirm Rejection",
    CancelLeave: "Confirm Cancellation",
  };

  const beginDrag = (event) => {
    if (
      window.matchMedia("(max-width: 767.98px)").matches ||
      event.button !== 0 ||
      event.target.closest("button")
    )
      return;
    const panel = event.currentTarget.closest(".tp-ai-panel");
    const bounds = panel.getBoundingClientRect();
    dragRef.current = {
      x: event.clientX - bounds.left,
      y: event.clientY - bounds.top,
    };
    event.currentTarget.setPointerCapture(event.pointerId);
  };
  const moveDrag = (event) => {
    if (window.matchMedia("(max-width: 767.98px)").matches || !dragRef.current)
      return;
    const panel = event.currentTarget.closest(".tp-ai-panel");
    const width = panel.offsetWidth;
    const height = panel.offsetHeight;
    setPanelPosition({
      left: Math.max(
        0,
        Math.min(window.innerWidth - width, event.clientX - dragRef.current.x),
      ),
      top: Math.max(
        0,
        Math.min(
          window.innerHeight - height,
          event.clientY - dragRef.current.y,
        ),
      ),
    });
  };
  const stopDrag = () => {
    dragRef.current = null;
  };

  return (
    <div className="tp-ai-assistant-root">
      {open && (
        <section
          className="tp-ai-panel"
          aria-label="TimePro AI Assistant"
          style={
            panelPosition
              ? {
                  position: "fixed",
                  left: panelPosition.left,
                  top: panelPosition.top,
                  right: "auto",
                  bottom: "auto",
                }
              : undefined
          }
        >
          <header
            className="tp-ai-header"
            onPointerDown={beginDrag}
            onPointerMove={moveDrag}
            onPointerUp={stopDrag}
            onPointerCancel={stopDrag}
            title="Drag to move AI Assistant"
          >
            <div>
              <strong>
                {moduleId === "reports"
                  ? reportsSubmodule
                    ? "AI Assistant — Leave Reports"
                    : "AI Assistant — Reports"
                  : moduleId === "timesheet"
                    ? "AI Assistant — Timesheet Management"
                    : moduleId
                      ? "AI Assistant — Leave Management"
                      : "AI Assistant"}
              </strong>
              <small>Your smart TimePro assistant</small>
            </div>
            <div className="tp-ai-header-actions">
              {moduleId && (
                <button
                  type="button"
                  title="Back to Modules"
                  aria-label="Back to Modules"
                  onClick={goBackModule}
                >
                  <SvgIcon name="arrowLeft" size={18} />
                </button>
              )}
              {moduleId && (moduleId !== "reports" || reportsSubmodule) && (
                <button
                  type="button"
                  title="New Conversation"
                  aria-label="New Conversation"
                  disabled={current.busy}
                  onClick={resetConversation}
                >
                  <SvgIcon name="add" size={18} />
                </button>
              )}
              <button
                type="button"
                title="Minimize"
                aria-label="Minimize AI Assistant"
                onClick={() => setOpen(false)}
              >
                <SvgIcon name="minus" size={18} />
              </button>
            </div>
          </header>
          {!moduleId ? (
            <div className="tp-ai-module-list">
              <p>
                Hello! I'm your TimePro AI Assistant. I can help you manage
                different activities in TimePro using natural-language
                conversations. Please select a module to get started.
              </p>
              {aiAssistantModules
                .filter((module) => !module.adminOnly || isReportAdmin)
                .map((module) => (
                  <button
                    type="button"
                    key={module.id}
                    className="tp-ai-module-card"
                    disabled={!module.enabled}
                    onClick={() => openModule(module.id)}
                  >
                    <SvgIcon name={module.icon} size={24} />
                    <span>
                      <strong>{module.name}</strong>
                      <small>{module.description}</small>
                    </span>
                    <span
                      className={`tp-ai-module-status ${module.enabled ? "tp-ai-module-status-available" : "tp-ai-module-status-soon"}`}
                    >
                      <SvgIcon
                        name={module.enabled ? "aiAvailable" : "aiComingSoon"}
                        size={15}
                      />
                      <span>{module.status}</span>
                    </span>
                  </button>
                ))}
            </div>
          ) : moduleId === "reports" && !reportsSubmodule ? (
            <div className="tp-ai-module-list tp-ai-report-submodules">
              <p>Select a reporting module to generate and export reports.</p>
              <button
                type="button"
                className="tp-ai-module-card"
                onClick={() => setReportsSubmodule("leaveReports")}
              >
                <SvgIcon name="aiLeaveReports" size={24} />
                <span>
                  <strong>Leave Reports</strong>
                  <small>Generate and export leave reports using AI.</small>
                </span>
                <span className="tp-ai-module-status tp-ai-module-status-available">
                  <SvgIcon name="aiAvailable" size={15} />
                  <span>Available</span>
                </span>
              </button>
              <button type="button" className="tp-ai-module-card" disabled>
                <SvgIcon name="aiTimesheetReports" size={24} />
                <span>
                  <strong>Timesheet Reports</strong>
                  <small>AI timesheet reporting will be available later.</small>
                </span>
                <span className="tp-ai-module-status tp-ai-module-status-soon">
                  <SvgIcon name="aiComingSoon" size={15} />
                  <span>Coming Soon</span>
                </span>
              </button>
            </div>
          ) : (
            <>
              <div
                className="tp-ai-messages"
                role="log"
                aria-live="polite"
                aria-relevant="additions text"
              >
                <div className="tp-ai-bubble assistant">
                  {moduleId === "reports"
                    ? "Hello! I can generate and export leave reports. Describe the report you need."
                    : moduleId === "timesheet"
                      ? "How can I help you with your timesheet today?"
                      : welcome}
                </div>
                {current.messages.map((message) => (
                  <div key={message.id} className="tp-ai-message-entry">
                    <div className={`tp-ai-bubble ${message.type}`}>
                      <span>{message.text}</span>
                      <small>{message.time}</small>
                    </div>
                    {message.timesheetResult && (
                      <AITimesheetResult
                        result={message.timesheetResult}
                        active={current.pending?.id === message.id}
                        busy={current.busy}
                        onConfirm={confirmTimesheet}
                        onCancel={cancelTimesheet}
                      />
                    )}
                    {message.readOnlyResult && (
                      <ReadOnlyLeaveResults result={message.readOnlyResult} />
                    )}
                    {message.reportResult && isReportAdmin && (
                      <AILeaveReport
                        key={`${message.id}-${message.reportResult.token}`}
                        report={message.reportResult}
                        onExport={exportReport}
                        onRegenerate={(report) =>
                          void regenerateReport(report, message.id)
                        }
                      />
                    )}
                    {regeneratingId === message.id && (
                      <div className="tp-ai-report-loading" role="status">
                        Regenerating report...
                      </div>
                    )}
                  </div>
                ))}
                {moduleId === "timesheet" &&
                  current.clarification &&
                  !current.clarification.requiresRemarks &&
                  !current.busy && (
                    <button
                      type="button"
                      className="btn tp-ai-confirm-cancel btn-sm"
                      onClick={cancelTimesheet}
                    >
                      Cancel Clarification
                    </button>
                  )}
                {moduleId !== "timesheet" &&
                  current.clarification &&
                  !current.busy && (
                    <div
                      className="tp-ai-clarification-card"
                      role="group"
                      aria-label={
                        current.clarification.kind === "requestOrYear"
                          ? "Clarify leave request ID or year"
                          : "Select half-day leave type"
                      }
                    >
                      {current.clarification.kind === "requestOrYear" ? (
                        <>
                          <button
                            type="button"
                            className="btn tp-ai-clarification-option tp-ai-clarification-morning btn-sm"
                            onClick={() => void send("Request ID")}
                          >
                            <SvgIcon name="calendar" size={19} /> Leave Request
                            ID
                          </button>
                          <button
                            type="button"
                            className="btn tp-ai-clarification-option tp-ai-clarification-afternoon btn-sm"
                            onClick={() => void send("Leave Year")}
                          >
                            <SvgIcon name="calendar" size={19} /> Leave Year
                          </button>
                        </>
                      ) : (
                        <>
                          <button
                            type="button"
                            className="btn tp-ai-clarification-option tp-ai-clarification-morning btn-sm"
                            disabled={current.busy}
                            onClick={() => void send("First Half")}
                          >
                            <SvgIcon name="morningHalf" size={21} />
                            <span>
                              First Half <small>(Morning)</small>
                            </span>
                          </button>
                          <button
                            type="button"
                            className="btn tp-ai-clarification-option tp-ai-clarification-afternoon btn-sm"
                            disabled={current.busy}
                            onClick={() => void send("Second Half")}
                          >
                            <SvgIcon name="afternoonHalf" size={21} />
                            <span>
                              Second Half <small>(Afternoon)</small>
                            </span>
                          </button>
                        </>
                      )}
                      <button
                        type="button"
                        className="btn btn-sm tp-ai-clarification-back"
                        onClick={() => update({ clarification: null })}
                      >
                        Cancel clarification
                      </button>
                    </div>
                  )}
                {current.selection && (
                  <div className="tp-ai-selection-card">
                    <div className="tp-ai-selection-heading">
                      <span
                        className="tp-ai-selection-heading-icon"
                        aria-hidden="true"
                      >
                        <SvgIcon name="approval" size={18} />
                      </span>
                      <div>
                        <strong>Select a leave request</strong>
                        <p>
                          {current.selection.matches.length} matching requests
                          found. Choose the correct one to continue.
                        </p>
                      </div>
                    </div>
                    <div className="tp-ai-selection-list">
                      {current.selection.matches.map((item, index) => (
                        <article
                          className="tp-ai-selection-item"
                          key={`${item.LeaveRequestID}-${index}`}
                        >
                          <div className="tp-ai-selection-item-top">
                            <span className="tp-ai-selection-number">
                              Request {index + 1}
                            </span>
                            <span className="tp-ai-selection-id">
                              ID: {readable(item.LeaveRequestID) || "—"}
                            </span>
                          </div>
                          <div className="tp-ai-selection-person">
                            {readable(item.EmployeeName) ||
                              readable(item.EmployeeCode) ||
                              "Leave request"}
                          </div>
                          <div className="tp-ai-selection-type">
                            {readable(item.LeaveType) || "Leave"}
                          </div>
                          <div className="tp-ai-selection-dates">
                            <SvgIcon name="calendar" size={15} />
                            <span>
                              {formatLeaveField("FromDate", item.FromDate) ||
                                "—"}
                              {readable(item.ToDate) &&
                              readable(item.ToDate) !== readable(item.FromDate)
                                ? ` – ${formatLeaveField("ToDate", item.ToDate)}`
                                : ""}
                            </span>
                          </div>
                          <details className="tp-ai-selection-details">
                            <summary>View request details</summary>
                            <div className="tp-ai-selection-detail-fields">
                              {[
                                ["Employee Code", "EmployeeCode"],
                                ["Total Leave Days", "TotalLeaveDays"],
                                ["Half-Day Type", "HalfDayType"],
                                ["Leave Status", "LeaveStatus"],
                                ["Reason", "Reason"],
                              ]
                                .filter(([, key]) => readable(item[key]))
                                .map(([label, key]) => (
                                  <div
                                    className="tp-ai-confirm-field"
                                    key={key}
                                  >
                                    <span>{label}</span>
                                    <strong>
                                      {formatLeaveField(key, item[key])}
                                    </strong>
                                  </div>
                                ))}
                            </div>
                          </details>
                          <button
                            type="button"
                            className="btn tp-ai-confirm-accept btn-sm tp-ai-selection-select"
                            title={`Select leave request ${item.LeaveRequestID}`}
                            disabled={
                              current.busy || !readable(item.LeaveRequestID)
                            }
                            onClick={() =>
                              void selectLeaveRequest(item.LeaveRequestID)
                            }
                          >
                            <SvgIcon name="approval" size={16} /> Select Request
                          </button>
                        </article>
                      ))}
                    </div>
                    <button
                      type="button"
                      className="btn tp-ai-confirm-cancel btn-sm tp-ai-selection-cancel"
                      disabled={current.busy}
                      onClick={cancelSelection}
                      title="Cancel selection"
                    >
                      <SvgIcon name="close" size={16} /> Cancel Selection
                    </button>
                  </div>
                )}
                {moduleId === "timesheet" &&
                  current.clarification?.requiresRemarks && (
                    <div className="tp-ai-confirm-card tp-ai-remarks-card">
                      <div className="tp-ai-remarks-title">
                        <SvgIcon name="approval" size={18} />
                        <strong>Provide Remarks</strong>
                      </div>
                      <p>
                        {current.clarification.intent === "RejectTimesheet"
                          ? "Reject timesheet"
                          : "Approve timesheet"}
                        . Enter remarks before reviewing the final confirmation.
                      </p>
                      <label
                        htmlFor="tp-ai-timesheet-remarks"
                        className="tp-ai-remarks-label"
                      >
                        Remarks <span aria-hidden="true">*</span>
                      </label>
                      <textarea
                        id="tp-ai-timesheet-remarks"
                        className="form-control tp-ai-remarks-input"
                        rows={3}
                        maxLength={2000}
                        value={current.clarification.remarksDraft || ""}
                        disabled={current.busy}
                        placeholder="Enter your remarks..."
                        onChange={(event) => {
                          const remarksDraft = event.target.value;
                          update((previous) => ({
                            ...previous,
                            clarification: {
                              ...previous.clarification,
                              remarksDraft,
                            },
                          }));
                          if (inputError) setInputError("");
                        }}
                      />
                      <div className="tp-ai-confirm-actions">
                        <button
                          type="button"
                          className="btn tp-ai-confirm-accept btn-sm"
                          disabled={
                            current.busy ||
                            !readable(current.clarification.remarksDraft)
                          }
                          onClick={() =>
                            void sendTimesheet(
                              readable(current.clarification.remarksDraft),
                              true,
                            )
                          }
                        >
                          <SvgIcon name="approval" size={16} /> Continue to
                          Confirmation
                        </button>
                        <button
                          type="button"
                          className="btn tp-ai-confirm-cancel btn-sm"
                          disabled={current.busy}
                          onClick={cancelTimesheet}
                        >
                          <SvgIcon name="close" size={16} /> Cancel
                        </button>
                      </div>
                    </div>
                  )}
                {current.remarksStep && (
                  <div className="tp-ai-confirm-card tp-ai-remarks-card">
                    <div className="tp-ai-remarks-title">
                      <SvgIcon name="approval" size={18} />
                      <strong>Provide Remarks</strong>
                    </div>
                    <p>
                      {actionName(current.remarksStep.intent)} leave request
                      {current.remarksStep.requestId
                        ? ` #${current.remarksStep.requestId}`
                        : ""}
                      . Enter remarks before reviewing the final confirmation.
                    </p>
                    <label
                      htmlFor="tp-ai-action-remarks"
                      className="tp-ai-remarks-label"
                    >
                      Remarks <span aria-hidden="true">*</span>
                    </label>
                    <textarea
                      id="tp-ai-action-remarks"
                      className={`form-control tp-ai-remarks-input ${inputError ? "is-invalid" : ""}`}
                      rows={3}
                      maxLength={1000}
                      value={remarksDraft}
                      disabled={current.busy}
                      placeholder="Enter your remarks..."
                      onChange={(event) => {
                        setRemarksDraft(event.target.value);
                        if (inputError) setInputError("");
                      }}
                    />
                    <div className="tp-ai-confirm-actions">
                      <button
                        type="button"
                        className="btn tp-ai-confirm-accept btn-sm"
                        disabled={current.busy || !readable(remarksDraft)}
                        onClick={() => void submitRemarks()}
                      >
                        <SvgIcon name="approval" size={16} /> Continue to
                        Confirmation
                      </button>
                      <button
                        type="button"
                        className="btn tp-ai-confirm-cancel btn-sm"
                        disabled={current.busy}
                        onClick={cancelRemarks}
                      >
                        <SvgIcon name="close" size={16} /> Cancel
                      </button>
                    </div>
                  </div>
                )}
                {current.review && !current.pending && (
                  <div className="tp-ai-confirm-card tp-ai-review-card">
                    <strong>Review Leave Details</strong>
                    {confirmationFields
                      .filter(
                        ([, field]) =>
                          current.review[field] !== undefined &&
                          current.review[field] !== null &&
                          String(current.review[field]).trim() !== "",
                      )
                      .map(([label, field]) => (
                        <div className="tp-ai-confirm-field" key={field}>
                          <span>{label}</span>
                          <strong>
                            {formatLeaveField(field, current.review[field])}
                          </strong>
                        </div>
                      ))}
                    <p className="tp-ai-review-note">
                      Review these details and provide any required remarks in
                      the message box. Approval or rejection will only be
                      available after the confirmation.
                    </p>
                  </div>
                )}
                {moduleId !== "timesheet" && current.pending && (
                  <div className="tp-ai-confirm-card">
                    <strong>Confirm leave action</strong>
                    {confirmationFields
                      .filter(
                        ([, field]) =>
                          current.pending[field] !== undefined &&
                          current.pending[field] !== null &&
                          String(current.pending[field]).trim() !== "",
                      )
                      .map(([label, field]) => (
                        <div className="tp-ai-confirm-field" key={field}>
                          <span>{label}</span>
                          <strong>
                            {formatLeaveField(field, current.pending[field])}
                          </strong>
                        </div>
                      ))}
                    <div className="tp-ai-confirm-actions">
                      <button
                        type="button"
                        className="btn tp-ai-confirm-accept btn-sm"
                        disabled={current.busy}
                        onClick={confirm}
                        title={
                          confirmLabels[current.pending.Intent] ||
                          "Confirm Leave Action"
                        }
                        aria-label={
                          confirmLabels[current.pending.Intent] ||
                          "Confirm Leave Action"
                        }
                      >
                        <SvgIcon name="approval" size={16} />{" "}
                        {confirmLabels[current.pending.Intent] ||
                          "Confirm Action"}
                      </button>
                      <button
                        type="button"
                        className="btn tp-ai-confirm-cancel btn-sm"
                        disabled={current.busy}
                        onClick={cancel}
                        title="Cancel Leave Action"
                        aria-label="Cancel Leave Action"
                      >
                        <SvgIcon name="close" size={16} /> Cancel
                      </button>
                    </div>
                  </div>
                )}
                {current.busy && (
                  <div
                    className="tp-ai-bubble assistant tp-ai-processing"
                    role="status"
                    aria-label="AI Assistant is processing your request"
                  >
                    <span className="visually-hidden">
                      AI Assistant is processing your request
                    </span>
                    <span className="tp-ai-processing-dots" aria-hidden="true">
                      <span />
                      <span />
                      <span />
                    </span>
                  </div>
                )}
                <div ref={messagesEndRef} />
              </div>
              {!current.messages.length &&
                !current.pending &&
                !current.selection && (
                  <div className="tp-ai-suggestions">
                    {(moduleId === "reports"
                      ? reportSuggestions
                      : moduleId === "timesheet"
                        ? timesheetSuggestions
                        : permittedSuggestions
                    ).map((prompt) => (
                      <button
                        type="button"
                        key={prompt}
                        onClick={() => update({ draft: prompt })}
                      >
                        {prompt}
                      </button>
                    ))}
                  </div>
                )}
              {inputError && (
                <div className="tp-ai-input-error" role="alert">
                  {inputError}
                </div>
              )}
              <div className="tp-ai-compose">
                <TextArea
                  id="tp-ai-message"
                  name="aiAssistantMessage"
                  rows={2}
                  maxLength={moduleId === "timesheet" ? 2000 : undefined}
                  ariaLabel="Message AI Assistant"
                  placeholder={
                    moduleId === "reports"
                      ? "Describe the leave report to generate..."
                      : moduleId === "timesheet"
                        ? "Ask about timesheet status, submission, or actions..."
                        : canApprove
                          ? "Ask about leave balance, history, status, or actions..."
                          : "Ask about leave balance, history, status, or requests..."
                  }
                  value={current.draft}
                  disabled={
                    current.busy ||
                    (moduleId !== "timesheet" && !!current.pending) ||
                    !!current.selection ||
                    !!current.remarksStep ||
                    (moduleId === "timesheet" &&
                      !!current.clarification?.requiresRemarks)
                  }
                  onChange={(event) => {
                    update({ draft: event.target.value });
                    if (inputError) setInputError("");
                  }}
                  onKeyDown={(event) => {
                    if (
                      event.key === "Enter" &&
                      !event.shiftKey &&
                      !event.nativeEvent.isComposing
                    ) {
                      event.preventDefault();
                      void send();
                    }
                  }}
                />
                <button
                  type="button"
                  className="btn tp-ai-send-button"
                  title="Send Message"
                  aria-label="Send Message"
                  disabled={
                    !readable(current.draft) ||
                    current.busy ||
                    (moduleId !== "timesheet" && !!current.pending) ||
                    !!current.selection ||
                    !!current.remarksStep ||
                    (moduleId === "timesheet" &&
                      !!current.clarification?.requiresRemarks)
                  }
                  onClick={() => void send()}
                >
                  <SvgIcon name="send" size={18} />
                </button>
              </div>
            </>
          )}
        </section>
      )}
      <button
        type="button"
        className="tp-ai-launcher"
        title="AI Assistant"
        aria-label="AI Assistant"
        aria-expanded={open}
        onClick={() => setOpen((value) => !value)}
      >
        <SvgIcon name={open ? "close" : "aiAssistant"} size={20} />
      </button>
    </div>
  );
}
