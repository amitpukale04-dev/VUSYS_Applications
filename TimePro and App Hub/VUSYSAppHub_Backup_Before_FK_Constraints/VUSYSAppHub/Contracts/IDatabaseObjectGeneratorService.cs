using VUSYSAppHub.Models;

namespace VUSYSAppHub.Contracts
{
    public interface IDatabaseObjectGeneratorService
    {
        Task<List<string>> GetApplicationNamesAsync();
        Task<List<DbGeneratorConnectionConfig>> GetConnectionConfigurationsAsync();
        Task<(bool Success, string Message)> TestConnectionAsync(DbGeneratorConnectionConfig config);
        Task<List<DbObjectItem>> GetDatabaseObjectsAsync(DbGeneratorConnectionConfig config);
        Task<string> GenerateScriptAsync(DbGeneratorConnectionConfig config, List<DbObjectItem> selectedObjects, ScriptGenerationOptions options);
        Task<EmployeeLoginValidationResultModel> ValidateLoginIdAsync(EmployeeLoginValidationRequestModel objEmployeeLoginValidationRequestModel);
        Task<string> SendDatabaseObjectGenerationOtpAsync(DeploymentSendOtpModel objDeploymentSendOtpModel);
    }
}
