namespace VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants
{
    public static class StoredProcedure
    {
        #region [Auth API]

        /// <summary>
        /// USER_LOGIN
        /// </summary>
        public static string USER_LOGIN = "usp_VUSYS_User_Login";

        /// <summary>
        /// USER_LAST_LOGIN_LOGOUT
        /// </summary>
        public static string USER_LAST_LOGIN_LOGOUT = "usp_VUSYS_UserLastLoginLogout";

        /// <summary>
        /// CHANGE_PASSWORD
        /// </summary>
        public static string CHANGE_PASSWORD = "usp_VUSYS_User_ChangePassword";

        #endregion [Auth API]

        #region [Master API]

        /// <summary>
        /// USER_MASTER
        /// </summary>
        public static string USER_MASTER = "usp_VUSYS_SaveUpdateUserDetails";

        /// <summary>
        /// USER_DETAIL_LIST
        /// </summary>
        public static string USER_DETAIL_LIST = "usp_VUSYS_GetUsersList";

        /// <summary>
        /// ROLE_MASTER
        /// </summary>
        public static string ROLE_MASTER = "usp_VUSYS_SaveUpdateRoleDetails";

        /// <summary>
        /// ROLE_DETAIL_LIST
        /// </summary>
        public static string ROLE_DETAIL_LIST = "usp_VUSYS_GetRoleList";

        /// <summary>
        /// GENDER_MASTER
        /// </summary>
        public static string GENDER_MASTER = "usp_VUSYS_SaveUpdateGenderDetails";

        /// <summary>
        /// GENDER_DETAIL_LIST
        /// </summary>
        public static string GENDER_DETAIL_LIST = "usp_VUSYS_GetGenderList";

        /// <summary>
        /// COUNTRY_MASTER
        /// </summary>
        public static string COUNTRY_MASTER = "usp_VUSYS_SaveUpdateCountryDetails";

        /// <summary>
        /// COUNTRY_DETAIL_LIST
        /// </summary>
        public static string COUNTRY_DETAIL_LIST = "usp_VUSYS_GetCountryList";

        /// <summary>
        /// STATE_MASTER
        /// </summary>
        public static string STATE_MASTER = "usp_VUSYS_SaveUpdateStateDetails";

        /// <summary>
        /// STATE_DETAIL_LIST
        /// </summary>
        public static string STATE_DETAIL_LIST = "usp_VUSYS_GetStateList";

        /// <summary>
        /// HOLIDAY_MASTER
        /// </summary>
        public static string HOLIDAY_MASTER = "usp_VUSYS_SaveUpdateHolidayDetails";

        /// <summary>
        /// HOLIDAY_DETAIL_LIST
        /// </summary>
        public static string HOLIDAY_DETAIL_LIST = "usp_VUSYS_GetHolidayList";

        /// <summary>
        /// LEAVE_TYPE_MASTER
        /// </summary>
        public static string LEAVE_TYPE_MASTER = "usp_VUSYS_SaveUpdateLeaveTypeDetails";

        /// <summary>
        /// LEAVE_TYPE_DETAIL_LIST
        /// </summary>
        public static string LEAVE_TYPE_DETAIL_LIST = "usp_VUSYS_GetLeaveTypeList";

        /// <summary>
        /// SECURITY_MASTER
        /// </summary>
        public static string SECURITY_MASTER = "usp_VUSYS_SaveUpdateSecurityMasterDetails";

        /// <summary>
        /// SECURITY_DETAIL_LIST
        /// </summary>
        public static string SECURITY_DETAIL_LIST = "usp_VUSYS_GetUserSecurityMasterList";

        /// <summary>
        /// USER_SECURITY_ACCESS
        /// </summary>
        public static string USER_SECURITY_ACCESS = "usp_VUSYS_UserSecurityAccess_Insert";

        /// <summary>
        /// USER_SECURITY_ACCESS_DETAIL_LIST
        /// </summary>
        public static string USER_SECURITY_ACCESS_DETAIL_LIST = "usp_VUSYS_GetUserSecurityAccessList";

        /// <summary>
        /// USER_MENU_ACCESS
        /// </summary>
        public static string USER_MENU_ACCESS = "usp_VUSYS_MenuAccess_FrontEnd";

        /// <summary>
        /// ROLE_MENU_ACCESS_INSERT
        /// </summary>
        public static string ROLE_MENU_ACCESS_INSERT = "usp_VUSYS_RoleMenuAccess_FrontEnd_Insert";

        /// <summary>
        /// DROPDOWN_ITEMS
        /// </summary>
        public static string DROPDOWN_ITEMS = "usp_VUSYS_Dropdown_Items";

        #endregion [Master API]

        #region [Configuration API]

        /// <summary>
        /// EMAIL_CONFIGURATION
        /// </summary>
        public static string EMAIL_CONFIGURATION = "usp_VUSYS_Save_UpdateEmailConfigurationDetails";

        /// <summary>
        /// EMAIL_CONFIGURATION_DETAIL_LIST
        /// </summary>
        public static string EMAIL_CONFIGURATION_DETAIL_LIST = "usp_VUSYS_GetEmailConfigurationList";

        #endregion [Configuration API]

        #region [Manage Employee API]

        /// <summary>
        /// EMPLOYEE_MASTER
        /// </summary>
        public static string EMPLOYEE_MASTER = "usp_VUSYS_SaveUpdateEmployeeDetails";

        /// <summary>
        /// EMPLOYEE_DETAIL_LIST
        /// </summary>
        public static string EMPLOYEE_DETAIL_LIST = "usp_VUSYS_GetEmployeeList";

        #endregion [Manage Employee API]

        #region [Manage Project API]

        /// <summary>
        /// PROJECT_MASTER
        /// </summary>
        public static string PROJECT_MASTER = "usp_VUSYS_SaveUpdateProjectDetails";

        /// <summary>
        /// PROJECT_DETAIL_LIST
        /// </summary>
        public static string PROJECT_DETAIL_LIST = "usp_VUSYS_GetProjectList";

        /// <summary>
        /// PROJECT_ACTIVITY
        /// </summary>
        public static string PROJECT_ACTIVITY = "usp_VUSYS_SaveUpdateProjectActivityDetails";

        /// <summary>
        /// PROJECT_ACTIVITY_DETAIL_LIST
        /// </summary>
        public static string PROJECT_ACTIVITY_DETAIL_LIST = "usp_VUSYS_GetProjectActivityList";

        #endregion [Manage Project API]

        #region [Timesheet API]

        /// <summary>
        /// SAVE_TIMESHEET
        /// </summary>
        public static string SAVE_TIMESHEET = "usp_VUSYS_SaveUpdateProjectTimeSheetDetails";

        /// <summary>
        /// GET_TIMESHEET_NOT_FILLED
        /// </summary>
        public static string GET_TIMESHEET_NOT_FILLED = "usp_VUSYS_GetEmployeesTimesheetNotFilled";

        /// <summary>
        /// GET_EMPLOYEE_PROJECT_LOG_TIMESHEET
        /// </summary>
        public static string GET_EMPLOYEE_PROJECT_LOG_TIMESHEET = "usp_VUSYS_GetEmployeeProjectTimesheet";

        /// <summary>
        /// EMPLOYEE_PROJECT_TIMESHEET_SUBMISSION
        /// </summary>
        public static string EMPLOYEE_PROJECT_TIMESHEET_SUBMISSION = "usp_VUSYS_EmployeeProjectTimesheetForSubmission";

        /// <summary>
        /// EMPLOYEE_TIMESHEET_APPROVE_REJECT
        /// </summary>
        public static string EMPLOYEE_TIMESHEET_APPROVE_REJECT = "usp_VUSYS_EmployeeProjectTimesheetApproveReject";

        /// <summary>
        /// SYNC_EMPLOYEE_LEAVE_HOLIDAY_TO_PROJECT_TIMESHEET
        /// </summary>
        public const string SYNC_EMPLOYEE_LEAVE_HOLIDAY_TO_PROJECT_TIMESHEET = "usp_VUSYS_SyncEmployeeLeaveAndHolidayToProjectTimeSheetDetails";

        #endregion [Timesheet API]

        #region [Leave Management API]

        /// <summary>
        /// EMPLOYEE_LEAVE_BALANCE_SAVE_UPDATE
        /// </summary>
        public static string EMPLOYEE_LEAVE_BALANCE_SAVE_UPDATE = "usp_VUSYS_SaveUpdateEmployeeLeaveBalanceDetails";

        /// <summary>
        /// EMPLOYEE_LEAVE_BALANCE_DETAIL_LIST
        /// </summary>
        public static string EMPLOYEE_LEAVE_BALANCE_DETAIL_LIST = "usp_VUSYS_GetEmployeeLeaveBalanceList";

        /// <summary>
        /// EMPLOYEE_LEAVE_BALANCE_INITIALIZE
        /// </summary>
        public static string EMPLOYEE_LEAVE_BALANCE_INITIALIZE = "usp_VUSYS_EmployeeLeaveBalance_Initialize";

        /// <summary>
        /// LEAVE_APPLICATION_SAVE_UPDATE
        /// </summary>
        public const string LEAVE_APPLICATION_SAVE_UPDATE = "usp_VUSYS_SaveUpdateLeaveApplicationDetails";

        /// <summary>
        /// GET_TOTAL_NO_OF_LEAVE_DAYS
        /// </summary>
        public const string GET_TOTAL_NO_OF_LEAVE_DAYS = "usp_VUSYS_GetTotalNoofLeaveDays";

        /// <summary>
        /// LEAVE_APPLICATION_DETAIL_LIST
        /// </summary>
        public const string LEAVE_APPLICATION_DETAIL_LIST = "usp_VUSYS_GetLeaveApplicationList";

        /// <summary>
        /// LEAVE_APPLICATION_APPROVE_REJECT_CANCEL
        /// </summary>
        public const string LEAVE_APPLICATION_APPROVE_REJECT_CANCEL = "usp_VUSYS_LeaveApplication_ApproveRejectCancel";

        #endregion [Leave Management API]

        #region [Report API]

        /// <summary>
        /// PROJECT_ALLOCATED_CONSUMED
        /// </summary>
        public static string PROJECT_ALLOCATED_CONSUMED = "usp_VUSYS_GetProjectAllocatedConsumedHoursReport";

        #endregion [Report API]

    }
}
