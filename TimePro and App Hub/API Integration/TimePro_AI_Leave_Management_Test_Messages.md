# VUSYS TimePro — AI Leave Management Test Messages

Test scenarios for the Google Gemini–powered Leave Management Agent in the .NET 10 Web API.

## API endpoint

`POST /api/AIAgentLeaveManagement/ProcessAILeaveManagementRequest`

Use a valid leave request ID from your database in place of the example IDs below. These examples show the expected **extracted action**, not a completed database operation.

## 1. CancelLeave

| Scenario | Employee message |
|---|---|
| Simple cancellation | Cancel my leave request 125 because my plans have changed. |
| Personal reasons | I want to cancel leave request 125 due to personal reasons. |
| No longer required | Please cancel my leave application 125 because I no longer need leave. |
| Work commitment | Cancel leave request 125 because I need to attend an important office meeting. |

**Swagger request**

```json
{
  "message": "Cancel my leave request 125 because my plans have changed."
}
```

**Expected Gemini extraction**

```json
{
  "Intent": "CancelLeave",
  "LeaveType": null,
  "FromDate": null,
  "ToDate": null,
  "IsHalfDay": false,
  "HalfDayType": null,
  "Reason": null,
  "LeaveRequestID": 125,
  "Remarks": "My plans have changed."
}
```

## 2. RejectLeave

| Scenario | Employee message |
|---|---|
| Simple rejection | Reject leave request 126 because of insufficient staffing. |
| Project deadline | Please reject leave request 126 due to an upcoming project deadline. |
| Business requirements | Reject employee leave request 126 because of critical business requirements. |
| Team availability | I want to reject leave application 126 because multiple team members are already on leave. |

**Swagger request**

```json
{
  "message": "Reject leave request 126 because of an upcoming project deadline."
}
```

**Expected Gemini extraction**

```json
{
  "Intent": "RejectLeave",
  "LeaveType": null,
  "FromDate": null,
  "ToDate": null,
  "IsHalfDay": false,
  "HalfDayType": null,
  "Reason": null,
  "LeaveRequestID": 126,
  "Remarks": "Upcoming project deadline."
}
```

## 3. ApproveLeave

| Scenario | Employee message |
|---|---|
| Simple approval | Approve leave request 127 with remarks Approved. |
| Approval with remarks | Please approve leave request 127. Remarks: Approved as requested. |
| Manager approval | Approve employee leave application 127. Remarks: Leave approved. |
| Planned leave | Approve leave request 127 because the employee has planned the leave in advance. |

**Swagger request**

```json
{
  "message": "Approve leave request 127. Remarks: Approved as requested."
}
```

**Expected Gemini extraction**

```json
{
  "Intent": "ApproveLeave",
  "LeaveType": null,
  "FromDate": null,
  "ToDate": null,
  "IsHalfDay": false,
  "HalfDayType": null,
  "Reason": null,
  "LeaveRequestID": 127,
  "Remarks": "Approved as requested."
}
```

## 4. Backend validation rules

| Validation | CancelLeave | RejectLeave | ApproveLeave |
|---|---|---|---|
| Valid LeaveRequestID | Required | Required | Required |
| Remarks | Required | Required | Required |
| Request must exist | Yes | Yes | Yes |
| Employee ownership | Own leave only | Approver permission | Approver permission |
| Allowed request status | Pending or Approved | Pending only | Pending only |
| Self-approval restriction | Not applicable | Yes | Yes |
| Confirmation required | Yes | Yes | Yes |

The current backend requires remarks for **all three** actions, including ApproveLeave. Approval and rejection require a user with approval permission; self-approval is not allowed.

## 5. Confirmation workflow

1. **Process the message:** Send a request to `ProcessAILeaveManagementRequest`. Gemini extracts the intent, request ID, and remarks. The backend validates the request and, if eligible, returns `ConfirmationRequired` with a confirmation token.
2. **Confirm the operation:** Send the returned confirmation token to the existing `ConfirmAILeaveManagementAction` endpoint. The backend rechecks the request, ownership/permissions, and status before executing the operation.

**Testing guidance:** Test cancellation using your own pending or approved leave request. Test approval and rejection using a pending request belonging to another employee and a user with approval permissions. Replace IDs 125, 126, and 127 with actual database IDs.
