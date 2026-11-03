using System.Data.Common;
using System.Data.Odbc;
using CommonLib;
using Microsoft.Data.SqlClient;
using VUSYSAppHub.DACHelper;
using VUSYSAppHub.Models;

namespace VUSYSAppHub.AppHubPageHelper
{
    public static class DBObjectGenerationCommonHelper
    {

        public static DbConnection CreateConnection(DbGeneratorConnectionConfig config)
        {
            try
            {
                if (IsSqlAnywhere(config))
                {
                    var odbcConnectionString = SQLAnyWhere17DBObjectGenerationHelper.BuildSqlAnywhereConnectionString(config);
                    return new OdbcConnection(odbcConnectionString);
                }

                var sqlServerConnectionString = SQLDBObjectGenerationHelper.BuildSqlServerConnectionString(config);
                return new SqlConnection(sqlServerConnectionString);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.CreateConnection()", ex.Message, false);
                throw new Exception($"Error while creating database connection: {ex.Message}", ex);
            }
        }

        public static bool IsSqlAnywhere(DbGeneratorConnectionConfig config)
        {
            try
            {
                return (config.DatabaseType ?? string.Empty).Contains("Anywhere", StringComparison.OrdinalIgnoreCase)
                       || (config.OdbcDriverName ?? string.Empty).Contains("SQL Anywhere", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.IsSqlAnywhere()", ex.Message, false);
                throw new Exception($"Error while checking database type for SQL Anywhere: {ex.Message}", ex);
            }
        }

        public static string ReplaceCreateWithAlter(string definition, string objectKeyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(definition))
                    return string.Empty;

                var normalizedKeyword = objectKeyword.Trim();

                var keywordVariants = new List<string>();

                if (normalizedKeyword.Equals("PROCEDURE", StringComparison.OrdinalIgnoreCase) ||
                    normalizedKeyword.Equals("PROC", StringComparison.OrdinalIgnoreCase))
                {
                    keywordVariants.Add("PROCEDURE");
                    keywordVariants.Add("PROC");
                }
                else
                {
                    keywordVariants.Add(normalizedKeyword);
                }

                foreach (var keyword in keywordVariants)
                {
                    var patterns = new[]
                    {
                        $"CREATE OR REPLACE {keyword}",
                        $"CREATE OR ALTER {keyword}",
                        $"CREATE {keyword}",
                        $"ALTER {keyword}",
                        $"create or replace {keyword.ToLowerInvariant()}",
                        $"create or alter {keyword.ToLowerInvariant()}",
                        $"create {keyword.ToLowerInvariant()}",
                        $"alter {keyword.ToLowerInvariant()}"
                    };

                    foreach (var pattern in patterns)
                    {
                        var index = definition.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                        if (index >= 0)
                        {
                            return definition.Remove(index, pattern.Length)
                                             .Insert(index, $"ALTER {normalizedKeyword}");
                        }
                    }
                }

                return definition;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.ReplaceCreateWithAlter()", ex.Message, false);
                throw new Exception($"Error while replacing CREATE with ALTER for object keyword {objectKeyword}: {ex.Message}", ex);
            }
        }

        public static string EscapeSqlUnicodeLiteral(string value)
        {
            try
            {
                return (value ?? string.Empty).Replace("'", "''");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral()", ex.Message, false);
                throw new Exception($"Error while escaping SQL unicode literal: {ex.Message}", ex);
            }
        }

        public static async Task<string?> TryGetStoredOriginalSourceAsync(IWebHostEnvironment _env, DbGeneratorConnectionConfig config, DbObjectItem item)
        {
            try
            {
                var possiblePaths = BuildStoredSourceCandidatePaths(_env, config, item);

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        var content = await File.ReadAllTextAsync(path);
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            return content;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.TryGetStoredOriginalSourceAsync()", ex.Message, false);
                throw new Exception($"Error while fetching stored original source for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static List<string> BuildStoredSourceCandidatePaths(IWebHostEnvironment _env, DbGeneratorConnectionConfig config, DbObjectItem item)
        {
            try
            {
                var root = Path.Combine(_env.ContentRootPath, "Configurations", "DbObjectOriginalSource");

                var application = SanitizePathSegment(config.ApplicationName);
                var environment = SanitizePathSegment(config.EnvironmentName);
                var databaseType = SanitizePathSegment(config.DatabaseType);
                var database = SanitizePathSegment(config.DatabaseName);
                var schema = SanitizePathSegment(item.SchemaName);
                var objectType = SanitizePathSegment(item.ObjectType);
                var objectName = SanitizePathSegment(item.ObjectName);

                var candidates = new List<string>
                {
                    Path.Combine(root, application, environment, databaseType, database, $"{schema}.{objectType}.{objectName}.sql"),
                    Path.Combine(root, application, environment, database, $"{schema}.{objectType}.{objectName}.sql"),
                    Path.Combine(root, application, environment, $"{schema}.{objectType}.{objectName}.sql"),
                    Path.Combine(root, application, $"{schema}.{objectType}.{objectName}.sql"),
                    Path.Combine(root, $"{schema}.{objectType}.{objectName}.sql"),
                    Path.Combine(root, application, environment, databaseType, database, objectType, $"{schema}.{objectName}.sql"),
                    Path.Combine(root, application, environment, database, objectType, $"{schema}.{objectName}.sql"),
                    Path.Combine(root, application, environment, objectType, $"{schema}.{objectName}.sql"),
                    Path.Combine(root, application, objectType, $"{schema}.{objectName}.sql"),
                    Path.Combine(root, objectType, $"{schema}.{objectName}.sql"),
                    Path.Combine(root, application, environment, databaseType, database, schema, $"{objectName}.sql"),
                    Path.Combine(root, application, environment, database, schema, $"{objectName}.sql"),
                    Path.Combine(root, application, environment, schema, $"{objectName}.sql"),
                    Path.Combine(root, schema, $"{objectName}.sql")
                };

                return candidates
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.BuildStoredSourceCandidatePaths()", ex.Message, false);
                throw new Exception($"Error while building stored source candidate paths for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static string NormalizeStoredSource(string definition)
        {
            try
            {
                var value = (definition ?? string.Empty).Trim();

                if (value.StartsWith("\uFEFF", StringComparison.Ordinal))
                {
                    value = value.TrimStart('\uFEFF');
                }

                while (value.EndsWith(";"))
                {
                    value = value[..^1].TrimEnd();
                }

                return value;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.NormalizeStoredSource()", ex.Message, false);
                throw new Exception($"Error while normalizing stored source: {ex.Message}", ex);
            }
        }

        public static string SanitizePathSegment(string? value)
        {
            try
            {
                var text = string.IsNullOrWhiteSpace(value) ? "_" : value.Trim();
                foreach (var invalid in Path.GetInvalidFileNameChars())
                {
                    text = text.Replace(invalid, '_');
                }

                return text;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.SanitizePathSegment()", ex.Message, false);
                throw new Exception($"Error while sanitizing path segment: {ex.Message}", ex);
            }
        }

        public static string EscapeSqlLiteral(string value)
        {
            try
            {
                return (value ?? string.Empty).Replace("'", "''");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.EscapeSqlLiteral()", ex.Message, false);
                throw new Exception($"Error while escaping SQL literal: {ex.Message}", ex);
            }
        }

        public static List<DbObjectItem> ApplyOrdering(List<DbObjectItem> selectedObjects, ScriptGenerationOptions options)
        {
            try
            {
                if (!options.GenerateInDependencyOrder)
                {
                    return selectedObjects
                        .OrderBy(x => x.ObjectType)
                        .ThenBy(x => x.SchemaName)
                        .ThenBy(x => x.ObjectName)
                        .ToList();
                }

                return selectedObjects
                    .OrderBy(x => GetDependencyWeight(x.ObjectType))
                    .ThenBy(x => x.SchemaName)
                    .ThenBy(x => x.ObjectName)
                    .ToList();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.ApplyOrdering()", ex.Message, false);
                throw new Exception($"Error while applying object ordering: {ex.Message}", ex);
            }
        }

        public static int GetDependencyWeight(string objectType)
        {
            try
            {
                return objectType switch
                {
                    "Table" => 1,
                    "View" => 2,
                    "Procedure" => 3,
                    "Function" => 4,
                    "Index" => 5,
                    "Trigger" => 6,
                    "DataScript" => 7,
                    _ => 99
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "DBObjectGenerationCommonHelper.GetDependencyWeight()", ex.Message, false);
                throw new Exception($"Error while getting dependency weight for object type {objectType}: {ex.Message}", ex);
            }
        }
    }
}