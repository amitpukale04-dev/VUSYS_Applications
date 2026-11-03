using System.Data.Common;
using System.Globalization;
using System.Text;
using CommonLib;
using Microsoft.Data.SqlClient;
using VUSYSAppHub.DACHelper;
using VUSYSAppHub.Models;

namespace VUSYSAppHub.AppHubPageHelper
{
    public static class SQLDBObjectGenerationHelper
    {
        public static string BuildSqlServerConnectionString(DbGeneratorConnectionConfig config)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(config.ConnectionString))
                {
                    return config.ConnectionString;
                }

                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = config.ServerName,
                    InitialCatalog = config.DatabaseName,
                    TrustServerCertificate = true,
                    ConnectTimeout = 15
                };

                var authenticationType = config.AuthenticationType ?? string.Empty;

                if (authenticationType.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
                    authenticationType.Equals("Integrated", StringComparison.OrdinalIgnoreCase) ||
                    authenticationType.Equals("Integrated Security", StringComparison.OrdinalIgnoreCase))
                {
                    builder.IntegratedSecurity = true;
                }
                else
                {
                    builder.UserID = config.UserName ?? string.Empty;
                    builder.Password = config.Password ?? string.Empty;
                    builder.IntegratedSecurity = false;
                }

                return builder.ConnectionString;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.BuildSqlServerConnectionString()", ex.Message, false);
                throw new Exception($"Error while building SQL Server connection string: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlServerObjectScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                if (item.ObjectType.StartsWith("DataScript", StringComparison.OrdinalIgnoreCase))
                {
                    return await GenerateSqlServerDataScriptAsync(connection, item, options);
                }
                return item.ObjectType switch
                {
                    "Table" => await GenerateSqlServerTableScriptAsync(_env, connection, config, item, options),
                    "View" => await GenerateSqlServerViewScriptAsync(_env, connection, config, item, options),
                    "Procedure" => await GenerateSqlServerProcedureScriptAsync(_env, connection, config, item, options),
                    "Function" => await GenerateSqlServerFunctionScriptAsync(_env, connection, config, item, options),
                    "Index" => await GenerateSqlServerIndexScriptAsync(_env, connection, config, item, options),
                    "Trigger" => await GenerateSqlServerTriggerScriptAsync(_env, connection, config, item, options),
                    _ => $"-- Unsupported object: [{item.SchemaName}].[{item.ObjectName}]"
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GenerateSqlServerObjectScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Server object script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlServerTableScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var storedSource = await DBObjectGenerationCommonHelper.TryGetStoredOriginalSourceAsync(_env, config, item);

                string tableScript;
                if (!string.IsNullOrWhiteSpace(storedSource))
                {
                    tableScript = BuildSqlServerStoredTableScript(item, storedSource, options);
                }
                else
                {
                    const string sql = @"
SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.NUMERIC_PRECISION,
    c.NUMERIC_SCALE,
    c.IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = @SchemaName
  AND c.TABLE_NAME = @TableName
ORDER BY c.ORDINAL_POSITION;";

                    var columns = new List<string>();

                    await using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;

                    var p1 = cmd.CreateParameter();
                    p1.ParameterName = "@SchemaName";
                    p1.Value = item.SchemaName;
                    cmd.Parameters.Add(p1);

                    var p2 = cmd.CreateParameter();
                    p2.ParameterName = "@TableName";
                    p2.Value = item.ObjectName;
                    cmd.Parameters.Add(p2);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader["COLUMN_NAME"]?.ToString() ?? string.Empty;
                        var dataType = reader["DATA_TYPE"]?.ToString() ?? string.Empty;
                        var maxLength = reader["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["CHARACTER_MAXIMUM_LENGTH"]);
                        var precision = reader["NUMERIC_PRECISION"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["NUMERIC_PRECISION"]);
                        var scale = reader["NUMERIC_SCALE"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["NUMERIC_SCALE"]);
                        var isNullable = (reader["IS_NULLABLE"]?.ToString() ?? "YES").Equals("YES", StringComparison.OrdinalIgnoreCase);

                        columns.Add($"    [{columnName}] {BuildSqlServerDataType(dataType, maxLength, precision, scale)} {(isNullable ? "NULL" : "NOT NULL")}");
                    }

                    var sb = new StringBuilder();

                    sb.AppendLine($"-- Table: [{item.SchemaName}].[{item.ObjectName}]");
                    sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{item.SchemaName}].[{item.ObjectName}]') AND type IN (N'U'))");
                    sb.AppendLine("BEGIN");
                    sb.AppendLine($"    CREATE TABLE [{item.SchemaName}].[{item.ObjectName}]");
                    sb.AppendLine("    (");
                    sb.AppendLine(string.Join("," + Environment.NewLine, columns));
                    sb.AppendLine("    )");
                    sb.AppendLine("END");
                    sb.AppendLine("GO");
                    sb.AppendLine();

                    tableScript = sb.ToString();
                }

                var finalBuilder = new StringBuilder();
                finalBuilder.Append(tableScript);

                var indexes = await GetSqlServerIndexDefinitionsForTableAsync(connection, item.SchemaName, item.ObjectName);
                foreach (var index in indexes)
                {
                    AppendSqlServerIndexScriptBlock(finalBuilder, index);
                    finalBuilder.AppendLine();
                }

                var triggers = await GetSqlServerTriggerDefinitionsForTableAsync(connection, item.SchemaName, item.ObjectName);
                foreach (var trigger in triggers)
                {
                    AppendSqlServerTriggerScriptBlock(finalBuilder, trigger);
                    finalBuilder.AppendLine();
                }

                return finalBuilder.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GenerateSqlServerTableScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Server table script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlServerViewScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var definition = await GetSqlServerStoredSourceOrModuleDefinitionAsync(_env, connection, config, item, "V");
                var escapedCreateDefinition = DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral(definition.Trim());

                var alterDefinition = DBObjectGenerationCommonHelper.ReplaceCreateWithAlter(definition, "VIEW");
                var escapedAlterDefinition = DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral(alterDefinition.Trim());

                var sb = new StringBuilder();

                sb.AppendLine($"-- View: [{item.SchemaName}].[{item.ObjectName}]");
                sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[{item.SchemaName}].[{item.ObjectName}]'))");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    EXEC dbo.sp_executesql @statement = N'{escapedCreateDefinition}'");
                sb.AppendLine("END");
                sb.AppendLine("ELSE");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    EXEC dbo.sp_executesql @statement = N'{escapedAlterDefinition}'");
                sb.AppendLine("END");
                sb.AppendLine("GO");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GenerateSqlServerViewScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Server view script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlServerProcedureScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var definition = await GetSqlServerStoredSourceOrModuleDefinitionAsync(_env, connection, config, item, "P", "PC");
                var alterDefinition = DBObjectGenerationCommonHelper.ReplaceCreateWithAlter(definition, "PROCEDURE");

                var sb = new StringBuilder();

                sb.AppendLine($"-- Procedure: [{item.SchemaName}].[{item.ObjectName}]");
                sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{item.SchemaName}].[{item.ObjectName}]') AND type IN (N'P', N'PC'))");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [{item.SchemaName}].[{item.ObjectName}] AS'");
                sb.AppendLine("END");
                sb.AppendLine("GO");
                sb.AppendLine(alterDefinition.Trim());
                sb.AppendLine("GO");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GenerateSqlServerProcedureScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Server procedure script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlServerFunctionScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var definition = await GetSqlServerStoredSourceOrModuleDefinitionAsync(_env, connection, config, item, "FN", "TF", "IF");

                if (string.IsNullOrWhiteSpace(definition))
                {
                    return $"-- Function: [{item.SchemaName}].[{item.ObjectName}]{Environment.NewLine}-- Definition not found.{Environment.NewLine}";
                }

                var normalizedDefinition = definition.Trim();

                // Convert CREATE → ALTER
                var alterDefinition = DBObjectGenerationCommonHelper.ReplaceCreateWithAlter(normalizedDefinition, "FUNCTION");

                // Escape for dynamic execution
                var escapedCreateDefinition = DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral(normalizedDefinition);
                var escapedAlterDefinition = DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral(alterDefinition.Trim());

                var sb = new StringBuilder();

                sb.AppendLine($"-- Function: [{item.SchemaName}].[{item.ObjectName}]");

                sb.AppendLine($"IF NOT EXISTS (");
                sb.AppendLine($"    SELECT 1 FROM sys.objects o");
                sb.AppendLine($"    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id");
                sb.AppendLine($"    WHERE s.name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}'");
                sb.AppendLine($"      AND o.name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}'");
                sb.AppendLine($"      AND o.type IN ('FN','TF','IF')");
                sb.AppendLine($")");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    EXEC dbo.sp_executesql @statement = N'{escapedCreateDefinition}'");
                sb.AppendLine("END");
                sb.AppendLine("ELSE");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    EXEC dbo.sp_executesql @statement = N'{escapedAlterDefinition}'");
                sb.AppendLine("END");
                sb.AppendLine("GO");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GenerateSqlServerFunctionScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Server function script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlServerIndexScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var storedSource = await DBObjectGenerationCommonHelper.TryGetStoredOriginalSourceAsync(_env, config, item);
                SqlServerIndexDefinitionResult? indexDefinition = null;

                if (!string.IsNullOrWhiteSpace(storedSource))
                {
                    indexDefinition = await GetSqlServerIndexParentInfoAsync(connection, item.SchemaName, item.ObjectName);
                    if (indexDefinition == null)
                    {
                        return BuildSqlServerStoredIndexScript(item, storedSource);
                    }

                    indexDefinition.CreateStatement = DBObjectGenerationCommonHelper.NormalizeStoredSource(storedSource);
                }
                else
                {
                    indexDefinition = await GetSqlServerIndexDefinitionAsync(connection, item.SchemaName, item.ObjectName);
                }

                if (indexDefinition == null || string.IsNullOrWhiteSpace(indexDefinition.CreateStatement))
                {
                    return $"-- Index: [{item.SchemaName}].[{item.ObjectName}]{Environment.NewLine}-- Definition not found.{Environment.NewLine}";
                }

                var sb = new StringBuilder();
                AppendSqlServerIndexScriptBlock(sb, indexDefinition);
                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GenerateSqlServerIndexScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Server index script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlServerTriggerScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var storedSource = await DBObjectGenerationCommonHelper.TryGetStoredOriginalSourceAsync(_env, config, item);
                SqlServerTriggerDefinitionResult? triggerDefinition = null;

                if (!string.IsNullOrWhiteSpace(storedSource))
                {
                    triggerDefinition = await GetSqlServerTriggerParentInfoAsync(connection, item.SchemaName, item.ObjectName);
                    if (triggerDefinition == null)
                    {
                        return BuildSqlServerStoredTriggerScript(item, storedSource);
                    }

                    triggerDefinition.Definition = DBObjectGenerationCommonHelper.NormalizeStoredSource(storedSource);
                }
                else
                {
                    triggerDefinition = await GetSqlServerTriggerDefinitionAsync(connection, item.SchemaName, item.ObjectName);
                }

                if (triggerDefinition == null || string.IsNullOrWhiteSpace(triggerDefinition.Definition))
                {
                    return $"-- Trigger: [{item.SchemaName}].[{item.ObjectName}]{Environment.NewLine}-- Definition not found.{Environment.NewLine}";
                }

                var sb = new StringBuilder();
                AppendSqlServerTriggerScriptBlock(sb, triggerDefinition);
                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GenerateSqlServerTriggerScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Server trigger script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlServerDataScriptAsync(DbConnection connection, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"-- DataScript: [{item.SchemaName}].[{item.ObjectName}]");
                sb.AppendLine("-- Source     : Live table data");

                if (!options.IncludeInsert && !options.IncludeUpdate && !options.IncludeDelete)
                {
                    sb.AppendLine("-- No operations selected (Insert/Update/Delete).");
                    sb.AppendLine("GO");
                    return sb.ToString();
                }

                var safeSchema = EscapeSqlServerIdentifier(item.SchemaName);
                var safeTable = EscapeSqlServerIdentifier(item.ObjectName);

                var primaryKeyColumns = await GetSqlServerPrimaryKeyColumnsAsync(connection, item.SchemaName, item.ObjectName);

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM {safeSchema}.{safeTable};";

                await using var reader = await cmd.ExecuteReaderAsync();

                var columnNames = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    columnNames.Add(reader.GetName(i));
                }

                if (columnNames.Count == 0)
                {
                    sb.AppendLine("-- No columns found.");
                    sb.AppendLine("GO");
                    return sb.ToString();
                }

                var safeColumnList = string.Join(", ", columnNames.Select(EscapeSqlServerIdentifier));
                var nonPrimaryKeyColumns = columnNames
                    .Where(x => !primaryKeyColumns.Contains(x, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                var hasRows = false;

                while (await reader.ReadAsync())
                {
                    hasRows = true;

                    var rowValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        rowValues[columnNames[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }

                    if (options.IncludeInsert)
                    {
                        var insertValues = columnNames
                            .Select(col => ConvertToSqlServerLiteral(rowValues[col]))
                            .ToList();

                        sb.AppendLine($"INSERT INTO {safeSchema}.{safeTable} ({safeColumnList}) VALUES ({string.Join(", ", insertValues)});");
                    }

                    if (options.IncludeUpdate)
                    {
                        if (primaryKeyColumns.Count == 0)
                        {
                            sb.AppendLine("-- UPDATE skipped: Primary key not found.");
                        }
                        else if (nonPrimaryKeyColumns.Count == 0)
                        {
                            sb.AppendLine("-- UPDATE skipped: No non-primary-key columns available to update.");
                        }
                        else
                        {
                            var setClauses = nonPrimaryKeyColumns
                                .Select(col => $"{EscapeSqlServerIdentifier(col)} = {ConvertToSqlServerLiteral(rowValues[col])}")
                                .ToList();

                            var whereClause = BuildSqlServerWhereClause(primaryKeyColumns, rowValues);

                            sb.AppendLine($"UPDATE {safeSchema}.{safeTable}");
                            sb.AppendLine($"SET {string.Join(", ", setClauses)}");
                            sb.AppendLine($"WHERE {whereClause};");
                        }
                    }

                    if (options.IncludeDelete)
                    {
                        if (primaryKeyColumns.Count == 0)
                        {
                            sb.AppendLine("-- DELETE skipped: Primary key not found.");
                        }
                        else
                        {
                            var whereClause = BuildSqlServerWhereClause(primaryKeyColumns, rowValues);

                            sb.AppendLine($"DELETE FROM {safeSchema}.{safeTable}");
                            sb.AppendLine($"WHERE {whereClause};");
                        }
                    }
                }

                if (!hasRows)
                {
                    sb.AppendLine("-- No data found.");
                }

                sb.AppendLine("GO");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GenerateSqlServerDataScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Server data script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static async Task<string> GetSqlServerStoredSourceOrModuleDefinitionAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, params string[] objectTypes)
        {
            try
            {
                var storedSource = await DBObjectGenerationCommonHelper.TryGetStoredOriginalSourceAsync(_env, config, item);
                if (!string.IsNullOrWhiteSpace(storedSource))
                {
                    return DBObjectGenerationCommonHelper.NormalizeStoredSource(storedSource);
                }

                var typeFilter = string.Empty;
                if (objectTypes != null && objectTypes.Length > 0)
                {
                    typeFilter = $" AND o.type IN ({string.Join(", ", objectTypes.Select(x => $"'{DBObjectGenerationCommonHelper.EscapeSqlLiteral(x)}'"))})";
                }

                var sql = $@"
SELECT m.definition
FROM sys.sql_modules m
INNER JOIN sys.objects o ON m.object_id = o.object_id
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE s.name = @SchemaName
  AND o.name = @ObjectName
  {typeFilter};";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@SchemaName";
                p1.Value = item.SchemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@ObjectName";
                p2.Value = item.ObjectName;
                cmd.Parameters.Add(p2);

                return (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GetSqlServerStoredSourceOrModuleDefinitionAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Server stored source or module definition for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static string BuildSqlServerStoredTableScript(DbObjectItem item, string definition, ScriptGenerationOptions options)
        {
            try
            {
                var normalizedDefinition = DBObjectGenerationCommonHelper.NormalizeStoredSource(definition);
                var sb = new StringBuilder();

                sb.AppendLine($"-- Table: [{item.SchemaName}].[{item.ObjectName}]");
                sb.AppendLine("-- Source     : Stored original source");
                sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{item.SchemaName}].[{item.ObjectName}]') AND type IN (N'U'))");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    EXEC dbo.sp_executesql @statement = N'{DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral(normalizedDefinition)}'");
                sb.AppendLine("END");
                sb.AppendLine("GO");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.BuildSqlServerStoredTableScript()", ex.Message, false);
                throw new Exception($"Error while building SQL Server stored table script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static string BuildSqlServerStoredIndexScript(DbObjectItem item, string definition)
        {
            try
            {
                var normalizedDefinition = DBObjectGenerationCommonHelper.NormalizeStoredSource(definition);
                var sb = new StringBuilder();

                sb.AppendLine($"-- Index: [{item.SchemaName}].[{item.ObjectName}]");
                sb.AppendLine("-- Source     : Stored original source");
                sb.AppendLine($"EXEC dbo.sp_executesql @statement = N'{DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral(normalizedDefinition)}'");
                sb.AppendLine("GO");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.BuildSqlServerStoredIndexScript()", ex.Message, false);
                throw new Exception($"Error while building SQL Server stored index script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static string BuildSqlServerStoredTriggerScript(DbObjectItem item, string definition)
        {
            try
            {
                var normalizedDefinition = DBObjectGenerationCommonHelper.NormalizeStoredSource(definition);
                var sb = new StringBuilder();

                sb.AppendLine($"-- Trigger: [{item.SchemaName}].[{item.ObjectName}]");
                sb.AppendLine("-- Source     : Stored original source");
                sb.AppendLine($"EXEC dbo.sp_executesql @statement = N'{DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral(normalizedDefinition)}'");
                sb.AppendLine("GO");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.BuildSqlServerStoredTriggerScript()", ex.Message, false);
                throw new Exception($"Error while building SQL Server stored trigger script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        public static string BuildSqlServerDataType(string dataType, int? maxLength, int? precision, int? scale)
        {
            try
            {
                return dataType.ToLowerInvariant() switch
                {
                    "varchar" or "nvarchar" or "char" or "nchar" =>
                        maxLength == -1 ? $"{dataType}(MAX)" : $"{dataType}({maxLength ?? 0})",

                    "decimal" or "numeric" =>
                        $"{dataType}({precision ?? 18},{scale ?? 0})",

                    "varbinary" or "binary" =>
                        maxLength == -1 ? $"{dataType}(MAX)" : $"{dataType}({maxLength ?? 0})",

                    _ => dataType
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.BuildSqlServerDataType()", ex.Message, false);
                throw new Exception($"Error while building SQL Server data type: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlServerModuleScriptAsync(DbConnection connection, DbObjectItem item, ScriptGenerationOptions options, string objectKeyword)
        {
            try
            {
                const string sql = @"
SELECT m.definition
FROM sys.sql_modules m
INNER JOIN sys.objects o ON m.object_id = o.object_id
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE s.name = @SchemaName
  AND o.name = @ObjectName;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@SchemaName";
                p1.Value = item.SchemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@ObjectName";
                p2.Value = item.ObjectName;
                cmd.Parameters.Add(p2);

                var definition = (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;

                var sb = new StringBuilder();
                sb.AppendLine($"-- {item.ObjectType}: [{item.SchemaName}].[{item.ObjectName}]");
                sb.AppendLine(definition.Trim());
                sb.AppendLine("GO");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GenerateSqlServerModuleScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Server module script for [{item.SchemaName}].[{item.ObjectName}]: {ex.Message}", ex);
            }
        }

        private static async Task<List<SqlServerIndexDefinitionResult>> GetSqlServerIndexDefinitionsForTableAsync(DbConnection connection, string schemaName, string tableName)
        {
            try
            {
                var results = new List<SqlServerIndexDefinitionResult>();

                const string sql = @"
WITH IndexBase AS
(
    SELECT
        i.object_id,
        i.index_id,
        i.name AS IndexName,
        s.name AS SchemaName,
        t.name AS TableName,
        i.is_unique,
        i.type_desc,
        i.has_filter,
        i.filter_definition
    FROM sys.indexes i
    INNER JOIN sys.tables t
        ON i.object_id = t.object_id
    INNER JOIN sys.schemas s
        ON t.schema_id = s.schema_id
    WHERE s.name = @SchemaName
      AND t.name = @TableName
      AND i.name IS NOT NULL
      AND i.is_primary_key = 0
      AND i.is_unique_constraint = 0
      AND i.type_desc <> 'HEAP'
)
SELECT
    ib.SchemaName,
    ib.TableName,
    ib.IndexName,
    'CREATE ' +
    CASE WHEN ib.is_unique = 1 THEN 'UNIQUE ' ELSE '' END +
    CASE
        WHEN ib.type_desc = 'CLUSTERED' THEN 'CLUSTERED '
        WHEN ib.type_desc = 'NONCLUSTERED' THEN 'NONCLUSTERED '
        ELSE ''
    END +
    'INDEX ' + QUOTENAME(ib.IndexName) + ' ON ' + QUOTENAME(ib.SchemaName) + '.' + QUOTENAME(ib.TableName) +
    ' (' + kc.KeyColumns + ')' +
    CASE 
        WHEN ic.IncludedColumns IS NOT NULL AND ic.IncludedColumns <> '' 
            THEN ' INCLUDE (' + ic.IncludedColumns + ')' 
        ELSE '' 
    END +
    CASE 
        WHEN ib.has_filter = 1 AND ib.filter_definition IS NOT NULL AND ib.filter_definition <> '' 
            THEN ' WHERE ' + ib.filter_definition 
        ELSE '' 
    END AS CreateStatement
FROM IndexBase ib
CROSS APPLY
(
    SELECT STUFF(
    (
        SELECT ', ' + QUOTENAME(c.name) + CASE WHEN icx.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END
        FROM sys.index_columns icx
        INNER JOIN sys.columns c
            ON icx.object_id = c.object_id
           AND icx.column_id = c.column_id
        WHERE icx.object_id = ib.object_id
          AND icx.index_id = ib.index_id
          AND icx.key_ordinal > 0
        ORDER BY icx.key_ordinal
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS KeyColumns
) kc
CROSS APPLY
(
    SELECT STUFF(
    (
        SELECT ', ' + QUOTENAME(c.name)
        FROM sys.index_columns icx
        INNER JOIN sys.columns c
            ON icx.object_id = c.object_id
           AND icx.column_id = c.column_id
        WHERE icx.object_id = ib.object_id
          AND icx.index_id = ib.index_id
          AND icx.is_included_column = 1
        ORDER BY c.column_id
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS IncludedColumns
) ic
ORDER BY ib.IndexName;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@SchemaName";
                p1.Value = schemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@TableName";
                p2.Value = tableName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new SqlServerIndexDefinitionResult
                    {
                        SchemaName = reader["SchemaName"]?.ToString() ?? string.Empty,
                        TableName = reader["TableName"]?.ToString() ?? string.Empty,
                        IndexName = reader["IndexName"]?.ToString() ?? string.Empty,
                        CreateStatement = reader["CreateStatement"]?.ToString() ?? string.Empty
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GetSqlServerIndexDefinitionsForTableAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Server index definitions for table [{schemaName}].[{tableName}]: {ex.Message}", ex);
            }
        }

        private static async Task<SqlServerIndexDefinitionResult?> GetSqlServerIndexDefinitionAsync(DbConnection connection, string schemaName, string indexName)
        {
            try
            {
                const string sql = @"
WITH IndexBase AS
(
    SELECT
        i.object_id,
        i.index_id,
        i.name AS IndexName,
        s.name AS SchemaName,
        t.name AS TableName,
        i.is_unique,
        i.type_desc,
        i.has_filter,
        i.filter_definition
    FROM sys.indexes i
    INNER JOIN sys.tables t
        ON i.object_id = t.object_id
    INNER JOIN sys.schemas s
        ON t.schema_id = s.schema_id
    WHERE s.name = @SchemaName
      AND i.name = @IndexName
      AND i.is_primary_key = 0
      AND i.is_unique_constraint = 0
      AND i.type_desc <> 'HEAP'
)
SELECT TOP 1
    ib.SchemaName,
    ib.TableName,
    ib.IndexName,
    'CREATE ' +
    CASE WHEN ib.is_unique = 1 THEN 'UNIQUE ' ELSE '' END +
    CASE
        WHEN ib.type_desc = 'CLUSTERED' THEN 'CLUSTERED '
        WHEN ib.type_desc = 'NONCLUSTERED' THEN 'NONCLUSTERED '
        ELSE ''
    END +
    'INDEX ' + QUOTENAME(ib.IndexName) + ' ON ' + QUOTENAME(ib.SchemaName) + '.' + QUOTENAME(ib.TableName) +
    ' (' + kc.KeyColumns + ')' +
    CASE 
        WHEN ic.IncludedColumns IS NOT NULL AND ic.IncludedColumns <> '' 
            THEN ' INCLUDE (' + ic.IncludedColumns + ')' 
        ELSE '' 
    END +
    CASE 
        WHEN ib.has_filter = 1 AND ib.filter_definition IS NOT NULL AND ib.filter_definition <> '' 
            THEN ' WHERE ' + ib.filter_definition 
        ELSE '' 
    END AS CreateStatement
FROM IndexBase ib
CROSS APPLY
(
    SELECT STUFF(
    (
        SELECT ', ' + QUOTENAME(c.name) + 
               CASE WHEN icx.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END
        FROM sys.index_columns icx
        INNER JOIN sys.columns c
            ON icx.object_id = c.object_id
           AND icx.column_id = c.column_id
        WHERE icx.object_id = ib.object_id
          AND icx.index_id = ib.index_id
          AND icx.key_ordinal > 0
        ORDER BY icx.key_ordinal
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS KeyColumns
) kc
CROSS APPLY
(
    SELECT STUFF(
    (
        SELECT ', ' + QUOTENAME(c.name)
        FROM sys.index_columns icx
        INNER JOIN sys.columns c
            ON icx.object_id = c.object_id
           AND icx.column_id = c.column_id
        WHERE icx.object_id = ib.object_id
          AND icx.index_id = ib.index_id
          AND icx.is_included_column = 1
        ORDER BY c.column_id
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS IncludedColumns
) ic;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@SchemaName";
                p1.Value = schemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@IndexName";
                p2.Value = indexName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SqlServerIndexDefinitionResult
                    {
                        SchemaName = reader["SchemaName"]?.ToString() ?? string.Empty,
                        TableName = reader["TableName"]?.ToString() ?? string.Empty,
                        IndexName = reader["IndexName"]?.ToString() ?? string.Empty,
                        CreateStatement = reader["CreateStatement"]?.ToString() ?? string.Empty
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GetSqlServerIndexDefinitionAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Server index definition for [{schemaName}].[{indexName}]: {ex.Message}", ex);
            }
        }

        private static async Task<SqlServerIndexDefinitionResult?> GetSqlServerIndexParentInfoAsync(DbConnection connection, string schemaName, string indexName)
        {
            try
            {
                const string sql = @"
SELECT TOP 1
    s.name AS SchemaName,
    t.name AS TableName,
    i.name AS IndexName
FROM sys.indexes i
INNER JOIN sys.tables t
    ON i.object_id = t.object_id
INNER JOIN sys.schemas s
    ON t.schema_id = s.schema_id
WHERE s.name = @SchemaName
  AND i.name = @IndexName
  AND i.is_primary_key = 0
  AND i.is_unique_constraint = 0
  AND i.type_desc <> 'HEAP';";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@SchemaName";
                p1.Value = schemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@IndexName";
                p2.Value = indexName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SqlServerIndexDefinitionResult
                    {
                        SchemaName = reader["SchemaName"]?.ToString() ?? string.Empty,
                        TableName = reader["TableName"]?.ToString() ?? string.Empty,
                        IndexName = reader["IndexName"]?.ToString() ?? string.Empty
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GetSqlServerIndexParentInfoAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Server index parent info for [{schemaName}].[{indexName}]: {ex.Message}", ex);
            }
        }

        private static async Task<List<SqlServerTriggerDefinitionResult>> GetSqlServerTriggerDefinitionsForTableAsync(DbConnection connection, string schemaName, string tableName)
        {
            try
            {
                const string sql = @"
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    tr.name AS TriggerName,
    sm.definition AS Definition
FROM sys.triggers tr
INNER JOIN sys.tables t
    ON tr.parent_id = t.object_id
INNER JOIN sys.schemas s
    ON t.schema_id = s.schema_id
INNER JOIN sys.sql_modules sm
    ON tr.object_id = sm.object_id
WHERE s.name = @SchemaName
  AND t.name = @TableName
ORDER BY tr.name;";

                var results = new List<SqlServerTriggerDefinitionResult>();

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@SchemaName";
                p1.Value = schemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@TableName";
                p2.Value = tableName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new SqlServerTriggerDefinitionResult
                    {
                        SchemaName = reader["SchemaName"]?.ToString() ?? string.Empty,
                        TableName = reader["TableName"]?.ToString() ?? string.Empty,
                        TriggerName = reader["TriggerName"]?.ToString() ?? string.Empty,
                        Definition = reader["Definition"]?.ToString() ?? string.Empty
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GetSqlServerTriggerDefinitionsForTableAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Server trigger definitions for table [{schemaName}].[{tableName}]: {ex.Message}", ex);
            }
        }

        private static async Task<SqlServerTriggerDefinitionResult?> GetSqlServerTriggerDefinitionAsync(DbConnection connection, string schemaName, string triggerName)
        {
            try
            {
                const string sql = @"
SELECT TOP 1
    s.name AS SchemaName,
    t.name AS TableName,
    tr.name AS TriggerName,
    sm.definition AS Definition
FROM sys.triggers tr
INNER JOIN sys.tables t
    ON tr.parent_id = t.object_id
INNER JOIN sys.schemas s
    ON t.schema_id = s.schema_id
INNER JOIN sys.sql_modules sm
    ON tr.object_id = sm.object_id
WHERE s.name = @SchemaName
  AND tr.name = @TriggerName;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@SchemaName";
                p1.Value = schemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@TriggerName";
                p2.Value = triggerName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SqlServerTriggerDefinitionResult
                    {
                        SchemaName = reader["SchemaName"]?.ToString() ?? string.Empty,
                        TableName = reader["TableName"]?.ToString() ?? string.Empty,
                        TriggerName = reader["TriggerName"]?.ToString() ?? string.Empty,
                        Definition = reader["Definition"]?.ToString() ?? string.Empty
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GetSqlServerTriggerDefinitionAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Server trigger definition for [{schemaName}].[{triggerName}]: {ex.Message}", ex);
            }
        }

        private static async Task<SqlServerTriggerDefinitionResult?> GetSqlServerTriggerParentInfoAsync(DbConnection connection, string schemaName, string triggerName)
        {
            try
            {
                const string sql = @"
SELECT TOP 1
    s.name AS SchemaName,
    t.name AS TableName,
    tr.name AS TriggerName
FROM sys.triggers tr
INNER JOIN sys.tables t
    ON tr.parent_id = t.object_id
INNER JOIN sys.schemas s
    ON t.schema_id = s.schema_id
WHERE s.name = @SchemaName
  AND tr.name = @TriggerName;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@SchemaName";
                p1.Value = schemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@TriggerName";
                p2.Value = triggerName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SqlServerTriggerDefinitionResult
                    {
                        SchemaName = reader["SchemaName"]?.ToString() ?? string.Empty,
                        TableName = reader["TableName"]?.ToString() ?? string.Empty,
                        TriggerName = reader["TriggerName"]?.ToString() ?? string.Empty
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GetSqlServerTriggerParentInfoAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Server trigger parent info for [{schemaName}].[{triggerName}]: {ex.Message}", ex);
            }
        }

        private static async Task<List<string>> GetSqlServerPrimaryKeyColumnsAsync(DbConnection connection, string schemaName, string tableName)
        {
            try
            {
                const string sql = @"
SELECT c.name AS ColumnName
FROM sys.key_constraints kc
INNER JOIN sys.tables t
    ON kc.parent_object_id = t.object_id
INNER JOIN sys.schemas s
    ON t.schema_id = s.schema_id
INNER JOIN sys.index_columns ic
    ON kc.parent_object_id = ic.object_id
   AND kc.unique_index_id = ic.index_id
INNER JOIN sys.columns c
    ON ic.object_id = c.object_id
   AND ic.column_id = c.column_id
WHERE kc.type = 'PK'
  AND s.name = @SchemaName
  AND t.name = @TableName
ORDER BY ic.key_ordinal;";

                var results = new List<string>();

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@SchemaName";
                p1.Value = schemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@TableName";
                p2.Value = tableName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var columnName = reader["ColumnName"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        results.Add(columnName);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.GetSqlServerPrimaryKeyColumnsAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Server primary key columns for [{schemaName}].[{tableName}]: {ex.Message}", ex);
            }
        }

        private static string BuildSqlServerWhereClause(IEnumerable<string> keyColumns, IReadOnlyDictionary<string, object?> rowValues)
        {
            try
            {
                var whereParts = new List<string>();

                foreach (var keyColumn in keyColumns)
                {
                    rowValues.TryGetValue(keyColumn, out var value);

                    if (value == null || value == DBNull.Value)
                    {
                        whereParts.Add($"{EscapeSqlServerIdentifier(keyColumn)} IS NULL");
                    }
                    else
                    {
                        whereParts.Add($"{EscapeSqlServerIdentifier(keyColumn)} = {ConvertToSqlServerLiteral(value)}");
                    }
                }

                return whereParts.Count > 0
                    ? string.Join(" AND ", whereParts)
                    : "1 = 0";
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.BuildSqlServerWhereClause()", ex.Message, false);
                throw new Exception($"Error while building SQL Server where clause: {ex.Message}", ex);
            }
        }

        private static void AppendSqlServerIndexScriptBlock(StringBuilder sb, SqlServerIndexDefinitionResult index)
        {
            try
            {
                var normalizedCreateDefinition = index.CreateStatement.Trim();
                var escapedCreateDefinition = DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral(normalizedCreateDefinition);

                sb.AppendLine($"-- Index: [{index.SchemaName}].[{index.IndexName}]");
                sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(index.SchemaName)}' AND t.name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(index.TableName)}' AND i.name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(index.IndexName)}')");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    EXEC dbo.sp_executesql @statement = N'{escapedCreateDefinition}'");
                sb.AppendLine("END");
                sb.AppendLine("ELSE");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    DROP INDEX [{index.IndexName}] ON [{index.SchemaName}].[{index.TableName}];");
                sb.AppendLine($"    EXEC dbo.sp_executesql @statement = N'{escapedCreateDefinition}'");
                sb.AppendLine("END");
                sb.AppendLine("GO");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.AppendSqlServerIndexScriptBlock()", ex.Message, false);
                throw new Exception($"Error while appending SQL Server index script block for [{index.SchemaName}].[{index.IndexName}]: {ex.Message}", ex);
            }
        }

        private static void AppendSqlServerTriggerScriptBlock(StringBuilder sb, SqlServerTriggerDefinitionResult trigger)
        {
            try
            {
                var normalizedCreateDefinition = trigger.Definition.Trim();
                var alterDefinition = DBObjectGenerationCommonHelper.ReplaceCreateWithAlter(normalizedCreateDefinition, "TRIGGER");
                var escapedCreateDefinition = DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral(normalizedCreateDefinition);
                var escapedAlterDefinition = DBObjectGenerationCommonHelper.EscapeSqlUnicodeLiteral(alterDefinition.Trim());

                sb.AppendLine($"-- Trigger: [{trigger.SchemaName}].[{trigger.TriggerName}]");
                sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.triggers tr INNER JOIN sys.tables t ON tr.parent_id = t.object_id INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(trigger.SchemaName)}' AND t.name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(trigger.TableName)}' AND tr.name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(trigger.TriggerName)}')");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    EXEC dbo.sp_executesql @statement = N'{escapedCreateDefinition}'");
                sb.AppendLine("END");
                sb.AppendLine("ELSE");
                sb.AppendLine("BEGIN");
                sb.AppendLine($"    EXEC dbo.sp_executesql @statement = N'{escapedAlterDefinition}'");
                sb.AppendLine("END");
                sb.AppendLine("GO");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.AppendSqlServerTriggerScriptBlock()", ex.Message, false);
                throw new Exception($"Error while appending SQL Server trigger script block for [{trigger.SchemaName}].[{trigger.TriggerName}]: {ex.Message}", ex);
            }
        }

        private static string EscapeSqlServerIdentifier(string identifier)
        {
            try
            {
                return $"[{(identifier ?? string.Empty).Replace("]", "]]")}]";
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.EscapeSqlServerIdentifier()", ex.Message, false);
                throw new Exception($"Error while escaping SQL Server identifier: {ex.Message}", ex);
            }
        }

        private static string ConvertToSqlServerLiteral(object? value)
        {
            try
            {
                if (value == null || value == DBNull.Value)
                {
                    return "NULL";
                }

                return value switch
                {
                    string stringValue => $"N'{stringValue.Replace("'", "''")}'",
                    char charValue => $"N'{charValue.ToString().Replace("'", "''")}'",
                    bool boolValue => boolValue ? "1" : "0",
                    byte byteValue => byteValue.ToString(CultureInfo.InvariantCulture),
                    sbyte sbyteValue => sbyteValue.ToString(CultureInfo.InvariantCulture),
                    short shortValue => shortValue.ToString(CultureInfo.InvariantCulture),
                    ushort ushortValue => ushortValue.ToString(CultureInfo.InvariantCulture),
                    int intValue => intValue.ToString(CultureInfo.InvariantCulture),
                    uint uintValue => uintValue.ToString(CultureInfo.InvariantCulture),
                    long longValue => longValue.ToString(CultureInfo.InvariantCulture),
                    ulong ulongValue => ulongValue.ToString(CultureInfo.InvariantCulture),
                    float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
                    double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
                    decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
                    DateTime dateTimeValue => $"'{dateTimeValue:yyyy-MM-dd HH:mm:ss.fffffff}'",
                    DateTimeOffset dateTimeOffsetValue => $"'{dateTimeOffsetValue:yyyy-MM-dd HH:mm:ss.fffffff zzz}'",
                    Guid guidValue => $"'{guidValue}'",
                    byte[] byteArrayValue => $"0x{BitConverter.ToString(byteArrayValue).Replace("-", string.Empty)}",
                    _ => $"N'{Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("'", "''")}'"
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLDBObjectGenerationHelper.ConvertToSqlServerLiteral()", ex.Message, false);
                throw new Exception($"Error while converting value to SQL Server literal: {ex.Message}", ex);
            }
        }
    }
}