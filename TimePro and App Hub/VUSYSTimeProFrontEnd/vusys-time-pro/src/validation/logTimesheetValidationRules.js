export const logTimesheetListValidationRules = {
  fromDate: {
    label: "From Date",
    required: true,
    messages: { required: "Select The From Date." },
  },
  toDate: {
    label: "To Date",
    required: true,
    messages: { required: "Select The To Date." },
    custom: (value, values) => {
      if (!value || !values?.fromDate) return true;
      return String(value) > String(values.fromDate);
    },
    customMessage: "To Date Should Be Greater Than From Date.",
  },
};

export const logTimesheetTaskValidationRules = {
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
  projectActivityID: {
    label: "Activity",
    required: true,
    number: true,
    minValue: 1,
    messages: {
      required: "Select The Activity.",
      minValue: "Select The Activity.",
    },
  },
  taskDate: {
    label: "Task Date",
    required: true,
    messages: { required: "Select The Task Date." },
  },
  noofHoursWorked: {
    label: "No. of Hours",
    required: true,
    number: true,
    minValue: 0.01,
    messages: {
      required: "Enter The No. of Hours.",
      number: "Enter A Valid Number Of Hours.",
      minValue: "No. of Hours Should Be Greater Than 0.",
    },
  },
  taskDescriptionRaw: {
    label: "Task Description",
    required: true,
    messages: { required: "Enter The Task Description." },
  },
};
