export const ROUTE_CONSTANTS = Object.freeze({
  COMMON: Object.freeze({
    HOME: "/",
    ERROR: "/error",
  }),

  AUTH: Object.freeze({
    LOGIN: "/login",
    FORGOT_PASSWORD: "/forgot-password",
    CHANGE_PASSWORD: "/change-password",
    RESET_PASSWORD: "/reset-password",
    OTP_AUTHENTICATION: "/otp-authentication",
    USER_PROFILE: "/user-profile",
  }),

  ADMINISTRATION: Object.freeze({
    SECURITY_MASTER: "/securities",
    SECURITY_ACCESS: "/security-access",
    USERS: "/users",
    USER_ADD: "/users/new",
    USER_EDIT: "/users/:userId",
    ROLE_MENU_ACCESS: "/menu-access",
    ROLE_MASTER: "/roles",
  }),

  MASTER: Object.freeze({
    GENDER_MASTER: "/genders",
    COUNTRY_MASTER: "/countries",
    STATE_MASTER: "/states",
    HOLIDAY_MASTER: "/holidays",
  }),

  CONFIGURATION: Object.freeze({
    EMAIL_CONFIGURATION: "/email-configuration",
    EMAIL_CONFIGURATION_ADD: "/email-configuration/new",
    EMAIL_CONFIGURATION_EDIT: "/email-configuration/:emailConfigurationId",
  }),

  MANAGE_EMPLOYEES: Object.freeze({
    EMPLOYEES: "/employees",
    EMPLOYEE_ADD: "/employees/new",
    EMPLOYEE_EDIT: "/employees/:employeeId",
    EMPLOYEE_TYPE: "/employee-types",
  }),

  MANAGE_PROJECTS: Object.freeze({
    PROJECTS: "/projects",
    PROJECT_ADD: "/projects/new",
    PROJECT_EDIT: "/projects/:projectId",
    EMPLOYEE_PROJECT_MAPPING: "/employee-project-mapping",
    EMPLOYEE_PROJECT_MAPPING_ADD: "/employee-project-mapping/new",
    EMPLOYEE_PROJECT_MAPPING_EDIT: "/employee-project-mapping/:mappingId",
    PROJECT_ACTIVITIES: "/project-activities",
  }),

  MANAGE_TIMESHEET: Object.freeze({
    LOG_TIMESHEET: "/log-timesheet",
    LOG_TIMESHEET_ADD: "/log-timesheet/new",
    LOG_TIMESHEET_EDIT: "/log-timesheet/:timesheetId",
    DOWNLOAD_TIMESHEET: "/download-timesheet",
    TIMESHEET_SUBMISSION: "/timesheet-submission",
    TIMESHEET_APPROVAL_REJECTION_LIST: "/timesheet-approval-rejection-list",
    TIMESHEET_APPROVAL_REJECTION: "/timesheet-approval-rejection",
  }),

  LEAVE_MANAGEMENT: Object.freeze({
    LEAVE_TYPES: "/leave-types",
    LEAVE_TYPE_ADD: "/leave-types/new",
    LEAVE_TYPE_EDIT: "/leave-types/:leaveTypeId",
    MY_LEAVE_REQUESTS: "/my-leave-requests",
    APPLY_LEAVE: "/my-leave-requests/new",
    APPLY_LEAVE_EDIT: "/my-leave-requests/:leaveRequestId",
    LEAVE_APPROVAL_REJECTION_LIST: "/leave-approval-rejection-list",
    LEAVE_APPROVAL_REJECTION: "/leave-approval-rejection",
    EMPLOYEE_LEAVE_BALANCE: "/employee-leave-balance",
    EMPLOYEE_LEAVE_BALANCE_ADD: "/employee-leave-balance/new",
    EMPLOYEE_LEAVE_BALANCE_EDIT:
      "/employee-leave-balance/:employeeLeaveBalanceId",
  }),

  TRANSACTIONS: Object.freeze({
    TRANSACTION_HISTORY: "/transaction-history",
  }),

  REPORTS: Object.freeze({
    TIMESHEET_REPORT: "/timesheet-report",
    ALLOCATED_CONSUMED_REPORT: "/allocated-consumed-report",
    LEAVE_REPORT: "/leave-report",
  }),
});
