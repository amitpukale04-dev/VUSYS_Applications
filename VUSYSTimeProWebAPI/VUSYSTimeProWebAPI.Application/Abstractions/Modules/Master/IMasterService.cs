using System.Data;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Modules.Master
{
    public interface IMasterService
    {
        #region [User Master]

        /// <summary>
        /// This Method Is Used To Save Or Update User Details.
        /// </summary>
        /// <param name="_objUsersM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateUserDetails(UsersM _objUsersM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get User Detail Or List.
        /// </summary>
        /// <param name="_objUserDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetUserDetailOrList(UserDetailsRequestParam _objUserDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [User Master]

        #region [Role Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Role Details.
        /// </summary>
        /// <param name="_objRolesM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateRoleDetails(RolesM _objRolesM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Role Detail Or List.
        /// </summary>
        /// <param name="_objRoleDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetRoleDetailOrList(RoleDetailsRequestParam _objRoleDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Role Master]

        #region [Security Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Security Details.
        /// </summary>
        /// <param name="_objSecurityMasterParameterCommon"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateSecurityDetails(SecurityMasterParameterCommon _objSecurityMasterParameterCommon, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Security Detail Or List.
        /// </summary>
        /// <param name="_objSecurityMasterDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetSecurityDetailOrList(SecurityMasterDetailsRequestParam _objSecurityMasterDetailsRequestParam, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Delete Security Details.
        /// </summary>
        /// <param name="_objSecurityMasterDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> DeleteSecurityDetails(SecurityMasterDetailsRequestParam _objSecurityMasterDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Security Master]

        #region [User Security Access]

        /// <summary>
        /// This Method Is Used To Save Or Update User Security Access Details.
        /// </summary>
        /// <param name="_lstUserSecurityAccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateUserSecurityAccessDetails(List<UserSecurityAccess> _lstUserSecurityAccess, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get User Security Access List.
        /// </summary>
        /// <param name="_objUserSecurityAccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetUserSecurityAccessList(UserSecurityAccess _objUserSecurityAccess, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Delete User Security Access Details.
        /// </summary>
        /// <param name="_objUserSecurityAccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> DeleteUserSecurityAccessDetails(UserSecurityAccess _objUserSecurityAccess, CancellationToken cancellationToken = default);

        #endregion [User Security Access]

        #region [User Menu Access]

        /// <summary>
        /// This Method Is Used To Get User Menu Access.
        /// </summary>
        /// <param name="_objUserMenuAccessRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetUserMenuAccess(UserMenuAccessRequestParam _objUserMenuAccessRequestParam, CancellationToken cancellationToken = default);

        #endregion [User Menu Access]

        #region [Role Menu Access]

        /// <summary>
        /// This Method Is Used To Save Or Update Role Menu Access Details.
        /// </summary>
        /// <param name="_lstRoleMenuAccessFrontEnd"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateRoleMenuAccessDetails(List<RoleMenuAccessFrontEnd> _lstRoleMenuAccessFrontEnd, CancellationToken cancellationToken = default);

        #endregion [Role Menu Access]

        #region [Dropdown Items]

        /// <summary>
        /// This Method Is Used To Get Dropdown Items.
        /// </summary>
        /// <param name="_objDropdownRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetDropdownItems(DropdownRequestParam _objDropdownRequestParam, CancellationToken cancellationToken = default);

        #endregion [Dropdown Items]

        #region [Gender Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Gender Details.
        /// </summary>
        /// <param name="_objGenderM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateGenderDetails(GenderM _objGenderM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Gender Detail Or List.
        /// </summary>
        /// <param name="_objGenderDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetGenderDetailOrList(GenderDetailsRequestParam _objGenderDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Gender Master]

        #region [Country Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Country Details.
        /// </summary>
        /// <param name="_objCountryM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateCountryDetails(CountryM _objCountryM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Country Detail Or List.
        /// </summary>
        /// <param name="_objCountryDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetCountryDetailOrList(CountryDetailsRequestParam _objCountryDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Country Master]

        #region [State Master]

        /// <summary>
        /// This Method Is Used To Save Or Update State Details.
        /// </summary>
        /// <param name="_objStateM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateStateDetails(StateM _objStateM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get State Detail Or List.
        /// </summary>
        /// <param name="_objStateDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetStateDetailOrList(StateDetailsRequestParam _objStateDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [State Master]

        #region [Holiday Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Holiday Details.
        /// </summary>
        /// <param name="_objHolidayM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateHolidayDetails(HolidayM _objHolidayM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Holiday Detail Or List.
        /// </summary>
        /// <param name="_objHolidayDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetHolidayDetailOrList(HolidayDetailsRequestParam _objHolidayDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Holiday Master]

        #region [Leave Type Master]

        /// <summary>
        /// This Method Is Used To Save Or Update Leave Type Details.
        /// </summary>
        /// <param name="_objLeaveTypeM"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> SaveUpdateLeaveTypeDetails(LeaveTypeM _objLeaveTypeM, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Leave Type Detail Or List.
        /// </summary>
        /// <param name="_objLeaveTypeDetailsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataSet> GetLeaveTypeDetailOrList(LeaveTypeDetailsRequestParam _objLeaveTypeDetailsRequestParam, CancellationToken cancellationToken = default);

        #endregion [Leave Type Master]
    }
}