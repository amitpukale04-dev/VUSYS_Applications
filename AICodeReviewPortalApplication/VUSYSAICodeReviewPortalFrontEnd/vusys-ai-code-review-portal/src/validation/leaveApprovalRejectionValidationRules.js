// ============================================================
// LEAVE APPROVAL / REJECTION VALIDATION RULES
// ============================================================

export const leaveApproveRejectValidationRules = {
  approvalStatus: {
    label: "Approval Status",
    required: true,
    messages: { required: "Select The Approval Status." },
  },
  remarks: {
    label: "Remarks",
    required: true,
    messages: { required: "Enter The Remarks." },
  },
};
