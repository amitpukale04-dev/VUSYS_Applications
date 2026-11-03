export const downloadTimesheetValidationRules = {
  employeeCode: {
    label: "Employee",
    required: true,
    messages: { required: "Select The Employee." },
  },
  timesheetMonth: {
    label: "Timesheet Month",
    required: true,
    messages: { required: "Select The Timesheet Month." },
  },
  timesheetYear: {
    label: "Timesheet Year",
    required: true,
    number: true,
    minValue: 1,
    messages: {
      required: "Select The Timesheet Year.",
      minValue: "Select The Timesheet Year.",
    },
  },
};
