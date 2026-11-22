using System.Data;
using System.Data.Odbc;
using System.Globalization;
using System.Text.Json;
using CommonLib;
using Newtonsoft.Json.Linq;
using VUSYSAppHub.AppHubPageHelper;
using VUSYSAppHub.Contracts;
using VUSYSAppHub.DACHelper;
using VUSYSAppHub.Models;

namespace VUSYSAppHub.Services
{
    public class UpdateDeploymentVersionService : IUpdateDeploymentVersionService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ODBCHelper _ODBCHelper;

        public UpdateDeploymentVersionService(IWebHostEnvironment env, ODBCHelper ODBCHelper)
        {
            _env = env;
            _ODBCHelper = ODBCHelper;
        }

        public async Task<List<ApplicationNameModel>> GetApplicationsAsync()
        {
            try
            {
                var filePath = CommonHelper.GetJSONFilePath(_env, "ApplicationName.json");

                if (!File.Exists(filePath))
                    return new List<ApplicationNameModel>();

                var json = await File.ReadAllTextAsync(filePath);

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var result = new List<ApplicationNameModel>();

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("ApplicationNames", out var applicationNamesElement) &&
                    applicationNamesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in applicationNamesElement.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                            continue;

                        var appName = item.GetString()?.Trim();

                        if (string.IsNullOrWhiteSpace(appName))
                            continue;

                        result.Add(new ApplicationNameModel
                        {
                            ApplicationName = appName
                        });
                    }
                }

                return result
                    .Where(x => !string.IsNullOrWhiteSpace(x.ApplicationName))
                    .GroupBy(x => x.ApplicationName, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .OrderBy(x => x.ApplicationName)
                    .ToList();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "UpdateDeploymentVersionService.GetApplicationsAsync()", ex.Message, false);
                throw new Exception($"Failed to load application names. {ex.Message}");
            }
        }

        public async Task<string> GetLastVersionAsync(string applicationName, string environmentName)
        {
            try
            {
                var deploymentVersionDetails = await GetDeploymentVersionDetailsAsync(applicationName, environmentName);
                return deploymentVersionDetails.LastVersion;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "UpdateDeploymentVersionService.GetLastVersionAsync()", ex.Message, false);
                throw new Exception($"Failed to fetch last version. {ex.Message}");
            }
        }

        public async Task<(string LastVersion, string AppUpdateBase64)> GetDeploymentVersionDetailsAsync(string applicationName, string environmentName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(applicationName))
                    throw new Exception("Application name is required.");

                if (string.IsNullOrWhiteSpace(environmentName))
                    throw new Exception("Environment name is required.");

                string normalizedApplicationName = AppHubCommonHelper.NormalizeApplicationName(applicationName);

                var connectionString = await GetConnectionStringAsync(normalizedApplicationName, environmentName);

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new Exception($"Connection string not found for Application = {applicationName}, Environment = {environmentName}.");

                var configId = GetConfigIdByApplication(applicationName);
                const string query = @"SELECT config_info, appupdate_base64 FROM system_config WHERE config_id = ?";
                var parameters = new List<(string, OdbcType, object, int?)>
                {
                    ("config_id", OdbcType.NVarChar, configId, 100)
                };

                var dataSet = await _ODBCHelper.ExecuteRawQueryWithMultipleParametersAsync(query, connectionString, parameters);

                if (dataSet == null ||
                    dataSet.Tables.Count == 0 ||
                    dataSet.Tables[0] == null ||
                    dataSet.Tables[0].Rows.Count == 0)
                {
                    return ("0.0", string.Empty);
                }

                var table = dataSet.Tables[0];
                var row = table.Rows[0];

                string versionText = "0.0";
                string appUpdateBase64 = string.Empty;

                if (table.Columns.Contains("config_info"))
                {
                    var configInfoValue = row["config_info"];

                    if (configInfoValue != null && configInfoValue != DBNull.Value)
                    {
                        var rawVersionText = Convert.ToString(configInfoValue)?.Trim();

                        if (!string.IsNullOrWhiteSpace(rawVersionText))
                        {
                            if (decimal.TryParse(rawVersionText, NumberStyles.Any, CultureInfo.InvariantCulture, out var versionDecimal))
                                versionText = versionDecimal.ToString("0.0", CultureInfo.InvariantCulture);
                            else if (decimal.TryParse(rawVersionText, NumberStyles.Any, CultureInfo.CurrentCulture, out versionDecimal))
                                versionText = versionDecimal.ToString("0.0", CultureInfo.InvariantCulture);
                        }
                    }
                }

                if (table.Columns.Contains("appupdate_base64"))
                {
                    var appUpdateBase64Value = row["appupdate_base64"];
                    if (appUpdateBase64Value != null && appUpdateBase64Value != DBNull.Value)
                    {
                        appUpdateBase64 = Convert.ToString(appUpdateBase64Value)?.Trim() ?? string.Empty;
                    }
                }
                return (versionText, appUpdateBase64);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "UpdateDeploymentVersionService.GetDeploymentVersionDetailsAsync()", ex.Message, false);
                throw new Exception($"Failed to fetch deployment version details. {ex.Message}");
            }
        }

        public string CalculateNextVersion(string lastVersion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(lastVersion))
                    return "0.1";

                if (!decimal.TryParse(lastVersion, NumberStyles.Any, CultureInfo.InvariantCulture, out var version))
                {
                    if (!decimal.TryParse(lastVersion, NumberStyles.Any, CultureInfo.CurrentCulture, out version))
                        return "0.1";
                }
                version += 0.1m;
                version = Math.Round(version, 1, MidpointRounding.AwayFromZero);
                return version.ToString("0.0", CultureInfo.InvariantCulture);
            }
            catch
            {
                return "0.1";
            }
        }

        public async Task<bool> UpdateDeploymentVersionAsync(UpdateDeploymentVersionModel model)
        {
            try
            {
                if (model == null)
                    throw new Exception("Model is required.");

                if (string.IsNullOrWhiteSpace(model.ApplicationName))
                    throw new Exception("Application name is required.");

                if (string.IsNullOrWhiteSpace(model.Environment))
                    throw new Exception("Environment is required.");

                if (string.IsNullOrWhiteSpace(model.VersionNumber))
                    throw new Exception("Version number is required.");

                string normalizedApplicationName = AppHubCommonHelper.NormalizeApplicationName(model.ApplicationName);

                var connectionString = await GetConnectionStringAsync(normalizedApplicationName, model.Environment);

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new Exception($"Connection string not found for Application = {model.ApplicationName}, Environment = {model.Environment}.");

                var configId = GetConfigIdByApplication(model.ApplicationName);

                var configInfoValue = model.VersionNumber;

                if (decimal.TryParse(model.VersionNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out var versionDecimal))
                    configInfoValue = versionDecimal.ToString("0.0", CultureInfo.InvariantCulture);
                else if (decimal.TryParse(model.VersionNumber, NumberStyles.Any, CultureInfo.CurrentCulture, out versionDecimal))
                    configInfoValue = versionDecimal.ToString("0.0", CultureInfo.InvariantCulture);

                string query;
                List<(string, OdbcType, object, int?)> parameters;

                if (!string.IsNullOrWhiteSpace(model.AppUpdateBase64))
                {
                    query = @"UPDATE system_config SET config_info = ?, appupdate_base64 = ? WHERE config_id = ?";

                    parameters = new List<(string, OdbcType, object, int?)>
                    {
                        ("config_info", OdbcType.NVarChar, configInfoValue, 100),
                        ("appupdate_base64", OdbcType.Text, model.AppUpdateBase64.Trim(), null),
                        ("config_id", OdbcType.NVarChar, configId, 100)
                    };
                }
                else
                {
                    query = @"UPDATE system_config SET config_info = ? WHERE config_id = ?";
                    parameters = new List<(string, OdbcType, object, int?)>
                    {
                        ("config_info", OdbcType.NVarChar, configInfoValue, 100),
                        ("config_id", OdbcType.NVarChar, configId, 100)
                    };
                }
                var rowsAffected = await _ODBCHelper.ExecuteRawNonQueryWithMultipleParametersAsync(query, connectionString, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "UpdateDeploymentVersionService.UpdateDeploymentVersionAsync()", ex.Message, false);
                throw new Exception($"Failed to update deployment version. {ex.Message}");
            }
        }

        public async Task<EmployeeLoginValidationResultModel> ValidateLoginIdAsync(EmployeeLoginValidationRequestModel objEmployeeLoginValidationRequestModel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(objEmployeeLoginValidationRequestModel.ApplicationName))
                    throw new Exception("Application name is required.");

                if (string.IsNullOrWhiteSpace(objEmployeeLoginValidationRequestModel.EnvironmentName))
                    throw new Exception("Environment is required.");

                if (string.IsNullOrWhiteSpace(objEmployeeLoginValidationRequestModel.LoginID))
                    throw new Exception("Login ID is required.");

                string normalizedApplicationName = AppHubCommonHelper.NormalizeApplicationName(objEmployeeLoginValidationRequestModel.ApplicationName);

                var connectionString = await GetConnectionStringAsync(normalizedApplicationName, objEmployeeLoginValidationRequestModel.EnvironmentName);

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new Exception($"Connection string not found for Application = {objEmployeeLoginValidationRequestModel.ApplicationName}, Environment = {objEmployeeLoginValidationRequestModel.EnvironmentName}.");

                const string query = @"
SELECT login_id, empl_name, email_id
FROM empl_master
WHERE login_id = ?";

                var parameters = new List<(string, OdbcType, object, int?)>
                {
                    ("login_id", OdbcType.NVarChar, objEmployeeLoginValidationRequestModel.LoginID.Trim(), 100)
                };

                var dataSet = await _ODBCHelper.ExecuteRawQueryWithMultipleParametersAsync(query, connectionString, parameters);

                if (Helper.CheckDataSetIsNullOrEmpty(dataSet))
                {
                    var table = dataSet.Tables[0];

                    if (!table.Columns.Contains("login_id") ||
                        !table.Columns.Contains("empl_name") ||
                        !table.Columns.Contains("email_id"))
                    {
                        throw new Exception("Required columns login_id, empl_name, email_id are not available in empl_master.");
                    }

                    var row = table.Rows[0];

                    var dbLoginId = Convert.ToString(row["login_id"])?.Trim() ?? string.Empty;
                    var employeeName = Convert.ToString(row["empl_name"])?.Trim() ?? string.Empty;
                    var emailId = Convert.ToString(row["email_id"])?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(dbLoginId))
                    {
                        return new EmployeeLoginValidationResultModel
                        {
                            IsValid = false,
                            ErrorMessage = "Invalid Login ID."
                        };
                    }

                    return new EmployeeLoginValidationResultModel
                    {
                        IsValid = true,
                        LoginId = dbLoginId,
                        EmployeeName = employeeName,
                        EmailId = emailId
                    };
                }
                else
                {
                    return new EmployeeLoginValidationResultModel
                    {
                        IsValid = false,
                        ErrorMessage = "Invalid Login ID."
                    };
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "UpdateDeploymentVersionService.ValidateLoginIdAsync()", ex.Message, false);
                throw new Exception($"Failed to validate Login ID. {ex.Message}");
            }
        }

        public async Task<string> SendDeploymentOtpAsync(DeploymentSendOtpModel objDeploymentSendOtpModel)
        {
            string emailSentResponseReturnResult = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(objDeploymentSendOtpModel.LoginId))
                    throw new Exception("Login ID is required.");

                if (string.IsNullOrWhiteSpace(objDeploymentSendOtpModel.EmailId))
                    throw new Exception("Email ID is required.");

                if (string.IsNullOrWhiteSpace(objDeploymentSendOtpModel.Otp))
                    throw new Exception("OTP is required.");

                string OTPEmailBodyTemplateFolderPath = CommonHelper.GetActualPath(_env, "Templates");
                string OTPEmailBodyTemplatePath = OTPEmailBodyTemplateFolderPath + "\\OTPEmailTemplate.html";

                StreamReader rdrEmailBody = new StreamReader(OTPEmailBodyTemplatePath);
                string txthtmlEmailBody = rdrEmailBody.ReadToEnd();
                string EmailBodyTemplate = txthtmlEmailBody;
                EmailBodyTemplate = EmailBodyTemplate.Replace("#Name#", objDeploymentSendOtpModel.EmployeeName);

                EmailBodyTemplate = EmailBodyTemplate.Replace("#ApplicationName#", objDeploymentSendOtpModel.ApplicationName);
                EmailBodyTemplate = EmailBodyTemplate.Replace("#EnvironmentName#", objDeploymentSendOtpModel.Environment);
                EmailBodyTemplate = EmailBodyTemplate.Replace("#LoginId#", objDeploymentSendOtpModel.LoginId);
                EmailBodyTemplate = EmailBodyTemplate.Replace("#NewVersion#", objDeploymentSendOtpModel.NewVersion);
                EmailBodyTemplate = EmailBodyTemplate.Replace("#OTP#", objDeploymentSendOtpModel.Otp);
                EmailBodyTemplate = EmailBodyTemplate.Replace("#ExpiryMinutes#", objDeploymentSendOtpModel.ExpiryInMinutes);
                EmailBodyTemplate = EmailBodyTemplate.Replace("#CurrentYear#", DateTime.Now.Year.ToString());

                SendEmailRequestParam objSendEmailRequestParam = new SendEmailRequestParam();
                objSendEmailRequestParam.ToEmail = objDeploymentSendOtpModel.EmailId;
                objSendEmailRequestParam.EmailSubject = $"OTP for Deployment Version Update - {objDeploymentSendOtpModel.ApplicationName} ({objDeploymentSendOtpModel.Environment})";
                objSendEmailRequestParam.EmailBody = EmailBodyTemplate;
                var EmailReturnResult = CommonHelper.SendEmailAPI(_env, objSendEmailRequestParam, string.Empty, string.Empty, false, false);
                string returnResult = ObjectToJson.ConvertDataSetToJson(EmailReturnResult);
                JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(returnResult));
                string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, "EMAIL_SENT_RESPONSE", 1);

                string successMessage = $"A New OTP has been sent to {objDeploymentSendOtpModel.EmailId}.";
                string failureMessage = "Failed To Send an Email !!!";
                string EmailSentStatus = Helper.ToProperCase(Helper.Error);
                emailSentResponseReturnResult = failureMessage;
                if (string.Equals(resultjObjectEmail, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    var emailResponseData = jObjectEmail["data"]?["EMAIL_SENT_RESPONSE"]?.ToObject<SendEmailResponseParam[]>();
                    var firstResponse = emailResponseData?.FirstOrDefault();
                    if (string.Equals(firstResponse?.Message, Helper.EmailSuccess, StringComparison.OrdinalIgnoreCase))
                    {
                        emailSentResponseReturnResult = successMessage;
                    }
                    else
                    {
                        emailSentResponseReturnResult = failureMessage;
                    }
                }
                else
                {
                    emailSentResponseReturnResult = failureMessage;
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "UpdateDeploymentVersionService.SendDeploymentOtpAsync()", ex.Message, false);
                throw new Exception($"Failed to send deployment OTP. {ex.Message}");
            }
            return emailSentResponseReturnResult;
        }

        private string GetConfigIdByApplication(string applicationName)
        {
            string ReturnResult = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(applicationName))
                    throw new Exception("Application name is required.");

                if (string.Equals(applicationName, "Sharpply", StringComparison.OrdinalIgnoreCase))
                {
                    ReturnResult = "ERP-WEB-VERSION";
                    return ReturnResult;
                }
                else if (string.Equals(applicationName, "Sharpply PWA", StringComparison.OrdinalIgnoreCase))
                {
                    ReturnResult = "CRM-PWA-VERSION";
                    return ReturnResult;
                }
                else if (string.Equals(applicationName, "Protectron", StringComparison.OrdinalIgnoreCase))
                {
                    ReturnResult = "ERP-WEB-VERSION";
                    return ReturnResult;
                }
                else if (string.Equals(applicationName, "Inspire Magnetics", StringComparison.OrdinalIgnoreCase))
                {
                    ReturnResult = "ERP-WEB-VERSION";
                    return ReturnResult;
                }
            }
            catch (Exception ex)
            {
                ReturnResult = string.Empty;
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "UpdateDeploymentVersionService.GetConfigIdByApplication()", ex.Message, false);
                throw new Exception($"Config mapping not found for application: {applicationName}");
            }
            return ReturnResult;
        }

        private async Task<string?> GetConnectionStringAsync(string applicationName, string environment)
        {
            try
            {
                string normalizedApplicationName = AppHubCommonHelper.NormalizeApplicationName(applicationName);
                string EncryptDecryptKey = AppHubCommonHelper.GetEncryptDecryptKey(normalizedApplicationName);

                var filePath = CommonHelper.GetJSONFilePath(_env, "DBConnection.json");

                if (!File.Exists(filePath))
                    throw new Exception($"DBConnection.json file not found at path: {filePath}");

                var json = await File.ReadAllTextAsync(filePath);

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    throw new Exception("Invalid DBConnection.json format.");

                if (!root.TryGetProperty("ConnectionMappings", out var mappingsElement) ||
                    mappingsElement.ValueKind != JsonValueKind.Array)
                {
                    throw new Exception("ConnectionMappings section not found in DBConnection.json.");
                }

                foreach (var item in mappingsElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                        continue;

                    var appName = item.TryGetProperty("Application", out var appElement)
                        ? appElement.GetString()?.Trim() ?? string.Empty
                        : string.Empty;

                    var envName = item.TryGetProperty("Environment", out var envElement)
                        ? envElement.GetString()?.Trim() ?? string.Empty
                        : string.Empty;

                    var connectionString = item.TryGetProperty("ConnectionString", out var connElement)
                        ? connElement.GetString()?.Trim() ?? string.Empty
                        : string.Empty;

                    if (string.Equals(appName, normalizedApplicationName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(envName, environment, StringComparison.OrdinalIgnoreCase))
                    {
                        return DbConnectionHelper.GetDBDecryptedConnectionString(connectionString, EncryptDecryptKey);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "UpdateDeploymentVersionService.GetConnectionStringAsync()", ex.Message, false);
                throw new Exception($"Failed to read DBConnection.json. {ex.Message}");
            }
        }

        

        
    }
}