export const leaveTypeValidationRules = {
  leaveTypeCode: {
    label: "Leave Type Code",
    required: true,
    maxLength: 100,
  },
  leaveType: {
    label: "Leave Type",
    required: true,
    maxLength: 200,
  },
  leaveTypeDescription: {
    label: "Leave Type Description",
    required: true,
    maxLength: 2000,
  },
  defaultLeaveCount: {
    label: "Default Leave Count",
    required: true,
    number: true,
    minValue: 0,
  },
};
