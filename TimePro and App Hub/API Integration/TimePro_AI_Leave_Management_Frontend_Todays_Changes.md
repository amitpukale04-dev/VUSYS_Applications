# VUSYS TimePro – AI Leave Management Frontend Changes (Today's Update)

## Objective

The AI Leave Management frontend was already implemented yesterday and is working.

Today, the .NET 10 Web API backend was enhanced to improve leave request searching and selection.

**Update only the existing AI Leave Management frontend implementation to support today's backend changes.**

Do not recreate the AI Leave Assistant, redesign the UI, or change existing working functionality.

## 1. Backend Changes Completed Today

The backend now supports:

1. Filtering eligible leave requests directly in SQL Server before applying `TOP (51)`.
2. Passing the AI action intent (`ApplyLeave`, `CancelLeave`, `ApproveLeave`, `RejectLeave`) through the backend.
3. Excluding ineligible leave statuses from search results.
4. Returning multiple eligible leave requests when more than one request matches.
5. Requiring explicit user selection when multiple eligible leave requests are found.
6. Continuing the existing confirmation-token workflow after identifying the correct leave request.

### Leave Status Eligibility

| AI Action | Eligible Leave Status |
|---|---|
| CancelLeave | Pending (1), Approved (2) |
| ApproveLeave | Pending (1) |
| RejectLeave | Pending (1) |

The backend handles these validations. The frontend must display the results correctly without duplicating backend business logic.

## 2. Review the Existing Frontend Implementation

Before making changes, inspect the existing:

- AI Leave Management page
- Chat message components
- API service methods
- Confirmation modal
- Clarification handling
- Multiple leave request handling, if already implemented
- Authentication and security implementation
- Existing CSS and responsive styling

Identify which parts already support today's changes.

**Modify only what is missing or requires adjustment.**

Do not create duplicate components or API service methods.

## 3. Multiple Eligible Leave Requests

This is the primary frontend change.

When the Process AI Leave Management API returns multiple eligible leave requests, display them in a professional, user-friendly selection interface within the existing AI conversation.

The backend may return a response similar to:

```json
{
  "message": "OK",
  "Status": "Error",
  "data": {
    "AI_LEAVE_MANAGEMENT_RESPONSE": [
      {
        "Message": "Multiple eligible leave requests match. Select the correct LeaveRequestID and send the request again.",
        "Intent": "CancelLeave",
        "FromDate": "2026-11-20",
        "ToDate": "2026-11-20",
        "ConfirmationToken": ""
      }
    ],
    "AI_LEAVE_REQUEST_DETAILS_RESPONSE": [
      {
        "LeaveRequestID": 2011,
        "EmployeeCode": "VUT002",
        "EmployeeName": "Example Employee",
        "LeaveType": "Casual Leave",
        "FromDate": "2026-11-20",
        "ToDate": "2026-11-20",
        "TotalLeaveDays": 0.5,
        "LeaveStatus": 1
      }
    ]
  }
}
```

This is an illustrative response only. Inspect the actual API response and use its real field names and values.

### Required Behavior

If multiple eligible leave requests are returned:

1. Display the backend message.
2. Display the eligible leave requests in a selectable list or responsive table.
3. Allow the user to select exactly one leave request.
4. Show the selected leave request details.
5. Continue the original AI action using the selected `LeaveRequestID`.
6. Call the existing Process API again to obtain a new confirmation token.
7. Display the existing confirmation modal.
8. Execute the action only after explicit confirmation.

**Never automatically select the latest leave request or the first record.**

## 4. Leave Request Selection UI

Display the following fields when available:

- LeaveRequestID
- Employee Code
- Employee Name
- Leave Type
- From Date
- To Date
- Total Leave Days
- Half-Day Type
- Leave Status
- Reason
- Action button: Select

Use the existing TimePro UI styling.

### Selection Example

If two eligible leave requests match:

| LeaveRequestID | Employee | Leave Dates | Status | Action |
|---|---|---|---|---|
| 2011 | Employee A | 20 Nov 2026 | Pending | Select |
| 2015 | Employee A | 20 Nov 2026 | Approved | Select |

The user must explicitly select one record.

After selecting `2011`, continue the original action by submitting a new message containing the selected LeaveRequestID.

For example:

Original request:

> Cancel my leave for 20 November 2026 because my plans have changed.

Follow-up request:

> Cancel my leave request ID 2011 because my plans have changed.

For approval:

> Approve leave request ID 2011. Remarks: Approved by manager.

For rejection:

> Reject leave request ID 2011. Remarks: Critical project delivery.

Use the actual selected ID and preserve the original intent, remarks, and relevant context.

Do not invent missing remarks or cancellation reasons.

Do not call the Confirm API directly after selection. The Process API must first generate a valid confirmation token for the selected request.

## 5. Handle API Response Status Correctly

The existing API uses DataSet-based JSON responses.

Inspect:

- Top-level `Status`
- `AI_LEAVE_MANAGEMENT_RESPONSE`
- `AI_LEAVE_REQUEST_DETAILS_RESPONSE`
- Row-level `Status`, if present
- `Message`
- `Intent`
- `ConfirmationToken`
- `ClarificationToken`

A multiple-match response may contain `Status = Error` while also providing valid leave request records for selection.

**Do not treat every Error response as a terminal failure.**

If the backend explicitly returns multiple eligible leave requests, display the selection interface rather than only showing an error toast.

For other errors, preserve the existing ToastService and error-handling behavior.

## 6. Confirmation Token Handling

After the user selects a leave request:

1. Clear any stale confirmation token associated with the ambiguous request.
2. Send the selected LeaveRequestID to the Process API.
3. Wait for the new confirmation response.
4. Display the selected leave request details.
5. Allow the user to confirm or cancel.
6. Call the existing Confirm API with the newly generated confirmation token.

Do not reuse old confirmation tokens.

Prevent duplicate API calls while processing.

Preserve the existing confirmation modal and its styling.

## 7. ApplyLeave Workflow

The ApplyLeave workflow is already working.

No new ApplyLeave functionality is required as part of today's update.

Verify that the existing implementation still supports:

- Single-day leave
- Multiple-day leave
- First-half leave
- Second-half leave
- Leave type resolution
- Clarification tokens
- Confirmation tokens

Do not modify the ApplyLeave workflow unless today's changes have caused a regression.

## 8. CancelLeave Workflow

Verify these scenarios:

**Scenario A: One eligible request**

- The backend returns one matching request.
- Display the confirmation modal.
- Continue the existing cancellation workflow.

**Scenario B: Multiple eligible requests**

- Display all returned eligible requests.
- Require explicit selection.
- Reprocess the selected LeaveRequestID.
- Display the confirmation modal.

**Scenario C: No eligible requests**

- Display the backend message.
- Do not show confirmation actions.

**Scenario D: Already cancelled or rejected request**

- Display the backend validation message.
- Do not allow cancellation.

## 9. ApproveLeave Workflow

Verify these scenarios:

- One pending leave request: continue to confirmation.
- Multiple pending requests: require LeaveRequestID selection.
- Already approved request: display validation error.
- Rejected or cancelled request: display validation error.
- Unauthorized approver: display permission error.

Preserve the original approval remarks when selecting a leave request.

Do not automatically approve any request.

## 10. RejectLeave Workflow

Verify these scenarios:

- One pending leave request: continue to confirmation.
- Multiple pending requests: require LeaveRequestID selection.
- Already rejected request: display validation error.
- Approved or cancelled request: display validation error.
- Missing rejection remarks: preserve existing validation.
- Unauthorized approver: display permission error.

Preserve the original rejection remarks when selecting a leave request.

Do not automatically reject any request.

## 11. State Management

Use the existing React state-management approach.

If required, maintain state for:

- Original AI message
- Original AI intent
- Original remarks
- Multiple matching leave requests
- Selected LeaveRequestID
- Selection loading state
- Confirmation token

Clear the multiple-selection state when:

- A leave request is successfully selected and reprocessed.
- The user starts a completely new request.
- The conversation is cleared.
- The action completes successfully.
- The user cancels the selection.

Prevent stale responses from replacing the current conversation state.

## 12. UI/UX and Responsiveness

Preserve the existing VUSYS TimePro styling.

Requirements:

- React 19.2.0
- Bootstrap 5.3.8
- Existing Light/Dark mode
- Existing CSS variables
- Existing ToastService
- Existing confirmation modal
- Existing authentication
- Existing security permissions
- Existing PWA functionality

The leave request selection UI must work correctly on:

- Desktop
- Tablet
- Mobile

For mobile, use responsive cards or an appropriately scrollable table.

Avoid horizontal page overflow.

Do not introduce new UI frameworks or unnecessary dependencies.

## 13. Testing

Test the following cases:

1. CancelLeave with one eligible request.
2. CancelLeave with multiple eligible requests.
3. CancelLeave with no eligible requests.
4. ApproveLeave with one pending request.
5. ApproveLeave with multiple pending requests.
6. RejectLeave with one pending request.
7. RejectLeave with multiple pending requests.
8. Select a LeaveRequestID and continue processing.
9. Preserve approval/rejection remarks.
10. Generate a fresh confirmation token after selection.
11. Confirm the selected leave request.
12. Cancel the confirmation without executing the action.
13. Handle expired confirmation tokens.
14. Handle API failures and unauthorized access.
15. Verify ApplyLeave remains unchanged.
16. Verify Light/Dark mode.
17. Verify desktop, tablet, and mobile responsiveness.

## 14. Implementation Rules

- Do not recreate the AI Leave Management page.
- Do not redesign the existing chat interface.
- Do not modify unrelated components.
- Do not modify backend APIs.
- Do not change the existing working ApplyLeave functionality.
- Do not hardcode leave statuses into frontend decision-making beyond display formatting.
- Do not automatically select any matching leave request.
- Do not automatically execute cancellation, approval, or rejection.
- Reuse the existing API services and utility methods.
- Follow existing code formatting, naming, comments, and indentation.
- Maintain the existing authentication and security architecture.
- Ensure full responsiveness.

## 15. Expected Deliverables

1. Review the existing AI Leave Management frontend implementation.
2. Identify changes required to support today's backend updates.
3. Implement only the necessary changes.
4. Update existing components rather than creating duplicates.
5. Verify multiple leave-request selection.
6. Verify confirmation-token handling.
7. Verify all four AI actions continue working.
8. Provide a summary of modified files and functionality.

**Important Final Instruction:**

Yesterday's frontend implementation is already completed and working.

Today's task is strictly an incremental enhancement to support the updated SQL-side eligibility filtering and explicit LeaveRequestID selection when multiple eligible leave requests match.

Preserve all existing functionality, styling, authentication, security, and responsiveness.
