// ============================================================
// MY LEAVE REQUEST VALIDATION RULES
// ============================================================

export const myLeaveRequestValidationRules = {
  employeeCode: { label: "Employee Code", required: true },
  leaveTypeID: {
    label: "Leave Type",
    required: true,
    custom: (value) => Number(value) > 0,
    customMessage: "Select The Leave Type.",
    messages: { required: "Select The Leave Type." },
  },
  fromDate: { label: "From Date", required: true },
  toDate: {
    label: "To Date",
    required: true,
    custom: (value, values) => {
      if (!value || !values?.fromDate) return true;
      return String(value) >= String(values.fromDate);
    },
    customMessage: "To Date Should Be Greater Than From Date.",
  },
  halfDayType: {
    label: "Half Day Type",
    custom: (value, values) =>
      !values?.isHalfDay || ["First Half", "Second Half"].includes(value),
    customMessage: "Select Half Day Type.",
  },
  reason: { label: "Leave Reason", required: true, maxLength: 2000 },
};
