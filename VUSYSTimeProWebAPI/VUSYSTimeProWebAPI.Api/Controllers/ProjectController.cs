using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebAPI.Api.Controllers.BaseControllers;
using VUSYSTimeProWebAPI.Api.Validation;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Project;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Controllers
{
    public class ProjectController : BaseApiAuthorizationController
    {
        private readonly IProjectService _ProjectService;
        private const string ControllerLogName = "ProjectController";

        /// <summary>
        /// Project Controller Class Constructor
        /// </summary>
        /// <param name="ProjectService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ProjectController(IProjectService ProjectService)
        {
            _ProjectService = ProjectService ?? throw new ArgumentNullException(nameof(ProjectService));
        }

        #region [Project Master]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Project Details.
        /// </summary>
        /// <param name="_objProjectRequestParameterCommon"></param>
        /// To Save and Update Project Details Below Request Parameters is required Such as ProjectID, ProjectCode, ClientName, ProjectName, ProjectDescription, 
        /// StartDate, EndDate, AllocatedHours, ConsumedHours, ProjectStatus, CreatedBy, UpdatedBy and IsDeleted.
        /// <returns>
        /// A JSON-formatted string representing the Project Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateProjectDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateProjectDetails(ProjectRequestParameterCommon _objProjectRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ProjectM _objProjectM = Helper.ToCopyClassProperties<ProjectRequestParameterCommon, ProjectM>(_objProjectRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ProjectService.SaveUpdateProjectDetails(_objProjectM, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateProjectDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Project Details.
        /// </summary>
        /// <param name="_objProjectDetailsRequestParam"></param>
        /// To Get Project Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Project Details.
        /// </returns>
        [HttpPost, ActionName("GetProjectDetails")]
        public async Task<ActionResult<string>> GetProjectDetails(ProjectDetailsRequestParam _objProjectDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ProjectService.GetProjectDetailOrList(_objProjectDetailsRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetProjectDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Project List.
        /// </summary>
        /// <param name="_objProjectDetailsRequestParam"></param>
        /// To Get Project List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the Project List.
        /// </returns>
        [HttpPost, ActionName("GetProjectList")]
        public async Task<ActionResult<string>> GetProjectList(ProjectDetailsRequestParam _objProjectDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objProjectDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ProjectService.GetProjectDetailOrList(_objProjectDetailsRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetProjectList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Project Master]

        #region [Employee Project Mapping]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Employee Project Mapping Details.
        /// </summary>
        /// <param name="_objEmployeeProjectMappingRequestParameterCommon"></param>
        /// To Save and Update Employee Project Mapping Details Below Request Parameters is required Such as Id, ProjectID, EmployeeCode, IsNotifyToEmployee, CreatedBy, UpdatedBy.
        /// <returns>
        /// A JSON-formatted string representing the Employee Project Mapping Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateEmployeeTypeDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateEmployeeTypeDetails(EmployeeProjectMappingRequestParameterCommon _objEmployeeProjectMappingRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    EmployeeProjectMappingM _objEmployeeProjectMappingM = Helper.ToCopyClassProperties<EmployeeProjectMappingRequestParameterCommon, EmployeeProjectMappingM>(_objEmployeeProjectMappingRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ProjectService.SaveUpdateEmployeeProjectMappingDetails(_objEmployeeProjectMappingM, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateEmployeeProjectMappingDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Employee Project Mapping Details.
        /// </summary>
        /// <param name="_objEmployeeProjectMappingDetailsRequestParam"></param>
        /// To Get Employee Project Mapping Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Employee Project Mapping Details.
        /// </returns>
        [HttpPost, ActionName("GetEmployeeProjectMappingDetails")]
        public async Task<ActionResult<string>> GetEmployeeProjectMappingDetails(EmployeeProjectMappingDetailsRequestParam _objEmployeeProjectMappingDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ProjectService.GetEmployeeProjectMappingDetailOrList(_objEmployeeProjectMappingDetailsRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeeProjectMappingDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Employee Project Mapping List.
        /// </summary>
        /// <param name="_objEmployeeProjectMappingDetailsRequestParam"></param>
        /// To Get Employee Project Mapping List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the Employee Project Mapping List.
        /// </returns>
        [HttpPost, ActionName("GetEmployeeProjectMappingList")]
        public async Task<ActionResult<string>> GetEmployeeProjectMappingList(EmployeeProjectMappingDetailsRequestParam _objEmployeeProjectMappingDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objEmployeeProjectMappingDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ProjectService.GetEmployeeProjectMappingDetailOrList(_objEmployeeProjectMappingDetailsRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetEmployeeProjectMappingList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Employee Project Mapping]

        #region [Project Activity]

        /// <summary>
        /// This Post Action Method Is Used To Save and Update Project Activity Details.
        /// </summary>
        /// <param name="_objProjectActivityRequestParameterCommon"></param>
        /// To Save and Update Project Activity Details Below Request Parameters is required Such as Id, Project Activity Id, Activity Name IsDeleted, CreatedBy, UpdatedBy
        /// <returns>
        /// A JSON-formatted string representing the Project Activity Details.
        /// </returns>
        [HttpPost, ActionName("SaveAndUpdateProjectActivityDetails")]
        public async Task<ActionResult<string>> SaveAndUpdateProjectActivityDetails(ProjectActivityRequestParameterCommon _objProjectActivityRequestParameterCommon)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ProjectActivityM _objProjectActivityM = Helper.ToCopyClassProperties<ProjectActivityRequestParameterCommon, ProjectActivityM>(_objProjectActivityRequestParameterCommon);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ProjectService.SaveUpdateProjectActivityDetails(_objProjectActivityM, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.SaveAndUpdateProjectActivityDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Project Activity Details.
        /// </summary>
        /// <param name="_objProjectActivityDetailsRequestParam"></param>
        /// To Get Project Activity Details Below Request Parameters is required Such as Id.
        /// <returns>
        /// A JSON-formatted string representing the Project Activity Details.
        /// </returns>
        [HttpPost, ActionName("GetProjectActivityDetails")]
        public async Task<ActionResult<string>> GetProjectActivityDetails(ProjectActivityDetailsRequestParam _objProjectActivityDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ProjectService.GetProjectActivityDetailOrList(_objProjectActivityDetailsRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetProjectActivityDetails()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        /// <summary>
        /// This Post Action Method Is Used To Get Project Activity List.
        /// </summary>
        /// <param name="_objProjectActivityDetailsRequestParam"></param>
        /// To Get Project Activity List Below Request Parameters is required Such as Id and Value must be 0.
        /// <returns>
        /// A JSON-formatted string representing the Project Activity List.
        /// </returns>
        [HttpPost, ActionName("GetProjectActivityList")]
        public async Task<ActionResult<string>> GetProjectActivityList(ProjectActivityDetailsRequestParam _objProjectActivityDetailsRequestParam)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (ModelState.IsValid)
                {
                    _objProjectActivityDetailsRequestParam.Id = 0;
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(await _ProjectService.GetProjectActivityDetailOrList(_objProjectActivityDetailsRequestParam, RequestCancellationToken));
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    ReturnResult = ModelErrorValidation.ConvertModelErrorsToJson(ModelState);
                    if (ReturnResult == null || ReturnResult == string.Empty)
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, ControllerLogNamespace, $"{ControllerLogName}.GetProjectActivityList()", ex.Message, false);
                throw;
            }
            return Content(Util.JSONListOK(ReturnResult), ContentTypeConstants.JSON_CONTENTTYPE);
        }

        #endregion [Project Activity]
    }
}
