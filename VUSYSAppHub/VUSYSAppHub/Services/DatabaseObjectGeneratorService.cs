using System.Data.Common;
using System.Text;
using System.Text.Json;
using CommonLib;
using Newtonsoft.Json.Linq;
using VUSYSAppHub.AppHubPageHelper;
using VUSYSAppHub.Contracts;
using VUSYSAppHub.DACHelper;
using VUSYSAppHub.Models;
using SendEmailRequestParam = VUSYSAppHub.Models.SendEmailRequestParam;

namespace VUSYSAppHub.Services
{
    public class DatabaseObjectGeneratorService : IDatabaseObjectGeneratorService
    {
        private readonly IWebHostEnvironment _env;

        public DatabaseObjectGeneratorService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<List<string>> GetApplicationNamesAsync()
        {
            try
            {
                var filePath = Path.Combine(_env.ContentRootPath, "Configurations", "ApplicationName.json");

                if (!File.Exists(filePath))
                {
                    return new List<string>();
                }

                var json = await File.ReadAllTextAsync(filePath);

                var wrapper = JsonSerializer.Deserialize<ApplicationNameWrapper>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return wrapper?.ApplicationNames?
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList()
                    ?? new List<string>();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.GetApplicationNamesAsync()", ex.Message, false);
                throw new Exception($"Error while fetching application names: {ex.Message}", ex);
            }
        }

        public async Task<List<DbGeneratorConnectionConfig>> GetConnectionConfigurationsAsync()
        {
            try
            {
                var filePath = Path.Combine(_env.ContentRootPath, "Configurations", "DBConnection.json");

                if (!File.Exists(filePath))
                {
                    return new List<DbGeneratorConnectionConfig>();
                }

                var json = await File.ReadAllTextAsync(filePath);

                var wrapper = JsonSerializer.Deserialize<DbConnectionWrapper>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var mappings = wrapper?.ConnectionMappings ?? new List<DbConnectionMapping>();
                var results = new List<DbGeneratorConnectionConfig>();

                foreach (var mapping in mappings)
                {
                    if (string.IsNullOrWhiteSpace(mapping.Application) ||
                        string.IsNullOrWhiteSpace(mapping.Environment) ||
                        string.IsNullOrWhiteSpace(mapping.ConnectionString))
                    {
                        continue;
                    }

                    var parsedConfig = ParseConnectionMapping(mapping);
                    results.Add(parsedConfig);
                }

                return results
                    .GroupBy(x => new
                    {
                        ApplicationName = x.ApplicationName?.Trim() ?? string.Empty,
                        EnvironmentName = x.EnvironmentName?.Trim() ?? string.Empty
                    }, StringComparerForAnonymousKey.Instance)
                    .Select(g => g.First())
                    .OrderBy(x => x.ApplicationName)
                    .ThenBy(x => GetEnvironmentOrder(x.EnvironmentName))
                    .ThenBy(x => x.EnvironmentName)
                    .ToList();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.GetConnectionConfigurationsAsync()", ex.Message, false);
                throw new Exception($"Error while fetching connection configurations: {ex.Message}", ex);
            }
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync(DbGeneratorConnectionConfig config)
        {
            try
            {
                await using var connection = DBObjectGenerationCommonHelper.CreateConnection(config);
                await connection.OpenAsync();
                await connection.CloseAsync();
                return (true, "Connection Successful !!!");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.TestConnectionAsync()", ex.Message, false);
                return (false, $"Connection failed: {ex.Message}");
            }
        }

        public async Task<List<DbObjectItem>> GetDatabaseObjectsAsync(DbGeneratorConnectionConfig config)
        {
            try
            {
                await using var connection = DBObjectGenerationCommonHelper.CreateConnection(config);
                await connection.OpenAsync();

                return DBObjectGenerationCommonHelper.IsSqlAnywhere(config)
                    ? await GetSqlAnywhereObjectsAsync(connection)
                    : await GetSqlServerObjectsAsync(connection);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.GetDatabaseObjectsAsync()", ex.Message, false);
                throw new Exception($"Error while fetching database objects: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateScriptAsync(DbGeneratorConnectionConfig config, List<DbObjectItem> selectedObjects, ScriptGenerationOptions options)
        {
            try
            {
                if (selectedObjects == null || selectedObjects.Count == 0)
                {
                    return "-- No objects selected.";
                }

                await using var connection = DBObjectGenerationCommonHelper.CreateConnection(config);
                await connection.OpenAsync();

                var orderedObjects = DBObjectGenerationCommonHelper.ApplyOrdering(selectedObjects, options);

                var sb = new StringBuilder();

                sb.AppendLine("-- Database Object Generator");
                sb.AppendLine($"-- Application : {config.ApplicationName}");
                sb.AppendLine($"-- Environment : {config.EnvironmentName}");
                sb.AppendLine($"-- Database    : {config.DatabaseName}");
                sb.AppendLine($"-- DatabaseType: {config.DatabaseType}");
                sb.AppendLine($"-- GeneratedOn : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                foreach (var item in orderedObjects)
                {
                    string objectScript = DBObjectGenerationCommonHelper.IsSqlAnywhere(config)
                        ? await SQLAnyWhere17DBObjectGenerationHelper.GenerateSqlAnywhereObjectScriptAsync(_env, connection, config, item, options)
                        : await SQLDBObjectGenerationHelper.GenerateSqlServerObjectScriptAsync(_env, connection, config, item, options);

                    sb.AppendLine(objectScript);
                    sb.AppendLine();
                    sb.AppendLine(new string('-', 120));
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.GenerateScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating database object script: {ex.Message}", ex);
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

                var connectionConfig = await GetConnectionConfigurationByApplicationAndEnvironmentAsync(
                    normalizedApplicationName,
                    objEmployeeLoginValidationRequestModel.EnvironmentName);

                if (connectionConfig == null || string.IsNullOrWhiteSpace(connectionConfig.ConnectionString))
                    throw new Exception($"Connection string not found for Application = {objEmployeeLoginValidationRequestModel.ApplicationName}, Environment = {objEmployeeLoginValidationRequestModel.EnvironmentName}.");

                string query;
                string inputValue = objEmployeeLoginValidationRequestModel.LoginID.Trim();

                bool isTimePro = normalizedApplicationName.Equals("TimePro", StringComparison.OrdinalIgnoreCase);
                bool isProtectronHR = normalizedApplicationName.Equals("Protectron HR", StringComparison.OrdinalIgnoreCase);

                if (isTimePro)
                {
                    query = @"
SELECT Username, FullName, EmailID
FROM VUSYS_Users
WHERE EmailID = @EmailID";
                }
                else if(isProtectronHR)
                {
                    query = @"
SELECT login_id, empl_name, email_id
FROM empl_master
WHERE login_id = ?";
                }
                else
                {
                    query = @"
SELECT login_id, empl_name, email_id
FROM empl_master
WHERE login_id = ?";
                }

                await using var connection = DBObjectGenerationCommonHelper.CreateConnection(connectionConfig);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = query;

                var parameter = command.CreateParameter();
                parameter.Value = inputValue;

                if (isTimePro)
                {
                    parameter.ParameterName = "@EmailID";
                }
                else
                {
                    parameter.ParameterName = "login_id";
                }

                command.Parameters.Add(parameter);

                await using var reader = await command.ExecuteReaderAsync();

                if (reader != null && await reader.ReadAsync())
                {
                    string dbLoginId;
                    string employeeName;
                    string emailId;

                    if (isTimePro)
                    {
                        dbLoginId = reader["Username"]?.ToString()?.Trim() ?? string.Empty;
                        employeeName = reader["FullName"]?.ToString()?.Trim() ?? string.Empty;
                        emailId = reader["EmailID"]?.ToString()?.Trim() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(emailId))
                        {
                            return new EmployeeLoginValidationResultModel
                            {
                                IsValid = false,
                                ErrorMessage = "Invalid Email ID."
                            };
                        }
                    }
                    else
                    {
                        dbLoginId = reader["login_id"]?.ToString()?.Trim() ?? string.Empty;
                        employeeName = reader["empl_name"]?.ToString()?.Trim() ?? string.Empty;
                        emailId = reader["email_id"]?.ToString()?.Trim() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(dbLoginId))
                        {
                            return new EmployeeLoginValidationResultModel
                            {
                                IsValid = false,
                                ErrorMessage = "Invalid Login ID."
                            };
                        }
                    }

                    return new EmployeeLoginValidationResultModel
                    {
                        IsValid = true,
                        LoginId = dbLoginId,
                        EmployeeName = employeeName,
                        EmailId = emailId
                    };
                }

                return new EmployeeLoginValidationResultModel
                {
                    IsValid = false,
                    ErrorMessage = isTimePro ? "Invalid Email ID." : "Invalid Login ID."
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.ValidateLoginIdAsync()", ex.Message, false);
                throw new Exception($"Failed to validate Login ID. {ex.Message}");
            }
        }

        public async Task<string> SendDatabaseObjectGenerationOtpAsync(DeploymentSendOtpModel objDeploymentSendOtpModel)
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

                string otpEmailBodyTemplateFolderPath = CommonHelper.GetActualPath(_env, "Templates");
                string otpEmailBodyTemplatePath = otpEmailBodyTemplateFolderPath + "\\DatabaseObjectGenerationOTPEmailTemplate.html";

                using StreamReader rdrEmailBody = new StreamReader(otpEmailBodyTemplatePath);
                string txthtmlEmailBody = rdrEmailBody.ReadToEnd();
                string emailBodyTemplate = txthtmlEmailBody;

                emailBodyTemplate = emailBodyTemplate.Replace("#Name#", objDeploymentSendOtpModel.EmployeeName);
                emailBodyTemplate = emailBodyTemplate.Replace("#ApplicationName#", objDeploymentSendOtpModel.ApplicationName);
                emailBodyTemplate = emailBodyTemplate.Replace("#EnvironmentName#", objDeploymentSendOtpModel.Environment);
                emailBodyTemplate = emailBodyTemplate.Replace("#LoginId#", objDeploymentSendOtpModel.LoginId);
                emailBodyTemplate = emailBodyTemplate.Replace("#Database#", objDeploymentSendOtpModel.ApplicationName);
                emailBodyTemplate = emailBodyTemplate.Replace("#OTP#", objDeploymentSendOtpModel.Otp);
                emailBodyTemplate = emailBodyTemplate.Replace("#ExpiryMinutes#", objDeploymentSendOtpModel.ExpiryInMinutes);
                emailBodyTemplate = emailBodyTemplate.Replace("#CurrentYear#", DateTime.Now.Year.ToString());

                SendEmailRequestParam objSendEmailRequestParam = new SendEmailRequestParam();
                objSendEmailRequestParam.ToEmail = objDeploymentSendOtpModel.EmailId;
                objSendEmailRequestParam.EmailSubject = $"OTP for Database Object Generation - {objDeploymentSendOtpModel.ApplicationName} ({objDeploymentSendOtpModel.Environment})";
                objSendEmailRequestParam.EmailBody = emailBodyTemplate;

                var emailReturnResult = CommonHelper.SendEmailAPI(_env, objSendEmailRequestParam, string.Empty, string.Empty, false, false);
                string returnResult = ObjectToJson.ConvertDataSetToJson(emailReturnResult);
                JObject jObjectEmail = ApiHelper.GetDataInJObject(Util.OK(returnResult));
                string resultjObjectEmail = ApiHelper.CheckResByTableName(jObjectEmail, Helper.EMAIL_SENT_RESPONSE_TABLE_NAME, 1);

                string successMessage = $"A New OTP has been sent to {objDeploymentSendOtpModel.EmailId}.";
                string failureMessage = "Failed To Send an Email !!!";
                emailSentResponseReturnResult = failureMessage;

                if (string.Equals(resultjObjectEmail, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    var emailResponseData = jObjectEmail["data"]?[Helper.EMAIL_SENT_RESPONSE_TABLE_NAME]?.ToObject<SendEmailResponseParam[]>();
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.SendDatabaseObjectGenerationOtpAsync()", ex.Message, false);
                throw new Exception($"Failed to send database object generation OTP. {ex.Message}");
            }
            return emailSentResponseReturnResult;
        }

        private async Task<DbGeneratorConnectionConfig?> GetConnectionConfigurationByApplicationAndEnvironmentAsync(string applicationName, string environmentName)
        {
            try
            {
                var allConnections = await GetConnectionConfigurationsAsync();

                if (string.Equals(applicationName, "Protectron", StringComparison.OrdinalIgnoreCase) || string.Equals(applicationName, "Protectron HR", StringComparison.OrdinalIgnoreCase))
                {
                    applicationName = "Protectron";
                }

                return allConnections.FirstOrDefault(x =>
                    string.Equals(AppHubCommonHelper.NormalizeApplicationName(x.ApplicationName), AppHubCommonHelper.NormalizeApplicationName(applicationName), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.EnvironmentName?.Trim(), environmentName?.Trim(), StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.GetConnectionConfigurationByApplicationAndEnvironmentAsync()", ex.Message, false);
                throw new Exception($"Error while fetching connection configuration. {ex.Message}", ex);
            }
        }

        private async Task<List<DbObjectItem>> GetSqlServerObjectsAsync(DbConnection connection)
        {
            try
            {
                const string sql = @"
SELECT 
    x.SchemaName,
    x.ObjectName,
    x.ObjectType
FROM
(
    SELECT 
        s.name AS SchemaName,
        o.name AS ObjectName,
        'Table' AS ObjectType,
        1 AS SortOrder
    FROM sys.objects o
    INNER JOIN sys.schemas s 
        ON o.schema_id = s.schema_id
    WHERE o.type = 'U'
      AND o.is_ms_shipped = 0

    UNION ALL

    SELECT 
        s.name AS SchemaName,
        o.name AS ObjectName,
        'View' AS ObjectType,
        2 AS SortOrder
    FROM sys.objects o
    INNER JOIN sys.schemas s 
        ON o.schema_id = s.schema_id
    WHERE o.type = 'V'
      AND o.is_ms_shipped = 0

    UNION ALL

    SELECT 
        s.name AS SchemaName,
        o.name AS ObjectName,
        'Procedure' AS ObjectType,
        3 AS SortOrder
    FROM sys.objects o
    INNER JOIN sys.schemas s 
        ON o.schema_id = s.schema_id
    WHERE o.type IN ('P', 'PC')
      AND o.is_ms_shipped = 0

    UNION ALL

    SELECT 
        s.name AS SchemaName,
        o.name AS ObjectName,
        'Function' AS ObjectType,
        4 AS SortOrder
    FROM sys.objects o
    INNER JOIN sys.schemas s 
        ON o.schema_id = s.schema_id
    WHERE o.type IN ('FN', 'TF', 'IF')
      AND o.is_ms_shipped = 0

    UNION ALL

    SELECT DISTINCT
        sch.name AS SchemaName,
        i.name AS ObjectName,
        'Index' AS ObjectType,
        5 AS SortOrder
    FROM sys.indexes i
    INNER JOIN sys.tables t
        ON i.object_id = t.object_id
    INNER JOIN sys.schemas sch
        ON t.schema_id = sch.schema_id
    WHERE t.is_ms_shipped = 0
      AND i.name IS NOT NULL
      AND i.name <> ''
      AND i.is_primary_key = 0
      AND i.is_unique_constraint = 0
      AND i.type_desc <> 'HEAP'

    UNION ALL

    SELECT DISTINCT
        sch.name AS SchemaName,
        tr.name AS ObjectName,
        'Trigger' AS ObjectType,
        6 AS SortOrder
    FROM sys.triggers tr
    INNER JOIN sys.tables t
        ON tr.parent_id = t.object_id
    INNER JOIN sys.schemas sch
        ON t.schema_id = sch.schema_id
    WHERE t.is_ms_shipped = 0
      AND tr.is_ms_shipped = 0
      AND tr.name IS NOT NULL
      AND tr.name <> ''

    UNION ALL

    SELECT 
        s.name AS SchemaName,
        t.name AS ObjectName,
        'DataScript' AS ObjectType,
        7 AS SortOrder
    FROM sys.tables t
    INNER JOIN sys.schemas s
        ON t.schema_id = s.schema_id
    WHERE t.is_ms_shipped = 0
) x
ORDER BY x.SortOrder, x.SchemaName, x.ObjectName;";

                var results = new List<DbObjectItem>();

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new DbObjectItem
                    {
                        SchemaName = reader["SchemaName"]?.ToString() ?? string.Empty,
                        ObjectName = reader["ObjectName"]?.ToString() ?? string.Empty,
                        ObjectType = reader["ObjectType"]?.ToString() ?? string.Empty
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.GetSqlServerObjectsAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Server objects: {ex.Message}", ex);
            }
        }

        private async Task<List<DbObjectItem>> GetSqlAnywhereObjectsAsync(DbConnection connection)
        {
            try
            {
                const string sql = @"
SELECT 
    x.SchemaName,
    x.ObjectName,
    x.ObjectType
FROM
(
    SELECT 
        su.user_name AS SchemaName,
        st.table_name AS ObjectName,
        'Table' AS ObjectType,
        1 AS SortOrder
    FROM SYS.SYSTAB st
    INNER JOIN SYS.SYSUSER su 
        ON st.creator = su.user_id
    LEFT JOIN SYS.SYSVIEW sv
        ON st.object_id = sv.view_object_id
    WHERE sv.view_object_id IS NULL
      AND su.user_name NOT IN ('SYS')
      AND st.table_name IS NOT NULL

    UNION ALL

    SELECT 
        su.user_name AS SchemaName,
        st.table_name AS ObjectName,
        'View' AS ObjectType,
        2 AS SortOrder
    FROM SYS.SYSTAB st
    INNER JOIN SYS.SYSUSER su 
        ON st.creator = su.user_id
    INNER JOIN SYS.SYSVIEW sv
        ON st.object_id = sv.view_object_id
    WHERE su.user_name NOT IN ('SYS')
      AND st.table_name IS NOT NULL

    UNION ALL

    SELECT 
        su.user_name AS SchemaName,
        sp.proc_name AS ObjectName,
        CASE
            WHEN LOWER(COALESCE(CAST(sp.source AS LONG VARCHAR), COALESCE(sp.proc_defn, ''))) LIKE 'create function%'
              OR LOWER(COALESCE(CAST(sp.source AS LONG VARCHAR), COALESCE(sp.proc_defn, ''))) LIKE 'alter function%'
              OR LOWER(COALESCE(CAST(sp.source AS LONG VARCHAR), COALESCE(sp.proc_defn, ''))) LIKE '% function %'
            THEN 'Function'
            ELSE 'Procedure'
        END AS ObjectType,
        CASE
            WHEN LOWER(COALESCE(CAST(sp.source AS LONG VARCHAR), COALESCE(sp.proc_defn, ''))) LIKE 'create function%'
              OR LOWER(COALESCE(CAST(sp.source AS LONG VARCHAR), COALESCE(sp.proc_defn, ''))) LIKE 'alter function%'
              OR LOWER(COALESCE(CAST(sp.source AS LONG VARCHAR), COALESCE(sp.proc_defn, ''))) LIKE '% function %'
            THEN 4
            ELSE 3
        END AS SortOrder
    FROM SYS.SYSPROCEDURE sp
    INNER JOIN SYS.SYSUSER su
        ON sp.creator = su.user_id
    WHERE su.user_name NOT IN ('SYS')
      AND sp.proc_name IS NOT NULL

    UNION ALL

    SELECT DISTINCT
        su.user_name AS SchemaName,
        sfk.role AS ObjectName,
        'ForeignKey' AS ObjectType,
        5 AS SortOrder
    FROM SYS.SYSFOREIGNKEY sfk
    INNER JOIN SYS.SYSTAB st
        ON sfk.foreign_table_id = st.table_id
    INNER JOIN SYS.SYSUSER su
        ON st.creator = su.user_id
    WHERE su.user_name NOT IN ('SYS')
      AND sfk.role IS NOT NULL
      AND sfk.role <> ''

    UNION ALL

    SELECT DISTINCT
        su.user_name AS SchemaName,
        si.index_name AS ObjectName,
        'Index' AS ObjectType,
        6 AS SortOrder
    FROM SYS.SYSIDX si
    INNER JOIN SYS.SYSTAB st
        ON si.table_id = st.table_id
    INNER JOIN SYS.SYSUSER su
        ON st.creator = su.user_id
    WHERE su.user_name NOT IN ('SYS')
      AND si.index_name IS NOT NULL
      AND si.index_name <> ''
      AND si.index_category <> ''
      AND NOT EXISTS
      (
          SELECT 1
          FROM SYS.SYSFOREIGNKEY sfk
          WHERE sfk.foreign_table_id = si.table_id
            AND sfk.role = si.index_name
      )

    UNION ALL

    SELECT DISTINCT
        su.user_name AS SchemaName,
        trg.trigger_name AS ObjectName,
        'Trigger' AS ObjectType,
        7 AS SortOrder
    FROM SYS.SYSTRIGGER trg
    INNER JOIN SYS.SYSTAB st
        ON trg.table_id = st.table_id
    INNER JOIN SYS.SYSUSER su
        ON st.creator = su.user_id
    WHERE su.user_name NOT IN ('SYS')
      AND trg.trigger_name IS NOT NULL
      AND trg.trigger_name <> ''

    UNION ALL

    SELECT DISTINCT
        su.user_name AS SchemaName,
        st.table_name AS ObjectName,
        'DataScript' AS ObjectType,
        8 AS SortOrder
    FROM SYS.SYSTAB st
    INNER JOIN SYS.SYSUSER su
        ON st.creator = su.user_id
    LEFT JOIN SYS.SYSVIEW sv
        ON st.object_id = sv.view_object_id
    WHERE sv.view_object_id IS NULL
      AND su.user_name NOT IN ('SYS')
      AND st.table_name IS NOT NULL
) x
ORDER BY x.SortOrder, x.SchemaName, x.ObjectName;";

                var results = new List<DbObjectItem>();

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new DbObjectItem
                    {
                        SchemaName = reader["SchemaName"]?.ToString() ?? string.Empty,
                        ObjectName = reader["ObjectName"]?.ToString() ?? string.Empty,
                        ObjectType = reader["ObjectType"]?.ToString() ?? string.Empty
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.GetSqlAnywhereObjectsAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere objects: {ex.Message}", ex);
            }
        }

        private DbGeneratorConnectionConfig ParseConnectionMapping(DbConnectionMapping mapping)
        {
            try
            {
                var connectionString = mapping.ConnectionString?.Trim() ?? string.Empty;
                mapping.Application = AppHubCommonHelper.NormalizeApplicationName(mapping.Application?.Trim());
                string encryptDecryptKey = string.Empty;
                encryptDecryptKey = AppHubCommonHelper.GetEncryptDecryptKey(mapping.Application);
                string decryptedConnectionString = string.Empty;
                if (mapping.Application != "TimePro")
                {
                    decryptedConnectionString = DbConnectionHelper.GetDBDecryptedConnectionString(connectionString, encryptDecryptKey);
                }
                else
                {
                    decryptedConnectionString = connectionString;
                }
                var values = ParseConnectionStringParts(decryptedConnectionString);

                var config = new DbGeneratorConnectionConfig
                {
                    ApplicationName = mapping.Application?.Trim() ?? string.Empty,
                    EnvironmentName = mapping.Environment?.Trim() ?? string.Empty,
                    ConnectionString = decryptedConnectionString
                };

                if (IsSqlAnywhereConnectionString(decryptedConnectionString, values))
                {
                    config.DatabaseType = "SQL Anywhere 17";
                    config.OdbcDriverName = GetFirstValue(values, "Driver");
                    config.UserName = GetFirstValue(values, "Uid", "UID", "User ID", "UserID");
                    config.Password = GetFirstValue(values, "Pwd", "PWD", "Password");
                    config.ServerName = GetFirstValue(values, "Server", "ServerName", "Host", "Data Source", "DataSource");
                    config.DatabaseName = GetFirstValue(values, "Database", "DBN", "Initial Catalog", "InitialCatalog");
                    config.AuthenticationType = string.IsNullOrWhiteSpace(config.UserName) ? "Windows" : "Sql";
                }
                else
                {
                    config.DatabaseType = "SQL Server";
                    config.ServerName = GetFirstValue(values, "Data Source", "DataSource", "Server", "Address", "Addr", "Network Address");
                    config.DatabaseName = GetFirstValue(values, "Initial Catalog", "InitialCatalog", "Database");

                    config.UserName = GetFirstValue(values, "User ID", "UserID", "UID");
                    config.Password = GetFirstValue(values, "Password", "Pwd", "PWD");

                    bool hasSqlCredentials =
                        !string.IsNullOrWhiteSpace(config.UserName) &&
                        !string.IsNullOrWhiteSpace(config.Password);

                    var integratedValue = GetFirstValue(values, "Integrated Security", "Trusted_Connection", "Trusted Connection");
                    bool usesIntegratedSecurity = IsTrueValue(integratedValue);

                    if (hasSqlCredentials)
                    {
                        config.AuthenticationType = "Sql";
                    }
                    else if (usesIntegratedSecurity)
                    {
                        config.AuthenticationType = "Windows";
                    }
                    else
                    {
                        config.AuthenticationType = "Windows"; // fallback
                    }
                }

                return config;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.ParseConnectionMapping()", ex.Message, false);
                throw new Exception($"Error while parsing connection mapping: {ex.Message}", ex);
            }
        }

        private static Dictionary<string, string> ParseConnectionStringParts(string connectionString)
        {
            try
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return result;
                }

                var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (var rawPart in parts)
                {
                    var part = rawPart.Trim();
                    if (string.IsNullOrWhiteSpace(part))
                        continue;

                    var index = part.IndexOf('=');
                    if (index <= 0)
                        continue;

                    var key = part[..index].Trim();
                    var value = part[(index + 1)..].Trim();

                    if (!result.ContainsKey(key))
                    {
                        result[key] = value;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.ParseConnectionStringParts()", ex.Message, false);
                throw new Exception($"Error while parsing connection string parts: {ex.Message}", ex);
            }
        }

        private static string GetFirstValue(Dictionary<string, string> values, params string[] keys)
        {
            try
            {
                foreach (var key in keys)
                {
                    if (values.TryGetValue(key, out var value))
                    {
                        return value ?? string.Empty;
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.GetFirstValue()", ex.Message, false);
                throw new Exception($"Error while fetching first value from connection string parts: {ex.Message}", ex);
            }
        }

        private static bool IsSqlAnywhereConnectionString(string connectionString, Dictionary<string, string> values)
        {
            try
            {
                if (values.TryGetValue("Driver", out var driverValue) &&
                    driverValue.Contains("SQL Anywhere", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return connectionString.Contains("SQL Anywhere", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.IsSqlAnywhereConnectionString()", ex.Message, false);
                throw new Exception($"Error while checking SQL Anywhere connection string: {ex.Message}", ex);
            }
        }

        private static bool IsTrueValue(string? value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                    return false;

                return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                       || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                       || value.Equals("sspi", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.IsTrueValue()", ex.Message, false);
                throw new Exception($"Error while checking true value: {ex.Message}", ex);
            }
        }

        private static int GetEnvironmentOrder(string? environmentName)
        {
            try
            {
                return (environmentName ?? string.Empty).Trim().ToLowerInvariant() switch
                {
                    "development" => 1,
                    "dev" => 1,
                    "staging" => 2,
                    "stage" => 2,
                    "uat" => 3,
                    "production" => 4,
                    "prod" => 4,
                    _ => 99
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "DatabaseObjectGeneratorService.GetEnvironmentOrder()", ex.Message, false);
                throw new Exception($"Error while getting environment order: {ex.Message}", ex);
            }
        }
    }
}