export const leaveReportValidationRules = {
  employeeCode: {
    label: "Employee",
    required: true,
    messages: { required: "Select The Employee." },
  },
  leaveMonth: {
    label: "Month",
    required: true,
    messages: { required: "Select The Month." },
  },
  leaveYear: {
    label: "Year",
    required: true,
    number: true,
    minValue: 1,
    messages: {
      required: "Select The Year.",
      minValue: "Select The Year.",
    },
  },
};
