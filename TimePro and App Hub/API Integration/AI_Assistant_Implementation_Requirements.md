# VUSYS TimePro — AI Assistant Implementation Requirements

## 1. Objective

Implement a **generic, modular, role-based floating AI Assistant** in the existing VUSYS TimePro React application. It must not be a standalone page, sidebar menu item, or separate route. Clicking a floating icon opens a chatbot over the current authenticated screen.

Initially show two modules:
- **Leave Management** — Available; implement fully now.
- **Timesheet** — Coming Soon; display its module card but do not implement AI functionality or call Timesheet AI APIs yet.

The application supports **Admin** and **Employee** roles. The assistant must present role-appropriate functionality, while the backend remains the authority for authorization and business operations. Architect the assistant so additional modules can be added without rebuilding its generic UI.

## 2. Existing Technology Stack and Standards

**Follow our existing application's UI/UX and architecture standards, including its established coding conventions, component structure, API integration, authentication, security, styling, and responsive design standards.**

## 3. Login API Feature Flag and Role Integration

The Login API returns `is_enable_ai_assistant`. Despite its name, this flag controls the **entire generic AI Assistant**, not just Leave Management. It is a **runtime Login API response property**, **not** a Vite environment variable. Do not read it from `import.meta.env` or add it to `.env` files.

Read the flag from the **actual** Login API response structure, normalize it to a boolean, and store it using the application's existing authentication/session state. Accept boolean `true` and case-insensitive string `"true"` (with optional whitespace) as enabled; treat all other values, including missing/null, as disabled.

```javascript
export const normalizeAIAssistantFlag = (value) =>
  value === true ||
  (typeof value === "string" && value.trim().toLowerCase() === "true");
```

Display the floating assistant icon **only if** the user is authenticated, the session is valid, the flag is enabled, and the role is reliably identified as Admin or Employee. Read role identity from existing authentication data and role definitions; **do not invent or hardcode unknown RoleIDs**. Hide the assistant until authoritative flag and role information are available.

Reuse existing session restoration logic to retain flag/role information after refresh. Do not call Login again solely to obtain the flag or create a separate authentication store. On logout, session expiration, or account switch, hide the assistant and clear conversations, drafts, pending confirmations, and all user-specific assistant state. Frontend visibility is **not** backend authorization.

## 4. Role-Based Capabilities

| Leave Management operation | Admin | Employee |
|---|---|---|
| Apply leave | If authorized | If authorized |
| Cancel own leave | If authorized | If authorized |
| Approve leave | If authorized | Hidden unless explicitly authorized |
| Reject leave | If authorized | Hidden unless explicitly authorized |

Use existing role/security access information to decide which operation suggestions and UI actions to show. An Admin is **not automatically** authorized for every operation; a user with an Employee role might have explicit approver permissions. Backend checks are mandatory for all operations, including requests typed manually that are not offered in suggestions.

Admin welcome text (adjust to actual permissions):
> Hello! I'm your TimePro Leave Management Assistant. I can help you apply for leave, cancel your leave requests, and approve or reject employee leave requests when you have the required permissions. How can I help you today?

Employee welcome text (adjust to actual permissions):
> Hello! I'm your TimePro Leave Management Assistant. I can help you apply for leave and cancel your existing leave requests. How can I help you today?

## 5. Floating AI Assistant Launcher

Mount **one** `AIAssistantWidget` in the authenticated application layout, not on every page. Do not display it on Login, Forgot Password, OTP, Reset Password, or other unauthenticated screens.

- Circular floating AI icon at bottom-right, fixed to viewport.
- Existing TimePro branding, theme-compatible accent/purple gradient, subtle shadow, hover/focus feedback, tooltip `AI Assistant`.
- Respect mobile safe areas and avoid overlap with important UI and toast notifications.
- Click toggles the chatbot; no navigation, route change, refresh, or page replacement.
- Preserve state while minimized and across authenticated route changes.
- Keep the existing header, sidebar, footer, and page functionality unchanged.

## 6. Initial Chatbot Screen — Module Selection

On opening, show the **generic module selector**, not Leave Management chat directly.

Header: **AI Assistant**

Subtitle: **Your smart TimePro assistant**

Welcome:
> Hello! I'm your TimePro AI Assistant. I can help you manage different activities in TimePro using natural-language conversations. Please select a module to get started.

Module cards:

1. **Leave Management** — calendar icon; description `Apply, approve, reject, or cancel leave requests using AI.` (adapt description to actual role permissions); **Available**; clickable and opens Leave Management chat.
2. **Timesheet** — clock icon; description `Manage timesheet activities using AI.`; **Coming Soon**; disabled/non-interactive; no API calls.

Use a reusable module registry/configuration rather than hardcoding Leave Management business logic into the generic widget. Example:

```javascript
const aiAssistantModules = [
  {
    id: "leaveManagement",
    name: "Leave Management",
    description: "Manage leave requests using AI.",
    icon: "calendar",
    enabled: true,
    status: "Available",
  },
  {
    id: "timesheet",
    name: "Timesheet",
    description: "Manage timesheet activities using AI.",
    icon: "clock",
    enabled: false,
    status: "Coming Soon",
  },
];
```

The registry controls presentation only; module access must still respect backend permissions.

## 7. Module Navigation and Conversation Isolation

Selecting Leave Management opens its chat **inside the same floating panel**, with header `AI Assistant — Leave Management`. Provide **Back to Modules**, **New Conversation** (when a module is selected), and **Close/Minimize** actions as appropriate.

- Back to Modules preserves the module's in-memory conversation and draft; does not execute or resubmit actions.
- Closing/minimizing preserves the conversation.
- Reopening a module restores its independent conversation.
- New Conversation clears **only that module's** messages, draft, and pending confirmation.
- Do not show one module's messages in another module.
- Keep pending confirmation tokens scoped to their module; never confirm an action from the wrong module or after its token is invalidated.
- Do not persist sensitive confirmation tokens in localStorage/sessionStorage.
- Do not assume the backend provides multi-turn conversational memory unless the API contract explicitly supports it.

## 8. Generic Architecture and Suggested Components

Separate shared chatbot UI/state/navigation from module-specific prompts, API mapping, permissions, and confirmation logic.

Suggested structure (adapt to existing project):

```text
src/
  components/
    aiAssistant/
      AIAssistantWidget.jsx
      AIAssistantWidget.css
      AIAssistantChatWindow.jsx
      AIAssistantModuleSelector.jsx
      AIAssistantMessage.jsx
      AIAssistantInput.jsx
      AIAssistantTypingIndicator.jsx
      modules/
        leaveManagement/
          AILeaveManagementChat.jsx
          AILeavePromptSuggestions.jsx
          AILeaveConfirmationCard.jsx
        timesheet/
          (future implementation; do not create empty files)
```

Place API functions according to the existing API service architecture. Reuse existing components and utilities; do not create unnecessary files, libraries, or duplicate state stores.

## 9. Chatbot Layout and Styling

- **Desktop:** floating panel above icon; suggested width 400–440px and max height 650–700px, constrained by viewport; rounded corners; scrollable conversation; message input at bottom.
- **Tablet:** adaptive panel size and margins; no horizontal overflow.
- **Mobile:** near-full-screen panel, safe-area padding, touch-friendly controls, visible back/close actions, accessible input when virtual keyboard opens, no horizontal scrolling.
- Follow existing TimePro light/dark theme and CSS variables (including `--tp-form-control-border` where applicable). Do not introduce conflicting global CSS.
- Keep page interaction available where practical; avoid a global blocking backdrop for the chatbot.

## 10. Leave Management — Current Implementation Scope

Implement these backend-supported intents:

- `ApplyLeave`
- `ApproveLeave`
- `RejectLeave`
- `CancelLeave`

Use role-appropriate welcome messages, prompts, and input placeholders. Do not implement AI intent detection, date extraction, leave-type classification, business validation, or permission decisions in React; these are backend responsibilities.

### Suggested prompts

**Apply (authorized Admin/Employee):**
- I am not feeling well and need sick leave tomorrow.
- Apply casual leave tomorrow because of personal work.
- I worked on Sunday and want comp off tomorrow.
- I need leave next Monday for a family function.
- Apply half-day leave tomorrow for personal work.

**Cancel own leave (authorized Admin/Employee):**
- Cancel my leave request 125 because my plans have changed.

**Approve (authorized approver only):**
- Approve leave request 127. Remarks: Approved as requested.

**Reject (authorized approver only):**
- Reject leave request 126 because of an upcoming project deadline.

The request IDs are **examples only**; never treat them as actual records or submit them automatically. Clicking a suggested prompt **populates the input** without sending it.

### Input

- Admin/approver placeholder (as authorized): `Ask me to apply, approve, reject, or cancel leave...`
- Employee placeholder (as authorized): `Ask me to apply or cancel leave...`
- Enter sends; Shift+Enter inserts newline; trim input; disable Send if empty; support multiline auto-resizing; prevent duplicate submissions; preserve drafts on minimize; mobile keyboard support.

### Messages

- User messages right-aligned, assistant messages left-aligned.
- Theme-compatible bubbles, timestamps, multiline wrapping, assistant icon.
- Show validation messages, confirmation cards, success/error messages, and typing/loading state.
- Scroll to latest message when appropriate without disrupting intentional scrollback.
- Show `AI Assistant is processing your request...` during API processing; no fake progress percentages.

## 11. Leave Management Backend API Integration

Use the application's **existing authenticated API client**, base URL, interceptors, authorization handling, response conventions, toast/error utilities, and validation helpers.

### Process AI Leave Request

`POST /api/AIAgentLeaveManagement/ProcessAILeaveManagementRequest`

Illustrative request:

```json
{
  "message": "I am not feeling well and need sick leave tomorrow."
}
```

The backend handles intent, dates, leave type, reason/remarks, validation, permissions, and confirmation-token creation.

### Confirm AI Leave Action

Integrate the existing backend operation **`ConfirmAILeaveManagementAction`**. Inspect the **actual Swagger contract** to determine its HTTP method, route, request DTO, response DTO, token field, and error format. **Do not invent** fields or endpoints.

Do not call Gemini directly from React. Do not expose the Gemini API key. Backend Gemini model fallback/rate-limit handling must not trigger frontend resubmissions.

## 12. Confirmation Workflow

**Every leave-changing action requires explicit user confirmation.** When the processing API returns confirmation-required details and a backend-generated token, render a confirmation card within Leave Management chat.

- **Apply Leave:** Leave Type, From Date, To Date, Full/Half Day, Half-Day Type, Number of Leave Days if provided, Reason; buttons **Confirm Apply Leave** and **Cancel**.
- **Approve Leave:** Leave Request ID, employee details if provided, remarks; buttons **Confirm Approval** and **Cancel**.
- **Reject Leave:** Leave Request ID, employee details if provided, remarks; buttons **Confirm Rejection** and **Cancel**.
- **Cancel Leave:** Leave Request ID, cancellation remarks; buttons **Confirm Cancellation** and **Cancel**.

Display only fields actually returned by the backend; do not invent missing data. Use existing TimePro confirmation UI patterns where appropriate.

- Never execute without explicit confirmation and valid backend token.
- Disable buttons during confirmation; prevent duplicate requests.
- Clear pending state on successful confirmation or user cancellation.
- Never reuse expired/invalid tokens or tokens from another module.
- Do not automatically retry a confirmation request after an ambiguous network failure.
- Show backend success/failure messages and preserve relevant conversation context.

## 13. Leave Type Display

The backend classifies leave types. Display the exact returned value; do not replicate classification logic in React.

| Example employee message | Expected backend leave type |
|---|---|
| I am sick tomorrow | Sick Leave |
| I have fever and need leave | Sick Leave |
| Apply comp off tomorrow | COMPOFF |
| I worked on Sunday and need leave | COMPOFF |
| Apply casual leave tomorrow | Privileage Leave |
| I have personal work tomorrow | Privileage Leave |
| I need leave tomorrow | Privileage Leave |

Preserve the database spelling **`Privileage Leave`**.

## 14. Errors, Loading, and Security

Handle success, confirmation required, missing request ID, missing reason/remarks, invalid dates/type, request not found, unauthorized operation, already approved/rejected/cancelled, Gemini timeout, rate limit, all models unavailable, network failure, session expiration, and unexpected server errors.

Use existing `showApiError` where appropriate and `applyApiValidationErrors` from **`CustomAPIValidation.js`** for compatible field errors. Avoid duplicate global toasts. Display user-friendly messages; never expose raw stack traces, internal exceptions, API keys, or sensitive data.

A manually typed unauthorized instruction (e.g., an Employee asks to approve a leave request) must never bypass backend authorization. Never invent LeaveRequestIDs or confirmation tokens. Never render AI-generated content as unsafe HTML.

## 15. Accessibility and Responsiveness

- Keyboard-accessible floating icon, module cards, input, and confirmation controls.
- Suitable `aria-label`, `aria-expanded`, focus management, and visible focus indicators.
- Appropriate chat message announcements and reduced-motion support.
- Test desktop, tablet, mobile, light/dark themes, mobile keyboard, safe areas, overlay positioning, and long messages.
- Preserve existing application styling and behavior.

## 16. Timesheet — Future Module

Show **Timesheet** as **Coming Soon** on the module selector for eligible users. Do not implement Timesheet AI intent extraction, endpoints, chat processing, mock responses, or confirmation workflows now. Design for future role/permission-aware Timesheet capabilities, but do not assume their contracts or operations.

## 17. Functional Testing Checklist

### Login flag, role, and session
- Enabled flag + Admin: assistant visible.
- Enabled flag + Employee: assistant visible.
- Disabled/missing/invalid flag: assistant hidden for either role.
- Unknown/unresolved role: assistant hidden.
- Logout/session expiration/account switch: user-specific state cleared.
- Authenticated route navigation: widget/conversation retained.
- Refresh: restore flag/role through existing session mechanism.

### Module selection and chat
- Floating icon opens module selector, not Leave Management directly.
- Leave Management Available and interactive.
- Timesheet Coming Soon and non-interactive; no API calls.
- Back, minimize, reopen, New Conversation, draft preservation, and per-module isolation.
- No sidebar menu item, new route, or page navigation.
- Suggested prompts populate but do not auto-submit.
- Duplicate message and confirmation requests prevented.

### Role-based operations
- Admin sees only authorized prompts/actions.
- Employee sees Apply/Cancel by default, not unauthorized Approve/Reject.
- Explicit approver permissions are honored regardless of role label.
- Unauthorized typed operations are rejected by backend.
- Leave ownership is enforced by backend.

### Leave AI and confirmation
- ApplyLeave, ApproveLeave, RejectLeave, CancelLeave.
- Sick Leave, COMPOFF, Privileage Leave, half-day.
- Missing remarks/ID, invalid dates, request not found.
- Confirmation success/failure/expiry/cancellation.
- Gemini timeout/rate limit/all models unavailable, network failures.
- Desktop/tablet/mobile and light/dark mode.

## 18. Implementation Instructions

Before coding, inspect:
1. Existing Login API integration and actual response shape.
2. Authentication/session state and refresh restoration.
3. Existing Admin/Employee role definitions and security access.
4. Authenticated application layout.
5. Existing API service conventions and Swagger contracts.
6. ToastService, `showApiError`, and `CustomAPIValidation.js`.
7. Theme variables, reusable UI components, and responsive CSS patterns.

Then implement the generic floating assistant and Leave Management integration using established project patterns. Do not rewrite authentication, introduce unnecessary libraries, hardcode unknown role IDs, alter sidebar navigation, create an assistant route, implement Timesheet AI prematurely, or modify unrelated functionality.

Do not use mock API responses in the final implementation. If API contracts or source files are missing, identify precisely what is needed rather than inventing fields.

## 19. Expected Deliverables

1. Login API feature flag normalization and integration with existing auth state.
2. Role/permission-aware global floating AI Assistant launcher.
3. Generic chatbot window, header, module selector, Back/New Conversation/Close controls.
4. Leave Management module with role-aware prompts, conversation, message input, loading, API integration, and confirmation cards.
5. Timesheet Coming Soon module card without backend functionality.
6. Isolated module state, safe session cleanup, responsive light/dark UI, accessibility.
7. Functional tests for feature flag, roles, chat, leave operations, confirmations, errors, and responsiveness.

After implementation, report files created/modified, feature-flag and role integration, architecture, Leave Management integration, future Timesheet extension approach, tests performed, and remaining limitations.

**Final requirement:** The result must be a **generic, modular, role-based floating AI Assistant** consistent with VUSYS TimePro standards, with **Leave Management functional now** and **Timesheet visibly Coming Soon**. All leave operations require backend validation, authorization, and explicit confirmation.
