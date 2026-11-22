export const timesheetApprovalRejectionFilterValidationRules = {
  employeeCode: {
    label: "Employee",
    required: true,
    messages: { required: "Select The Employee." },
  },
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

export const timesheetApproveRejectValidationRules = {
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


export const timesheetApprovalRejectionUrlValidationRules = {
  employeeCode: {
    label: "Employee Code",
    required: true,
    messages: { required: "Enter The Employee Code." },
  },
  timesheetMonth: {
    label: "Timesheet Month",
    required: true,
    messages: { required: "Enter The Timesheet Month." },
  },
  timesheetYear: {
    label: "Timesheet Year",
    required: true,
    number: true,
    minValue: 1,
    messages: {
      required: "Enter The Timesheet Year.",
      minValue: "Timesheet Year Should Be Greater Than 0.",
    },
  },
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
