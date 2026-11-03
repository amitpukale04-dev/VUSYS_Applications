import { APP_CONFIG, buildApiUrl } from "./environmentConfig";

export const baseUrl = APP_CONFIG.apiBaseUrl;
export const Enviroment = APP_CONFIG.environment;
export const VersionNo = APP_CONFIG.versionNo;
export const ContentType = "application/json";
export const MultiPartFormDataContentType = "multipart/form-data";

/**
 * Central API endpoint definitions.
 * Add new TimePro endpoints here and consume them from service modules.
 */
export const auth = Object.freeze({
  login: buildApiUrl("/api/Auth/Login"),
  change_password: buildApiUrl("/api/Auth/ChangePassword"),
  forgot_password: buildApiUrl("/api/Auth/ForgotPasswordRequest"),
  reset_password: buildApiUrl("/api/Auth/ResetPassword"),
  check_for_password_link_available: buildApiUrl(
    "/api/Auth/CheckForPasswordLinkAvailable",
  ),
  logout: buildApiUrl("/api/Auth/Logout"),
  refresh_token: "/api/Auth/RefreshToken",
});

export const frontEndUserTabs = Object.freeze({
  get_user_tab_list: buildApiUrl("/api/FrontEndUserTabs/GetUserTabList"),
  save_and_update_user_tabs: buildApiUrl(
    "/api/FrontEndUserTabs/SaveAndUpdateUserTabs",
  ),
});

export const otp = Object.freeze({
  generate_or_resend_otp: buildApiUrl("/api/OTP/GenerateOrResendOTP"),
});

export const master = Object.freeze({
  save_and_update_user_details: buildApiUrl(
    "/api/Master/SaveAndUpdateUserDetails",
  ),
  get_user_details: buildApiUrl("/api/Master/GetUserDetails"),
  get_user_list: buildApiUrl("/api/Master/GetUserList"),
  get_user_menu_access: buildApiUrl("/api/Master/GetUserMenuAccess"),
  get_dropdown_items: buildApiUrl("/api/Master/GetDropdownItems"),
  get_user_security_access_list: buildApiUrl(
    "/api/Master/GetUserSecurityAccessList",
  ),
  save_update_user_security_access_details: buildApiUrl(
    "/api/Master/SaveUpdateUserSecurityAccessDetails",
  ),
  save_update_role_menu_access_details: buildApiUrl(
    "/api/Master/SaveUpdateRoleMenuAccessDetails",
  ),
  save_and_update_role_details: buildApiUrl(
    "/api/Master/SaveAndUpdateRoleDetails",
  ),
  get_role_details: buildApiUrl("/api/Master/GetRoleDetails"),
  get_role_list: buildApiUrl("/api/Master/GetRoleList"),
  save_and_update_gender_details: buildApiUrl(
    "/api/Master/SaveAndUpdateGenderDetails",
  ),
  get_gender_details: buildApiUrl("/api/Master/GetGenderDetails"),
  get_gender_list: buildApiUrl("/api/Master/GetGenderList"),
  save_and_update_country_details: buildApiUrl(
    "/api/Master/SaveAndUpdateCountryDetails",
  ),
  get_country_details: buildApiUrl("/api/Master/GetCountryDetails"),
  get_country_list: buildApiUrl("/api/Master/GetCountryList"),
  save_and_update_leave_type_details: buildApiUrl(
    "/api/Master/SaveAndUpdateLeaveTypeDetails",
  ),
  get_leave_type_details: buildApiUrl("/api/Master/GetLeaveTypeDetails"),
  get_leave_type_list: buildApiUrl("/api/Master/GetLeaveTypeList"),
  get_holiday_list: buildApiUrl("/api/Master/GetHolidayList"),
  get_holiday_details: buildApiUrl("/api/Master/GetHolidayDetails"),
  save_and_update_holiday_details: buildApiUrl(
    "/api/Master/SaveAndUpdateHolidayDetails",
  ),
  save_and_update_state_details: buildApiUrl(
    "/api/Master/SaveAndUpdateStateDetails",
  ),
  get_state_details: buildApiUrl("/api/Master/GetStateDetails"),
  get_state_list: buildApiUrl("/api/Master/GetStateList"),
  save_update_security_details: buildApiUrl(
    "/api/Master/SaveUpdateSecurityDetails",
  ),
  get_security_details: buildApiUrl("/api/Master/GetSecurityDetails"),
  get_security_list: buildApiUrl("/api/Master/GetSecurityList"),
});

export const employee = Object.freeze({
  save_and_update_employee_details: buildApiUrl(
    "/api/Employee/SaveAndUpdateEmployeeDetails",
  ),
  get_employee_details: buildApiUrl("/api/Employee/GetEmployeeDetails"),
  get_employee_list: buildApiUrl("/api/Employee/GetEmployeeList"),
  save_and_update_employee_type_details: buildApiUrl(
    "/api/Employee/SaveAndUpdateEmployeeTypeDetails",
  ),
  get_employee_type_details: buildApiUrl(
    "/api/Employee/GetEmployeeTypeDetails",
  ),
  get_employee_type_list: buildApiUrl("/api/Employee/GetEmployeeTypeList"),
});

export const project = Object.freeze({
  save_and_update_project_details: buildApiUrl(
    "/api/Project/SaveAndUpdateProjectDetails",
  ),
  get_project_details: buildApiUrl("/api/Project/GetProjectDetails"),
  get_project_list: buildApiUrl("/api/Project/GetProjectList"),
  save_and_update_project_activity_details: buildApiUrl(
    "/api/Project/SaveAndUpdateProjectActivityDetails",
  ),
  get_project_activity_details: buildApiUrl(
    "/api/Project/GetProjectActivityDetails",
  ),
  get_project_activity_list: buildApiUrl("/api/Project/GetProjectActivityList"),
  save_and_update_employee_project_mapping_details: buildApiUrl(
    "/api/Project/SaveAndUpdateEmployeeTypeDetails",
  ),
  get_employee_project_mapping_details: buildApiUrl(
    "/api/Project/GetEmployeeProjectMappingDetails",
  ),
  get_employee_project_mapping_list: buildApiUrl(
    "/api/Project/GetEmployeeProjectMappingList",
  ),
});

export const configuration = Object.freeze({
  save_and_update_email_configuration_details: buildApiUrl(
    "/api/Configuration/SaveAndUpdateEmailConfigurationDetails",
  ),
  get_email_configuration_details: buildApiUrl(
    "/api/Configuration/GetEmailConfigurationDetails",
  ),
  get_email_configuration_list: buildApiUrl(
    "/api/Configuration/GetEmailConfigurationList",
  ),
});

export const leaveManagement = Object.freeze({
  check_leave_request_approval_status: buildApiUrl(
    "/api/LeaveManagement/CheckLeaveRequestApprovalStatus",
  ),
  get_pending_leave_request_list: buildApiUrl(
    "/api/LeaveManagement/GetPendingLeaveRequestList",
  ),
  get_current_year_leave_balance: buildApiUrl(
    "/api/LeaveManagement/GetCurrentYearLeaveBalanceBasedOnLeaveType",
  ),
  save_and_update_leave_request_details: buildApiUrl(
    "/api/LeaveManagement/SaveAndUpdateLeaveRequestDetails",
  ),
  get_leave_request_details: buildApiUrl(
    "/api/LeaveManagement/GetLeaveRequestDetails",
  ),
  get_leave_request_list: buildApiUrl(
    "/api/LeaveManagement/GetLeaveRequestList",
  ),
  approve_reject_cancel_leave_request: buildApiUrl(
    "/api/LeaveManagement/ApproveOrRejectOrCancelLeaveRequest",
  ),
  save_and_update_employee_leave_balance_details: buildApiUrl(
    "/api/LeaveManagement/SaveAndUpdateEmployeeLeaveBalanceDetails",
  ),
  get_employee_leave_balance_details: buildApiUrl(
    "/api/LeaveManagement/GetEmployeeLeaveBalanceDetails",
  ),
  get_employee_leave_balance_list: buildApiUrl(
    "/api/LeaveManagement/GetEmployeeLeaveBalanceList",
  ),
  save_employee_leave_balance_initialize: buildApiUrl(
    "/api/LeaveManagement/SaveEmployeeLeaveBalanceInitialize",
  ),
});

export const report = Object.freeze({
  get_timesheet_report_list: buildApiUrl("/api/Report/GetTimesheetReportList"),
  get_project_allocated_consumed_report_list: buildApiUrl(
    "/api/Report/GetProjectAllocatedConsumedReportList",
  ),
  get_employee_leave_report_list: buildApiUrl(
    "/api/Report/GetEmployeeLeaveReportList",
  ),
});

export const timesheet = Object.freeze({
  send_timesheet_submission_reminder_notification_to_employee: buildApiUrl(
    "/api/Timesheet/SendTimesheetSubmissionReminderNotificationToEmployee",
  ),
  get_or_export_employee_timesheet_list: buildApiUrl(
    "/api/Timesheet/GetOrExportEmployeeTimesheetList",
  ),
  get_timesheet_submission_list: buildApiUrl(
    "/api/Timesheet/GetTimesheetSubmissionList",
  ),
  submit_timesheet_details_for_approval: buildApiUrl(
    "/api/Timesheet/SubmitTimesheetDetailsForApproval",
  ),
  approve_or_reject_timesheet: buildApiUrl(
    "/api/Timesheet/ApproveOrRejectTimesheet",
  ),
  check_timesheet_approval_status: buildApiUrl(
    "/api/Timesheet/CheckTimesheetApprovalStatus",
  ),
  get_log_timesheet_list: buildApiUrl("/api/Timesheet/GetLogTimesheetList"),
  get_timesheet_details: buildApiUrl("/api/Timesheet/GetTimesheetDetails"),
  save_and_update_timesheet_details: buildApiUrl(
    "/api/Timesheet/SaveAndUpdateTimesheetDetails",
  ),
});

export const apiUrl = (path = "") => buildApiUrl(path);

export const ensureBaseUrl = () => {
  if (!baseUrl) {
    throw new Error(
      "API Base URL is not configured. Please set VITE_API_BASE_URL for the active environment and restart the application.",
    );
  }
};

export const ensureEnvironment = () => {
  if (!Enviroment) {
    throw new Error(
      "Environment is not configured. Please set VITE_ENVIRONMENT for the active environment and restart the application.",
    );
  }
};
