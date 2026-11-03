export const employeeProjectMappingValidationRules = {
  employeeCode: {
    label: "Employee",
    required: true,
    messages: { required: "Select The Employee." },
  },
  projectID: {
    label: "Project",
    required: true,
    number: true,
    minValue: 1,
    messages: {
      required: "Select The Project.",
      minValue: "Select The Project.",
    },
  },
};
