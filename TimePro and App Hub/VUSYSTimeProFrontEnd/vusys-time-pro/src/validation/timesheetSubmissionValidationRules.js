export const timesheetSubmissionValidationRules = {
  timesheetMonth: {
    label: "Month",
    required: true,
    messages: { required: "Select The Month." },
  },
  timesheetYear: {
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
