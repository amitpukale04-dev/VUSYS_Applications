export const employeeLeaveBalanceValidationRules = {
  employeeCode: {
    label: "Employee",
    required: true,
  },
  leaveTypeID: {
    label: "Leave Type",
    required: true,
    custom: (value) => Number(value) > 0,
    customMessage: "Select The Leave Type.",
  },
  leaveYear: {
    label: "Year",
    required: true,
    custom: (value) => Number(value) > 0,
    customMessage: "Select The Year.",
  },
  openingBalance: {
    label: "Opening Balance",
    required: true,
    number: true,
    minValue: 0,
  },
  allocatedLeave: {
    label: "Allocated Leave",
    required: true,
    number: true,
    minValue: 0,
  },
  usedLeave: {
    label: "Used Leave",
    required: true,
    number: true,
    minValue: 0,
  },
  pendingLeave: {
    label: "Pending Leave",
    required: true,
    number: true,
    minValue: 0,
  },
  availableBalance: {
    label: "Available Balance",
    required: true,
    number: true,
    minValue: 0,
  },
};
