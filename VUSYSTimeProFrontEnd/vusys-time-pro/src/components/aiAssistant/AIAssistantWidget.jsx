import { useEffect, useRef, useState } from "react";
import SvgIcon from "../common/SvgIcon";
import TextArea from "../forms/TextArea";
import { useAuth } from "../../context/AuthContext";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { aiAssistantModules } from "./aiAssistantModules";
import {
  processAILeaveRequest,
  confirmAILeaveAction,
} from "../../services/aiLeaveManagementService";
import "./AIAssistantWidget.css";

const initialConversation = () => ({
  messages: [],
  draft: "",
  pending: null,
  busy: false,
});
const normalizeRole = (user) =>
  String(user?.role ?? user?.UserRole ?? "")
    .trim()
    .toLowerCase();
const readable = (value) => String(value ?? "").trim();
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
        ),
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
  const canApply = hasPermission("My Leave Requests", "Apply");
  const canApprove = hasPermission("Leave Approval/Rejection List", "Approve");
  const [open, setOpen] = useState(false);
  const [panelPosition, setPanelPosition] = useState(null);
  const [inputError, setInputError] = useState("");
  const dragRef = useRef(null);
  const [moduleId, setModuleId] = useState(null);
  const [conversations, setConversations] = useState({
    leaveManagement: initialConversation(),
  });
  const current = conversations.leaveManagement;
  const messagesEndRef = useRef(null);
  const requestRef = useRef(false);
  const sessionRef = useRef(null);
  const epochRef = useRef(0);
  const userKey = eligible ? String(user?.userId ?? user?.id ?? "") : "";
  const sessionKey = eligible && userKey ? userKey : null;

  useEffect(() => {
    if (sessionRef.current !== sessionKey) {
      sessionRef.current = sessionKey;
      epochRef.current += 1;
      requestRef.current = false;
      setOpen(false);
      setModuleId(null);
      setConversations({
        leaveManagement: sessionKey
          ? restoreConversation(sessionKey)
          : initialConversation(),
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
          messages: conversations.leaveManagement.messages.slice(-100),
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
      leaveManagement:
        typeof change === "function"
          ? change(previous.leaveManagement)
          : { ...previous.leaveManagement, ...change },
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
    ]
      .filter(Boolean)
      .join(", ") || "with leave-related questions"
  }. How can I help you today?`;

  const send = async () => {
    const message = readable(current.draft);
    if (!message || requestRef.current || current.pending) return;
    if (!isMeaningfulMessage(message)) {
      setInputError(
        "Please enter a meaningful leave-related request (8–2000 characters). For example: Apply leave tomorrow for personal work.",
      );
      return;
    }
    setInputError("");
    requestRef.current = true;
    const epoch = epochRef.current;
    update((previous) => ({
      ...previous,
      draft: "",
      busy: true,
      messages: [...previous.messages, newMessage("user", message)],
    }));
    try {
      const details = await processAILeaveRequest(message);
      if (epoch !== epochRef.current) return;
      const token = readable(details.ConfirmationToken);
      update((previous) => ({
        ...previous,
        busy: false,
        messages: [
          ...previous.messages,
          newMessage(
            "assistant",
            details.Message ||
              (token
                ? "Review and confirm this action."
                : "Request processed."),
          ),
        ],
        pending: token ? { ...details, token } : null,
      }));
    } catch (error) {
      if (epoch === epochRef.current)
        append("error", error?.message || "Unable to process the request.");
    } finally {
      if (epoch === epochRef.current) {
        requestRef.current = false;
        update({ busy: false });
      }
    }
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
  const resetConversation = () => {
    if (requestRef.current) return;
    epochRef.current += 1;
    update(initialConversation());
    setInputError("");
  };
  const confirmationFields = [
    ["Intent", "Intent"],
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
    if (event.button !== 0 || event.target.closest("button")) return;
    const panel = event.currentTarget.closest(".tp-ai-panel");
    const bounds = panel.getBoundingClientRect();
    dragRef.current = {
      x: event.clientX - bounds.left,
      y: event.clientY - bounds.top,
    };
    event.currentTarget.setPointerCapture(event.pointerId);
  };
  const moveDrag = (event) => {
    if (!dragRef.current) return;
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
                {moduleId ? "AI Assistant — Leave Management" : "AI Assistant"}
              </strong>
              <small>Your smart TimePro assistant</small>
            </div>
            <div className="tp-ai-header-actions">
              {moduleId && (
                <button
                  type="button"
                  title="Back to Modules"
                  aria-label="Back to Modules"
                  onClick={() => setModuleId(null)}
                >
                  <SvgIcon name="arrowLeft" size={18} />
                </button>
              )}
              {moduleId && (
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
              {aiAssistantModules.map((module) => (
                <button
                  type="button"
                  key={module.id}
                  className="tp-ai-module-card"
                  disabled={!module.enabled}
                  onClick={() => setModuleId(module.id)}
                >
                  <SvgIcon name={module.icon} size={24} />
                  <span>
                    <strong>{module.name}</strong>
                    <small>{module.description}</small>
                  </span>
                  <span className="tp-ai-module-status">{module.status}</span>
                </button>
              ))}
            </div>
          ) : (
            <>
              <div
                className="tp-ai-messages"
                role="log"
                aria-live="polite"
                aria-relevant="additions text"
              >
                <div className="tp-ai-bubble assistant">{welcome}</div>
                {current.messages.map((message) => (
                  <div
                    key={message.id}
                    className={`tp-ai-bubble ${message.type}`}
                  >
                    <span>{message.text}</span>
                    <small>{message.time}</small>
                  </div>
                ))}
                {current.pending && (
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
                          <strong>{String(current.pending[field])}</strong>
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
                    <span>AI Assistant is processing your request</span>
                    <span className="tp-ai-processing-dots" aria-hidden="true">
                      <span>.</span>
                      <span>.</span>
                      <span>.</span>
                    </span>
                  </div>
                )}
                <div ref={messagesEndRef} />
              </div>
              {!current.messages.length && !current.pending && (
                <div className="tp-ai-suggestions">
                  {permittedSuggestions.map((prompt) => (
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
                  ariaLabel="Message AI Assistant"
                  placeholder={
                    canApprove
                      ? "Ask me to apply, approve, reject, or cancel leave..."
                      : "Ask me to apply or cancel leave..."
                  }
                  value={current.draft}
                  disabled={current.busy || !!current.pending}
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
                    !!current.pending
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
