export const aiAssistantModules = Object.freeze([
  {
    id: "leaveManagement",
    name: "Leave Management",
    icon: "leaveManagement",
    description: "Manage leave requests using AI.",
    enabled: true,
    status: "Available",
  },
  {
    id: "reports",
    name: "Reports",
    icon: "report",
    description: "Generate and export AI reports.",
    enabled: true,
    adminOnly: true,
    status: "Available",
  },
  {
    id: "timesheet",
    name: "Timesheet",
    icon: "timesheet",
    description: "Manage timesheet activities using AI.",
    enabled: false,
    status: "Coming Soon",
  },
]);
