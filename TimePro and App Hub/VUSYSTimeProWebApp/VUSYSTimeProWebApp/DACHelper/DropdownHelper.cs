using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Rendering;
using VUSYSTimeProWebApp.Models;
using CommonLib;
using DocumentFormat.OpenXml.InkML;
using CommonLib.Constants;

namespace VUSYSTimeProWebApp.DACHelper
{
    public static class DropdownHelper
    {
        #region [DropDown List]

        /// <summary>
        /// This Method Is Used To Get Role List.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SelectList FillRoleList(AppDbContext db)
        {
            SelectList roleItems = null;
            try
            {
                List<SelectListItem> roleList = (from c in db.RoleList
                                                 where c.IsDeleted == false
                                                 orderby c.RoleName ascending
                                                 select new SelectListItem() { Text = c.RoleName, Value = Convert.ToString(c.Id) }).ToList();
                roleList.Insert(0, new SelectListItem()
                {
                    Text = "SELECT",
                    Value = ""
                });
                roleItems = new SelectList(roleList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillRoleList()", ex.Message, false);
                throw;
            }
            return roleItems;
        }

        /// <summary>
        /// This Method Is Used To Get Gender Master List.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SelectList FillGenderMasterList(AppDbContext db)
        {
            SelectList genderItems = null;
            try
            {
                List<SelectListItem> genderList = (from c in db.GenderMasterList
                                                   where c.IsDeleted == false
                                                   orderby c.Gender ascending
                                                   select new SelectListItem() { Text = c.Gender, Value = Convert.ToString(c.Id) }).ToList();
                genderList.Insert(0, new SelectListItem()
                {
                    Text = "SELECT",
                    Value = ""
                });
                genderItems = new SelectList(genderList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillGenderMasterList()", ex.Message, false);
                throw;
            }
            return genderItems;
        }

        /// <summary>
        /// This Method Is Used To Get Employee Type Master List.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SelectList FillEmployeeTypeMasterList(AppDbContext db)
        {
            SelectList employeeTypeItems = null;
            try
            {
                List<SelectListItem> employeeTypeList = (from c in db.EmployeeTypeMasterList
                                                   where c.IsDeleted == false
                                                   orderby c.EmployeeType ascending
                                                   select new SelectListItem() { Text = c.EmployeeType, Value = Convert.ToString(c.Id) }).ToList();
                employeeTypeList.Insert(0, new SelectListItem()
                {
                    Text = "SELECT",
                    Value = ""
                });
                employeeTypeItems = new SelectList(employeeTypeList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillEmployeeTypeMasterList()", ex.Message, false);
                throw;
            }
            return employeeTypeItems;
        }

        /// <summary>
        /// This Method Is Used To Get Employee Type Data.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="protector"></param>
        /// <param name="EmployeeTypeID"></param>
        /// <returns></returns>
        public static List<EmployeeTypeDataList> GetEmployeeTypeData(AppDbContext db, IDataProtector protector, int EmployeeTypeID = 0)
        {
            List<EmployeeTypeDataList> employeeTypeList = new List<EmployeeTypeDataList>();
            try
            {
                if (EmployeeTypeID > 0)
                {
                    var query = from employeeType in db.EmployeeTypeMasterList
                                where employeeType.Id == EmployeeTypeID
                                select new
                                {
                                    Id = employeeType.Id,
                                    EncryptedId = protector.Protect(employeeType.Id.ToString()),
                                    EmployeeType = employeeType.EmployeeType,
                                    IsDeleted = employeeType.IsDeleted,
                                    ActiveStatus = employeeType.IsDeleted == false ? "Active" : "Inactive",
                                };

                    employeeTypeList = query.AsEnumerable()
                        .Select((item, index) => new EmployeeTypeDataList
                        {
                            SLNo = (Int64)index + 1, // index starts from 0, so add 1 to make it 1-based
                            Id = item.Id,
                            EncryptedId = item.EncryptedId,
                            EmployeeType = item.EmployeeType,
                            IsDeleted = item.IsDeleted,
                            ActiveStatus = item.ActiveStatus
                        }).ToList();
                }
                else
                {
                    var query = from employeeType in db.EmployeeTypeMasterList
                                select new
                                {
                                    Id = employeeType.Id,
                                    EncryptedId = protector.Protect(employeeType.Id.ToString()),
                                    EmployeeType = employeeType.EmployeeType,
                                    IsDeleted = employeeType.IsDeleted,
                                    ActiveStatus = employeeType.IsDeleted == false ? "Active" : "Inactive",
                                };

                    employeeTypeList = query.AsEnumerable()
                        .Select((item, index) => new EmployeeTypeDataList
                        {
                            SLNo = (Int64)index + 1, // index starts from 0, so add 1 to make it 1-based
                            Id = item.Id,
                            EncryptedId = item.EncryptedId,
                            EmployeeType = item.EmployeeType,
                            IsDeleted = item.IsDeleted,
                            ActiveStatus = item.ActiveStatus
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetEmployeeTypeData()", ex.Message, false);
                throw;
            }
            return employeeTypeList;
        }

        /// <summary>
        /// This Method Is Used To Get Country Master List.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SelectList FillCountryMasterList(AppDbContext db)
        {
            SelectList countryMasterItems = null;
            try
            {
                List<SelectListItem> countryMasterList = (from c in db.CountryMasterList
                                                          where c.IsDeleted == false
                                                          orderby c.CountryName ascending
                                                          select new SelectListItem() { Text = c.CountryName, Value = Convert.ToString(c.Id) }).ToList();
                countryMasterList.Insert(0, new SelectListItem()
                {
                    Text = "SELECT",
                    Value = ""
                });
                countryMasterItems = new SelectList(countryMasterList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillCountryMasterList()", ex.Message, false);
                throw;
            }
            return countryMasterItems;
        }

        /// <summary>
        /// This Method Is Used To Get State Master List.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="CountryID"></param>
        /// <returns></returns>
        public static SelectList FillStateMasterList(AppDbContext db, int CountryID)
        {
            SelectList stateMasterItems = null;
            try
            {
                List<SelectListItem> stateMasterList = (from c in db.StateMasterList
                                                        where c.CountryID == CountryID && c.IsDeleted == false
                                                        orderby c.StateName ascending
                                                        select new SelectListItem() { Text = c.StateName, Value = Convert.ToString(c.Id) }).ToList();
                stateMasterList.Insert(0, new SelectListItem()
                {
                    Text = "SELECT",
                    Value = ""
                });
                stateMasterItems = new SelectList(stateMasterList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillStateMasterList()", ex.Message, false);
                throw;
            }
            return stateMasterItems;
        }

        /// <summary>
        /// This Method Is Used To Get Employee List.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SelectList FillEmployeeList(AppDbContext db)
        {
            SelectList employeeItems = null;
            try
            {
                List<SelectListItem> employeeList = (from c in db.EmployeeList
                                                 where c.IsDeleted == false
                                                 orderby c.FullName ascending
                                                 select new SelectListItem() { Text = c.FullName, Value = Convert.ToString(c.EmployeeCode) }).ToList();
                employeeList.Insert(0, new SelectListItem()
                {
                    Text = "SELECT",
                    Value = ""
                });
                employeeItems = new SelectList(employeeList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillEmployeeList()", ex.Message, false);
                throw;
            }
            return employeeItems;
        }

        /// <summary>
        /// This Method Is Used To Fill Project List.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SelectList FillProjectList(AppDbContext db)
        {
            SelectList projectItems = null;
            try
            {
                List<SelectListItem> projectList = (from c in db.ProjectList
                                                    where c.IsDeleted == false
                                                     orderby c.ProjectName ascending
                                                     select new SelectListItem() { Text = c.ProjectName, Value = Convert.ToString(c.Id) }).ToList();
                projectList.Insert(0, new SelectListItem()
                {
                    Text = "SELECT",
                    Value = ""
                });
                projectItems = new SelectList(projectList, "Value", "Text");
            }
            catch(Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillProjectList()", ex.Message, false);
                throw;
            }
            return projectItems;
        }

        /// <summary>
        /// This Method Is Used To Fill Project List.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SelectList FillProjectListReport(AppDbContext db)
        {
            SelectList projectItems = null;
            try
            {
                List<SelectListItem> projectList = (from c in db.ProjectList
                                                    where c.ProjectCode != "VUSYS_TIMELYAWAY" && c.IsDeleted == false
                                                    orderby c.ProjectName ascending
                                                    select new SelectListItem() { Text = c.ProjectName, Value = Convert.ToString(c.Id) }).ToList();
                projectList.Insert(0, new SelectListItem()
                {
                    Text = "ALL",
                    Value = "0"
                });
                projectItems = new SelectList(projectList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillProjectList()", ex.Message, false);
                throw;
            }
            return projectItems;
        }

        /// <summary>
        /// This Method Is Used To Get Project Activity List.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SelectList FillProjectActivityList(AppDbContext db)
        {
            SelectList projectActivityItems = null;
            try
            {
                List<SelectListItem> projectActivityList = (from c in db.ProjectActivityList
                                                   where c.IsDeleted == false
                                                   orderby c.ActivityName ascending
                                                   select new SelectListItem() { Text = c.ActivityName, Value = Convert.ToString(c.Id) }).ToList();
                projectActivityList.Insert(0, new SelectListItem()
                {
                    Text = "SELECT",
                    Value = ""
                });
                projectActivityItems = new SelectList(projectActivityList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillProjectActivityList()", ex.Message, false);
                throw;
            }
            return projectActivityItems;
        }

        /// <summary>
        /// This Method Is Used To Get Activity Name By Project Activity ID.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="ProjectActivityID"></param>
        /// <returns></returns>
        public static string GetProjectActivityNameByProjectActivityID(AppDbContext db, int ProjectActivityID)
        {
            string ProjectActivityName = string.Empty;
            try
            {
                var projectActivityList = (from c in db.ProjectActivityList
                                   where c.Id == ProjectActivityID && c.IsDeleted == false
                                   orderby c.ActivityName ascending
                                   select new { c.ActivityName }).ToList();

                if (projectActivityList != null && projectActivityList.Count > 0)
                {
                    ProjectActivityName = projectActivityList[0].ActivityName;
                }
            }
            catch (Exception ex)
            {
                ProjectActivityName = string.Empty;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetProjectActivityNameByProjectActivityID()", ex.Message, false);
            }
            return ProjectActivityName;
        }

        /// <summary>
        /// This Method Is Used To Get Project Leave Activity Name.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static List<ProjectActivityM> GetProjectLeaveActivityName(AppDbContext db, bool IsOnLeave)
        {
            List<ProjectActivityM> lstProjectActivityM = new List<ProjectActivityM>();
            try
            {
                if(IsOnLeave)
                {
                    var query = db.ProjectActivityList.Where(pa => new[] { "ON LEAVE", "HOLIDAY", "COMPOFF" }.Contains(pa.ActivityName))
                        .Select(pa => new
                        {
                            Id = pa.Id,
                            ActivityName = pa.ActivityName
                        }).ToList();

                    lstProjectActivityM = query.AsEnumerable()
                        .Select((item, index) => new ProjectActivityM
                        {
                            Id = item.Id,
                            ActivityName = item.ActivityName
                        }).ToList();
                }
                else
                {
                    var query = db.ProjectActivityList.Where(pa => !new[] { "ON LEAVE", "HOLIDAY", "COMPOFF" }.Contains(pa.ActivityName))
                        .Select(pa => new
                        {
                            Id = pa.Id,
                            ActivityName = pa.ActivityName
                        }).ToList();

                    lstProjectActivityM = query.AsEnumerable()
                        .Select((item, index) => new ProjectActivityM
                        {
                            Id = item.Id,
                            ActivityName = item.ActivityName
                        }).ToList();
                }
                //if (projectActivityList != null && projectActivityList.Count > 0)
                //{
                //    //ProjectLeaveActivityID = projectActivityList[0].Id;
                //    lstProjectActivityM = projectActivityList.ToList();
                //}
            }
            catch (Exception ex)
            {
                lstProjectActivityM = null;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetProjectActivityNameByProjectActivityID()", ex.Message, false);
            }
            return lstProjectActivityM;
        }

        /// <summary>
        /// This Method Is Used To Get Project Name By Project ID.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="ProjectID"></param>
        /// <returns></returns>
        public static string GetProjectNameByProjectID(AppDbContext db, int ProjectID)
        {
            string ProjectName = string.Empty;
            try
            {
                var projectList = (from c in db.ProjectList
                                                    where c.Id == ProjectID && c.IsDeleted == false
                                                    orderby c.ProjectName ascending
                                                    select new { c.ProjectName}).ToList();

                if (projectList != null && projectList.Count > 0)
                {
                    ProjectName = projectList[0].ProjectName;
                }
            }
            catch (Exception ex)
            {
                ProjectName = string.Empty;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetProjectNameByProjectID()", ex.Message, false);
            }
            return ProjectName;
        }

        /// <summary>
        /// This Method Is Used To Check Timesheet Sent For Approval.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="EmployeeCode"></param>
        /// <returns></returns>
        public static bool CheckTimesheetSentForApproval(AppDbContext db, string EmployeeCode, DateTime? TaskDate = null)
        {
            bool IsSentForApproval = false;
            try
            {
                DateTime effectiveDate = TaskDate ?? DateTime.Now;
                string TimesheetMonth = DateTimeHelper.GetMonthName(effectiveDate.Month);
                int TimesheetYear = effectiveDate.Year;
                IsSentForApproval = db.ProjectTimesheetList.Where(c => c.EmployeeCode == EmployeeCode && c.TimesheetMonth == TimesheetMonth && c.TimesheetYear == TimesheetYear && !c.IsDeleted && c.IsSentForApproval).Select(c => (c.IsApproved == true) || (c.IsApproved == false && c.ApprovedOrRejectedReason == null)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                IsSentForApproval = false;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "DropdownHelper.CheckTimesheetSentForApproval()", ex.Message, false);
            }
            return IsSentForApproval;
        }

        /// <summary>
        /// This Method Is Used To Check Timesheet Approved or Not.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="EmployeeCode"></param>
        /// <returns></returns>
        public static bool CheckTimesheetApproved(AppDbContext db, string EmployeeCode, DateTime? TaskDate = null)
        {
            bool IsApproved = false;
            try
            {
                DateTime effectiveDate = TaskDate ?? DateTime.Now;
                string TimesheetMonth = DateTimeHelper.GetMonthName(effectiveDate.Month);
                int TimesheetYear = effectiveDate.Year;
                IsApproved = db.ProjectTimesheetList.Where(c => c.EmployeeCode == EmployeeCode && c.TimesheetMonth == TimesheetMonth && c.TimesheetYear == TimesheetYear && !c.IsDeleted).Select(c => (bool)c.IsApproved).FirstOrDefault();
            }
            catch (Exception ex)
            {
                IsApproved = false;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "DropdownHelper.CheckTimesheetApproved()", ex.Message, false);
            }
            return IsApproved;
        }

        /// <summary>
        /// This Method Is Used To Check Current Month Timesheet Closed Status.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="EmployeeCode"></param>
        /// <returns></returns>
        public static bool CheckCurrentMonthTimesheetClosedStatus(AppDbContext db, string EmployeeCode)
        {
            bool IsClosed = false;
            try
            {
                string TimesheetMonth = DateTimeHelper.GetMonthName(DateTime.Now.Month);
                int TimesheetYear = DateTime.Now.Year;
                IsClosed = db.ProjectTimesheetList.Where(c => c.EmployeeCode == EmployeeCode && c.TimesheetMonth == TimesheetMonth && c.TimesheetYear == TimesheetYear && !c.IsDeleted).Select(c => (bool)c.IsTimesheetClosed).FirstOrDefault();
            }
            catch (Exception ex)
            {
                IsClosed = false;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetCurrentMonthTimesheetClosedStatus()", ex.Message, false);
            }
            return IsClosed;
        }

        /// <summary>
        /// This Method Is Used To Get Project Status List.
        /// </summary>
        /// <returns></returns>
        public static SelectList FillProjectStatusList()
        {
            SelectList projectStatusItems = null;
            try
            {
                var ProjectStatusList = Enum.GetValues(typeof(ProjectStatus)).Cast<ProjectStatus>().Select(e => new SelectListItem
                {
                    Text = e.ToString().Replace("_", " "),
                    Value = ((int)e).ToString()
                }).ToList();

                ProjectStatusList.Insert(0, new SelectListItem
                {
                    Text = "SELECT",
                    Value = ""
                });
                projectStatusItems = new SelectList(ProjectStatusList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillProjectStatusList()", ex.Message, false);
                throw;
            }
            return projectStatusItems;
        }

        /// <summary>
        /// This Method Is Used To Get Project Timesheet Approval Status List.
        /// </summary>
        /// <returns></returns>
        public static SelectList FillProjectTimesheetApprovalStatusList()
        {
            SelectList projectTimesheetApprovalStatusItems = null;
            try
            {
                var ProjectTimesheetApprovalStatusList = Enum.GetValues(typeof(ProjectTimesheetApprovalStatus)).Cast<ProjectTimesheetApprovalStatus>().Select(e => new SelectListItem
                {
                    Text = e.ToString().Replace("_", " "),
                    Value = ((int)e).ToString()
                }).ToList();

                ProjectTimesheetApprovalStatusList.Insert(0, new SelectListItem
                {
                    Text = "SELECT",
                    Value = ""
                });
                projectTimesheetApprovalStatusItems = new SelectList(ProjectTimesheetApprovalStatusList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillProjectTimesheetApprovalStatusList()", ex.Message, false);
                throw;
            }
            return projectTimesheetApprovalStatusItems;
        }

        /// <summary>
        /// This Method Is Used To Get Employee Project Mapping Data.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="protector"></param>
        /// <param name="ProjectID"></param>
        /// <param name="EmployeeCode"></param>
        /// <returns></returns>
        public static List<EmployeeProjectMappingDataList> GetEmployeeProjectMappingData(AppDbContext db, IDataProtector protector, int ProjectID = 0, string EmployeeCode = "")
        {
            List<EmployeeProjectMappingDataList> empProjectMappingList = new List<EmployeeProjectMappingDataList>();
            try
            {
                if (ProjectID > 0 && EmployeeCode != "")
                {
                    var query = from employeeProjectMapping in db.EmployeeProjectMappingList
                                join projectMaster in db.ProjectList on employeeProjectMapping.ProjectID equals projectMaster.Id
                                join employeeMaster in db.EmployeeList on employeeProjectMapping.EmployeeCode equals employeeMaster.EmployeeCode
                                where employeeProjectMapping.ProjectID == ProjectID && employeeProjectMapping.EmployeeCode == EmployeeCode
                                select new
                                {
                                    EmployeeProjectMappingID = employeeProjectMapping.Id,
                                    EncryptedId = protector.Protect(employeeProjectMapping.Id.ToString()),
                                    ProjectID = employeeProjectMapping.ProjectID,
                                    EmployeeCode = employeeProjectMapping.EmployeeCode,
                                    ProjectCode = projectMaster.ProjectCode,
                                    ProjectName = projectMaster.ProjectName,
                                    ClientName = projectMaster.ClientName,
                                    ProjectDescription = projectMaster.ProjectDescription,
                                    StartDate = projectMaster.StartDate,
                                    EndDate = projectMaster.EndDate,
                                    EmployeeName = employeeMaster.FullName,
                                    EmployeeEmailID = employeeMaster.EmailID,
                                    IsNotifyToEmployee = employeeProjectMapping.IsNotifyToEmployee,
                                    IsDeleted = employeeProjectMapping.IsDeleted,
                                    ActiveStatus = employeeProjectMapping.IsDeleted == false ? "Active" : "Inactive",
                                };

                    empProjectMappingList = query.AsEnumerable()
                        .Select((item, index) => new EmployeeProjectMappingDataList
                        {
                            SLNo = (Int64)index + 1, // index starts from 0, so add 1 to make it 1-based
                            EmployeeProjectMappingID = item.EmployeeProjectMappingID,
                            EncryptedId = item.EncryptedId,
                            ProjectID = item.ProjectID,
                            ProjectCode = item.ProjectCode,
                            ProjectName = item.ProjectName,
                            ClientName = item.ClientName,
                            ProjectDescription = item.ProjectDescription,
                            StartDate = item.StartDate.ToString(DateTimeFormatConstants.DDMMMYYYY_SLASH_DATEFORMAT),
                            EndDate = item.EndDate.ToString(DateTimeFormatConstants.DDMMMYYYY_SLASH_DATEFORMAT),
                            EmployeeCode = item.EmployeeCode,
                            EmployeeName = item.EmployeeName,
                            EmployeeEmailID = item.EmployeeEmailID,
                            IsNotifyToEmployee = item.IsNotifyToEmployee,
                            IsDeleted = item.IsDeleted,
                            ActiveStatus = item.ActiveStatus
                        }).ToList();
                }
                else
                {
                    var query = from employeeProjectMapping in db.EmployeeProjectMappingList
                                join projectMaster in db.ProjectList on employeeProjectMapping.ProjectID equals projectMaster.Id
                                join employeeMaster in db.EmployeeList on employeeProjectMapping.EmployeeCode equals employeeMaster.EmployeeCode
                                select new
                                {
                                    EmployeeProjectMappingID = employeeProjectMapping.Id,
                                    EncryptedId = protector.Protect(employeeProjectMapping.Id.ToString()),
                                    ProjectID = employeeProjectMapping.ProjectID,
                                    EmployeeCode = employeeProjectMapping.EmployeeCode,
                                    ProjectCode = projectMaster.ProjectCode,
                                    ProjectName = projectMaster.ProjectName,
                                    ClientName = projectMaster.ClientName,
                                    ProjectDescription = projectMaster.ProjectDescription,
                                    StartDate = projectMaster.StartDate,
                                    EndDate = projectMaster.EndDate,
                                    EmployeeName = employeeMaster.FullName,
                                    EmployeeEmailID = employeeMaster.EmailID,
                                    IsNotifyToEmployee = employeeProjectMapping.IsNotifyToEmployee,
                                    IsDeleted = employeeProjectMapping.IsDeleted,
                                    ActiveStatus = employeeProjectMapping.IsDeleted == false ? "Active" : "Inactive",
                                };

                    empProjectMappingList = query.AsEnumerable()
                        .Select((item, index) => new EmployeeProjectMappingDataList
                        {
                            SLNo = (Int64)index + 1, // index starts from 0, so add 1 to make it 1-based
                            EmployeeProjectMappingID = item.EmployeeProjectMappingID,
                            EncryptedId = item.EncryptedId,
                            ProjectID = item.ProjectID,
                            ProjectCode = item.ProjectCode,
                            ProjectName = item.ProjectName,
                            ClientName = item.ClientName,
                            ProjectDescription = item.ProjectDescription,
                            StartDate = item.StartDate.ToString(DateTimeFormatConstants.DDMMMYYYY_SLASH_DATEFORMAT),
                            EndDate = item.EndDate.ToString(DateTimeFormatConstants.DDMMMYYYY_SLASH_DATEFORMAT),
                            EmployeeCode = item.EmployeeCode,
                            EmployeeName = item.EmployeeName,
                            EmployeeEmailID = item.EmployeeEmailID,
                            IsNotifyToEmployee = item.IsNotifyToEmployee,
                            IsDeleted = item.IsDeleted,
                            ActiveStatus = item.ActiveStatus
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetEmployeeProjectMappingData()", ex.Message, false);
                throw;
            }
            return empProjectMappingList;
        }

        /// <summary>
        /// This Method Is Used To Fill Project Name Based on Employee Code.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="EmployeeCode"></param>
        /// <returns></returns>
        public static SelectList FillProjectNameBasedOnEmployeeCode(AppDbContext db, string EmployeeCode)
        {
            SelectList projectItems = null;
            try
            {
                List<SelectListItem> projectList = (from p in db.ProjectList join epm in db.EmployeeProjectMappingList on p.Id equals epm.ProjectID 
                                                    where epm.EmployeeCode == EmployeeCode && p.ProjectStatus == "2" && epm.IsDeleted == false
                                                    orderby p.ProjectName ascending
                                                    select new SelectListItem() { Text = p.ProjectName, Value = Convert.ToString(epm.ProjectID) }).ToList();
                projectList.Insert(0, new SelectListItem()
                {
                    Text = "SELECT",
                    Value = ""
                });
                projectItems = new SelectList(projectList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillProjectNameBasedOnEmployeeCode()", ex.Message, false);
                throw;
            }
            return projectItems;
        }

        /// <summary>
        /// This Method Is Used To Fill Year Month List.
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public static SelectList FillYearMonthList(string Source)
        {
            SelectList Items = null;
            try
            {
                List<SelectListItem> DropDownData = new List<SelectListItem>();
                DropDownData.Add(new SelectListItem { Value = "", Text = "SELECT" });
                if (Source == "Year")
                {
                    int currentYear = DateTime.Now.Year;
                    var years = Enumerable.Range(1990, currentYear - 1989).Reverse().ToList();
                    foreach (var year in years)
                    {
                        DropDownData.Add(new SelectListItem { Value = year.ToString(), Text = year.ToString() });
                    }
                }
                if (Source == "Month")
                {
                    var monthNames = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames;
                    foreach (var month in monthNames)
                    {
                        if (!string.IsNullOrEmpty(month))  // Skip empty entries in the array
                        {
                            DropDownData.Add(new SelectListItem { Value = month, Text = month });
                        }
                    }
                }
                Items = new SelectList(DropDownData, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.FillYearMonthList()", ex.Message, false);
                throw;
            }
            return Items;
        }

        /// <summary>
        /// This Method Is Used To Get Approver Email ID.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static List<UsersM> GetApproverDetails(AppDbContext db)
        {
            List<UsersM> UserDetails = new List<UsersM>();
            try
            {
                var userList = (from c in db.UserList
                                   where c.IsApprover == true && c.IsDeleted == false
                                   orderby c.EmailID ascending
                                   select new UsersM
                                   {
                                       Id = c.Id,
                                       FullName = c.FullName,
                                       EmailID = c.EmailID
                                   }).ToList();

                if (userList != null && userList.Count > 0)
                {
                    UserDetails = userList.ToList();
                }
            }
            catch (Exception ex)
            {
                UserDetails = null;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetApproverDetails()", ex.Message, false);
            }
            return UserDetails;
        }

        /// <summary>
        /// This Method Is Used To Get User Details By Employee Code.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="EmployeeCode"></param>
        /// <returns></returns>
        public static List<UsersM> GetUserDetailsByEmployeeCode(AppDbContext db, string EmployeeCode)
        {
            List<UsersM> UserDetails = new List<UsersM>();
            try
            {
                var userList = (from c in db.UserList
                                where c.EmployeeCode == EmployeeCode && c.IsDeleted == false
                                orderby c.EmailID ascending
                                select new UsersM
                                {
                                    Id = c.Id,
                                    FullName = c.FullName,
                                    EmailID = c.EmailID
                                }).ToList();

                if (userList != null && userList.Count > 0)
                {
                    UserDetails = userList.ToList();
                }
            }
            catch (Exception ex)
            {
                UserDetails = null;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetUserDetailsByEmployeeCode()", ex.Message, false);
            }
            return UserDetails;
        }

        /// <summary>
        /// This Method Is Used To Get Employees Working on Project.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SelectList GetEmployeesWorkingOnProject(AppDbContext db)
        {
            SelectList employeeItems = null;
            try
            {

                List<SelectListItem> employeeList = (from epm in db.EmployeeProjectMappingList
                                                     join e in db.EmployeeList on epm.EmployeeCode equals e.EmployeeCode
                                                     group e by new { e.EmployeeCode, e.FullName } into g
                                                     orderby g.Key.FullName
                                                     select new SelectListItem
                                                     {
                                                         Text = g.Key.FullName,
                                                         Value = g.Key.EmployeeCode
                                                     }).ToList();

                employeeList.Insert(0, new SelectListItem()
                {
                    Text = "SELECT",
                    Value = ""
                });
                employeeItems = new SelectList(employeeList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetUserDetailsByEmployeeCode()", ex.Message, false);
            }
            return employeeItems;
        }

        /// <summary>
        /// This Method Is Used To Get Employees Working on Project.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SelectList GetEmployeesWorkingOnProjectWithAll(AppDbContext db)
        {
            SelectList employeeItems = null;
            try
            {

                List<SelectListItem> employeeList = (from epm in db.EmployeeProjectMappingList
                                                     join e in db.EmployeeList on epm.EmployeeCode equals e.EmployeeCode
                                                     group e by new { e.EmployeeCode, e.FullName } into g
                                                     orderby g.Key.FullName
                                                     select new SelectListItem
                                                     {
                                                         Text = g.Key.FullName,
                                                         Value = g.Key.EmployeeCode
                                                     }).ToList();

                employeeList.Insert(0, new SelectListItem()
                {
                    Text = "ALL",
                    Value = "0"
                });
                employeeItems = new SelectList(employeeList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetEmployeesWorkingOnProjectWithAll()", ex.Message, false);
            }
            return employeeItems;
        }

        /// <summary>
        /// This Method Is Used To Get Logged In Employee Working on Project.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="EmployeeCode"></param>
        /// <returns></returns>
        public static SelectList GetLoggedInEmployeeWorkingOnProject(AppDbContext db, string EmployeeCode)
        {
            SelectList employeeItems = null;
            try
            {

                List<SelectListItem> employeeList = (from epm in db.EmployeeProjectMappingList
                                                     join e in db.EmployeeList on epm.EmployeeCode equals e.EmployeeCode
                                                     where e.EmployeeCode == EmployeeCode
                                                     group e by new { e.EmployeeCode, e.FullName } into g
                                                     orderby g.Key.FullName
                                                     select new SelectListItem
                                                     {
                                                         Text = g.Key.FullName,
                                                         Value = g.Key.EmployeeCode
                                                     }).ToList();

                employeeItems = new SelectList(employeeList, "Value", "Text");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "DropdownHelper.GetLoggedInEmployeeWorkingOnProject()", ex.Message, false);
            }
            return employeeItems;
        }

        /// <summary>
        /// This Method Is Used To Get Email Configuration Count.
        /// </summary>
        /// <returns></returns>
        public static int GetApproverCount(AppDbContext db, int UserID)
        {
            int TotalCount = 0;
            try
            {
                TotalCount = (from c in db.UserList
                              where c.IsApprover == true && c.IsDeleted == false && c.Id != UserID
                              select new { c.Id }).Count();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.Controllers", "UsersController.GetApproverCount()", ex.Message, false);
                throw;
            }
            return TotalCount;
        }

        #endregion [DropDown List]
    }
}
