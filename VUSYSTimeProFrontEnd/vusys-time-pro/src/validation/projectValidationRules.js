export const projectValidationRules = {
  projectCode: {
    label: "Project Code",
    required: true,
    maxLength: 100,
  },
  clientName: {
    label: "Client Name",
    required: true,
    maxLength: 200,
  },
  projectName: {
    label: "Project Name",
    required: true,
    maxLength: 200,
  },
  projectDescription: {
    label: "Project Description",
    required: true,
    maxLength: 2000,
  },
  startDate: {
    label: "Start Date",
    required: true,
    date: true,
    messages: { required: "Select The Start Date." },
  },
  endDate: {
    label: "End Date",
    required: true,
    date: true,
    custom: (value, values) => {
      if (!value || !values?.startDate) return true;
      return String(value) >= String(values.startDate);
    },
    customMessage: "End Date Should Be Greater Than Or Equal to Start Date.",
    messages: { required: "Select The End Date." },
  },
  allocatedHours: {
    label: "Allocated Hours",
    required: true,
    number: true,
    minValue: 0,
  },
  consumedHours: {
    label: "Consumed Hours",
    required: true,
    number: true,
    minValue: 0,
  },
  projectStatus: {
    label: "Project Status",
    required: true,
    messages: { required: "Select The Project Status." },
  },
};
