using VUSYSAppHub.Models;

namespace VUSYSAppHub.Services
{
    public interface IUpdateDeploymentVersionService
    {
        Task<List<ApplicationNameModel>> GetApplicationsAsync();
        Task<string> GetLastVersionAsync(string applicationName, string environmentName);
        Task<(string LastVersion, string AppUpdateBase64)> GetDeploymentVersionDetailsAsync(string applicationName, string environmentName);
        string CalculateNextVersion(string lastVersion);
        Task<bool> UpdateDeploymentVersionAsync(UpdateDeploymentVersionModel model);
        Task<EmployeeLoginValidationResultModel> ValidateLoginIdAsync(EmployeeLoginValidationRequestModel objEmployeeLoginValidationRequestModel);
        Task<string> SendDeploymentOtpAsync(DeploymentSendOtpModel objDeploymentSendOtpModel);
    }
}