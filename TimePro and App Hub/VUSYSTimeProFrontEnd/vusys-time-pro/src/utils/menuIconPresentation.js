// Shared presentation for Sidebar and Role Menu Access. Permissions and routes remain API-driven.
const normalize = (value) => String(value || "").trim().toLowerCase().replace(/\s+/g, " ");
const presentations = {
  "leave management": { svgIcon: "leaveManagement", tone: "violet" },
  "leave type list": { svgIcon: "leaveType", tone: "violet" },
  "my leave requests": { svgIcon: "myLeaveRequests", tone: "cyan" },
  "apply leave": { svgIcon: "myLeaveRequests", tone: "cyan" },
  "leave approval/rejection list": { svgIcon: "leaveApproval", tone: "rose" },
  "leave approval/rejection": { svgIcon: "leaveApproval", tone: "rose" },
  "employee leave balance list": { svgIcon: "leaveBalance", tone: "emerald" },
  "leave balance": { svgIcon: "leaveBalance", tone: "emerald" },
  "holiday list": { svgIcon: "holidayList", tone: "amber" },
  "holiday master": { svgIcon: "holidayList", tone: "amber" },
};
export const getMenuIconPresentation = (menu = {}) => ({
  svgIcon: presentations[normalize(menu.label || menu.name)]?.svgIcon || menu.svgIcon || "home",
  tone: presentations[normalize(menu.label || menu.name)]?.tone || menu.tone || "indigo",
});
