using VUSYSTimeProWebApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using ForgotPassword = VUSYSTimeProWebApp.Models.ForgotPassword;

namespace VUSYSTimeProWebApp.ViewModels
{
    public class AllViewModel
    {
		/// <summary>
		/// User Login
		/// </summary>
		public UserLogin UserLogin { get; set; }

        /// <summary>
        /// OTP Authentication
        /// </summary>
        public OTPAuthentication otpAuth { get; set; }

        /// <summary>
		/// Change Password
		/// </summary>
		public ChangePassword changePassword { get; set; }

        /// <summary>
        /// Change Password On Reset
        /// </summary>
        public ChangePasswordOnReset changePasswordOnReset { get; set; }

        /// <summary>
        /// Forgot Password
        /// </summary>
        public ForgotPassword forgotPassword { get; set; }

        /// <summary>
        /// Users
        /// </summary>
        public UsersM Users { get; set; }

        /// <summary>
        /// Employee Master
        /// </summary>
        public EmployeeM EmployeeMaster { get; set; }

        /// <summary>
        /// Employee Project Mapping
        /// </summary>
        public EmployeeProjectMappingM employeeProjectMapping { get; set; }

        /// <summary>
        /// Employee Type
        /// </summary>
        public EmployeeTypeM employeeTypeMaster { get; set; }

        /// <summary>
        /// Project Time Sheet
        /// </summary>
        public ProjectTimesheet projectTimesheet { get; set; }

        /// <summary>
        /// Project Time Sheet Filter
        /// </summary>
        public ProjectTimesheetFilter projectTimesheetFilter { get; set; }

        /// <summary>
        /// Project Time Sheet Submission Filter
        /// </summary>
        public ProjectTimesheetSubmissionFilter projectTimesheetSubmissionFilter { get; set; }

        /// <summary>
        /// Project Time Sheet Report Filter
        /// </summary>
        public ProjectTimesheetReportFilter projectTimesheetReportFilter { get; set; }

        /// <summary>
        /// Leave Report Filter
        /// </summary>
        public LeaveReportFilter leaveReportFilter { get; set; }

        /// <summary>
        /// Project Time Sheet Approve Reject Filter
        /// </summary>
        public ProjectTimesheetApproveRejectFilter projectTimesheetApproveRejectFilter { get; set; }

        /// <summary>
        /// Project Time Sheet Approve Rejection
        /// </summary>
        public ProjectTimesheetApproveRejection projectTimesheetApproveRejection { get; set; }

        /// <summary>
        /// Download Timesheet Filter
        /// </summary>
        public DownloadTimesheetFilter downloadTimesheetFilter { get; set; }

        /// <summary>
        /// Project Allocated Consumed Report Filter
        /// </summary>
        public ProjectAllocatedConsumedReportFilter projectAllocatedConsumedReportFilter { get; set; }

        /// <summary>
        /// Menu Access Role
        /// </summary>
        public MenuAccessRole menuAccessRole { get; set; }

        /// <summary>
        /// Main Menu
        /// </summary>
        public MainMenu mainMenu { get; set; }

        /// <summary>
        /// Menu List
        /// </summary>
        public List<MainMenu> MenuList { get; set; }

        /// <summary>
        /// Role Master
        /// </summary>
        public RolesM RoleMaster { get; set; }

        /// <summary>
        /// Project Master
        /// </summary>
        public ProjectM ProjectMaster { get; set; }

        /// <summary>
        /// Project Activity
        /// </summary>
        public ProjectActivityM ProjectActivity { get; set; }

        /// <summary>
        /// Gender Master
        /// </summary>
        public GenderM GenderMaster { get; set; }

		/// <summary>
		/// Main Menu Master
		/// </summary>
		public MainMenuMasterM MainMenuMaster { get; set; }

		/// <summary>
		/// Sub Menu Master
		/// </summary>
		public SubMenuMasterM SubMenuMaster { get; set; }

		/// <summary>
		/// User Profile Image
		/// </summary>
		public UserProfileImage userProfileImage { get; set; }

        /// <summary>
        /// Role List
        /// </summary>
        public List<SelectListItem> RoleList { get; set; }

        /// <summary>
		/// Country Master
		/// </summary>
		public CountryM CountryMaster { get; set; }

        /// <summary>
		/// State Master
		/// </summary>
		public StateM StateMaster { get; set; }

		/// <summary>
		/// Email Configuration Master
		/// </summary>
		public ConfigureEmailM EmailConfigurationMaster { get; set; }

        /// <summary>
		/// Common Error
		/// </summary>
		public CommonError commonError { get; set; }
    }
}
