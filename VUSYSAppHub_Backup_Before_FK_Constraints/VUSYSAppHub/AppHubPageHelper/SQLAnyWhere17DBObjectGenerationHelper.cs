using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CommonLib;
using VUSYSAppHub.DACHelper;
using VUSYSAppHub.Models;

namespace VUSYSAppHub.AppHubPageHelper
{
    public static class SQLAnyWhere17DBObjectGenerationHelper
    {
        public static string BuildSqlAnywhereConnectionString(DbGeneratorConnectionConfig config)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(config.ConnectionString))
                {
                    return config.ConnectionString;
                }

                var driver = string.IsNullOrWhiteSpace(config.OdbcDriverName)
                    ? "SQL Anywhere 17"
                    : config.OdbcDriverName;

                var parts = new List<string>
                {
                    $"Driver={{{driver}}}"
                };

                if (!string.IsNullOrWhiteSpace(config.UserName))
                    parts.Add($"UID={config.UserName}");

                if (!string.IsNullOrWhiteSpace(config.Password))
                    parts.Add($"PWD={config.Password}");

                if (!string.IsNullOrWhiteSpace(config.ServerName))
                    parts.Add($"Server={config.ServerName}");

                if (!string.IsNullOrWhiteSpace(config.DatabaseName))
                    parts.Add($"Database={config.DatabaseName}");

                return string.Join(";", parts) + ";";
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.BuildSqlAnywhereConnectionString()", ex.Message, false);
                throw new Exception($"Error while building SQL Anywhere connection string: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlAnywhereObjectScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                if (item.ObjectType.StartsWith("DataScript", StringComparison.OrdinalIgnoreCase))
                {
                    return await GenerateSqlAnywhereDataScriptAsync(connection, item, options);
                }

                return item.ObjectType switch
                {
                    "Table" => await GenerateSqlAnywhereTableScriptAsync(_env, connection, config, item, options),
                    "View" => await GenerateSqlAnywhereViewScriptAsync(_env, connection, config, item, options),
                    "Procedure" => await GenerateSqlAnywhereProcedureScriptAsync(_env, connection, config, item, options),
                    "Function" => await GenerateSqlAnywhereFunctionScriptAsync(_env, connection, config, item, options),
                    "Index" => await GenerateSqlAnywhereIndexScriptAsync(_env, connection, config, item, options),
                    "Trigger" => await GenerateSqlAnywhereTriggerScriptAsync(_env, connection, config, item, options),
                    _ => $"-- Unsupported object: {item.SchemaName}.{item.ObjectName}"
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GenerateSqlAnywhereObjectScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Anywhere object script for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlAnywhereTableScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var storedSource = await DBObjectGenerationCommonHelper.TryGetStoredOriginalSourceAsync(_env, config, item);
                if (!string.IsNullOrWhiteSpace(storedSource))
                {
                    return BuildSqlAnywhereStoredTableScript(item, storedSource, options);
                }

                var definition = await GetSqlAnywhereTableDefinitionAsync(connection, item);

                var sb = new StringBuilder();
                sb.AppendLine($"-- Table: {item.SchemaName}.{item.ObjectName}");
                sb.AppendLine("-- Source     : Database metadata");
                sb.AppendLine("-- Note       : Original formatting/casing may differ from authored source.");
                AppendSqlAnywhereWrappedStatements(sb, item, definition);
                sb.AppendLine();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GenerateSqlAnywhereTableScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Anywhere table script for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlAnywhereViewScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var sourceResult = await GetSqlAnywhereSourceResultAsync(_env, connection, config, item, async () =>
                {
                    var definition = await GetSqlAnywhereViewDefinitionAsync(connection, item);
                    return NormalizeSqlAnywhereDefinition(definition);
                });

                var createDefinition = sourceResult.Definition;
                var alterDefinition = DBObjectGenerationCommonHelper.ReplaceCreateWithAlter(createDefinition, "VIEW");
                var createExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(createDefinition);
                var alterExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(alterDefinition);

                var sb = new StringBuilder();
                sb.AppendLine($"-- View: {item.SchemaName}.{item.ObjectName}");
                sb.AppendLine($"-- Source     : {sourceResult.SourceDisplay}");
                if (!sourceResult.IsStoredOriginal && !sourceResult.IsDatabaseOriginalSource)
                {
                    sb.AppendLine("-- Note       : Original formatting/casing may differ from authored source.");
                }

                sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM SYSTAB st INNER JOIN SYSUSER su ON st.creator = su.user_id INNER JOIN SYSVIEW sv ON st.object_id = sv.view_object_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND st.table_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
                sb.AppendLine("THEN");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{createExecutionSql}';");
                sb.AppendLine("ELSE");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{alterExecutionSql}';");
                sb.AppendLine("END IF;");
                sb.AppendLine();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GenerateSqlAnywhereViewScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Anywhere view script for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlAnywhereProcedureScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var sourceResult = await GetSqlAnywhereSourceResultAsync(_env, connection, config, item, async () =>
                {
                    return await GetSqlAnywhereProcedureDefinitionAsync(connection, item);
                });

                var createDefinition = sourceResult.Definition;
                var alterDefinition = DBObjectGenerationCommonHelper.ReplaceCreateWithAlter(createDefinition, "PROCEDURE");
                var createExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(createDefinition);
                var alterExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(alterDefinition);

                var sb = new StringBuilder();
                sb.AppendLine($"-- Procedure: {item.SchemaName}.{item.ObjectName}");
                sb.AppendLine($"-- Source     : {sourceResult.SourceDisplay}");
                if (!sourceResult.IsStoredOriginal && !sourceResult.IsDatabaseOriginalSource)
                {
                    sb.AppendLine("-- Note       : Original formatting/casing may differ from authored source.");
                }

                sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM SYSPROCEDURE sp INNER JOIN SYSUSER su ON sp.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND sp.proc_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
                sb.AppendLine("THEN");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{createExecutionSql}';");
                sb.AppendLine("ELSE");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{alterExecutionSql}';");
                sb.AppendLine("END IF;");
                sb.AppendLine();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GenerateSqlAnywhereProcedureScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Anywhere procedure script for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlAnywhereFunctionScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var sourceResult = await GetSqlAnywhereSourceResultAsync(_env, connection, config, item, async () =>
                {
                    return await GetSqlAnywhereProcedureDefinitionAsync(connection, item);
                });

                var createDefinition = sourceResult.Definition;
                var alterDefinition = DBObjectGenerationCommonHelper.ReplaceCreateWithAlter(createDefinition, "FUNCTION");
                var createExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(createDefinition);
                var alterExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(alterDefinition);

                var sb = new StringBuilder();
                sb.AppendLine($"-- Function: {item.SchemaName}.{item.ObjectName}");
                sb.AppendLine($"-- Source     : {sourceResult.SourceDisplay}");
                if (!sourceResult.IsStoredOriginal && !sourceResult.IsDatabaseOriginalSource)
                {
                    sb.AppendLine("-- Note       : Original formatting/casing may differ from authored source.");
                }

                sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM SYSPROCEDURE sp INNER JOIN SYSUSER su ON sp.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND sp.proc_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
                sb.AppendLine("THEN");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{createExecutionSql}';");
                sb.AppendLine("ELSE");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{alterExecutionSql}';");
                sb.AppendLine("END IF;");
                sb.AppendLine();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GenerateSqlAnywhereFunctionScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Anywhere function script for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlAnywhereIndexScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var sourceResult = await GetSqlAnywhereSourceResultAsync(_env, connection, config, item, async () =>
                {
                    return await GetSqlAnywhereIndexDefinitionAsync(connection, item);
                });

                var createDefinition = sourceResult.Definition;
                var createExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(createDefinition);
                var dropDefinition = BuildSqlAnywhereDropIndexStatement(item.SchemaName, item.ObjectName);
                var dropExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(dropDefinition);

                var sb = new StringBuilder();
                sb.AppendLine($"-- Index: {item.SchemaName}.{item.ObjectName}");
                sb.AppendLine($"-- Source     : {sourceResult.SourceDisplay}");
                if (!sourceResult.IsStoredOriginal && !sourceResult.IsDatabaseOriginalSource)
                {
                    sb.AppendLine("-- Note       : Original formatting/casing may differ from authored source.");
                }

                sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM SYS.SYSIDX si INNER JOIN SYS.SYSTAB st ON si.table_id = st.table_id INNER JOIN SYS.SYSUSER su ON st.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND si.index_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
                sb.AppendLine("THEN");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{createExecutionSql}';");
                sb.AppendLine("ELSE");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{dropExecutionSql}';");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{createExecutionSql}';");
                sb.AppendLine("END IF;");
                sb.AppendLine();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GenerateSqlAnywhereIndexScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Anywhere index script for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlAnywhereTriggerScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var sourceResult = await GetSqlAnywhereSourceResultAsync(_env, connection, config, item, async () =>
                {
                    return await GetSqlAnywhereTriggerDefinitionAsync(connection, item);
                });

                var createDefinition = sourceResult.Definition;
                var alterDefinition = DBObjectGenerationCommonHelper.ReplaceCreateWithAlter(createDefinition, "TRIGGER");

                var createExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(createDefinition);
                var alterExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(alterDefinition);

                var sb = new StringBuilder();
                sb.AppendLine($"-- Trigger: {item.SchemaName}.{item.ObjectName}");
                sb.AppendLine($"-- Source     : {sourceResult.SourceDisplay}");
                if (!sourceResult.IsStoredOriginal && !sourceResult.IsDatabaseOriginalSource)
                {
                    sb.AppendLine("-- Note       : Original formatting/casing may differ from authored source.");
                }

                sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM SYS.SYSTRIGGER trg INNER JOIN SYS.SYSTAB st ON trg.table_id = st.table_id INNER JOIN SYS.SYSUSER su ON st.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND trg.trigger_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
                sb.AppendLine("THEN");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{createExecutionSql}';");
                sb.AppendLine("ELSE");
                sb.AppendLine($"   EXECUTE IMMEDIATE '{alterExecutionSql}';");
                sb.AppendLine("END IF;");
                sb.AppendLine();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GenerateSqlAnywhereTriggerScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Anywhere trigger script for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateSqlAnywhereDataScriptAsync(DbConnection connection, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"-- DataScript: {item.SchemaName}.{item.ObjectName}");
                sb.AppendLine("-- Source     : Live table data");

                if (!options.IncludeInsert && !options.IncludeUpdate && !options.IncludeDelete)
                {
                    sb.AppendLine("-- No operations selected (Insert/Update/Delete).");
                    sb.AppendLine();
                    return sb.ToString();
                }

                var safeSchema = EscapeSqlAnywhereIdentifier(item.SchemaName);
                var safeTable = EscapeSqlAnywhereIdentifier(item.ObjectName);

                var primaryKeyColumns = await GetSqlAnywherePrimaryKeyColumnsAsync(connection, item.SchemaName, item.ObjectName);

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM {safeSchema}.{safeTable}";

                await using var reader = await cmd.ExecuteReaderAsync();

                var columnNames = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    columnNames.Add(reader.GetName(i));
                }

                if (columnNames.Count == 0)
                {
                    sb.AppendLine("-- No columns found.");
                    sb.AppendLine();
                    return sb.ToString();
                }

                var columnList = string.Join(", ", columnNames.Select(EscapeSqlAnywhereIdentifier));
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

                    var insertValues = columnNames
                        .Select(col => ConvertToSqlAnywhereLiteral(rowValues[col]))
                        .ToList();

                    if (options.IncludeInsert)
                    {
                        sb.AppendLine($"INSERT INTO {safeSchema}.{safeTable} ({columnList}) VALUES ({string.Join(", ", insertValues)});");
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
                                .Select(col => $"{EscapeSqlAnywhereIdentifier(col)} = {ConvertToSqlAnywhereLiteral(rowValues[col])}")
                                .ToList();

                            var whereClause = BuildSqlAnywhereWhereClause(primaryKeyColumns, rowValues);

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
                            var whereClause = BuildSqlAnywhereWhereClause(primaryKeyColumns, rowValues);

                            sb.AppendLine($"DELETE FROM {safeSchema}.{safeTable}");
                            sb.AppendLine($"WHERE {whereClause};");
                        }
                    }
                }

                if (!hasRows)
                {
                    sb.AppendLine("-- No data found.");
                }

                sb.AppendLine();
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GenerateSqlAnywhereDataScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Anywhere data script for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GetSqlAnywhereTableDefinitionAsync(DbConnection connection, DbObjectItem item)
        {
            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT sa_get_table_definition('{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}','{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')";

                var definition = (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
                return NormalizeSqlAnywhereDefinition(definition);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereTableDefinitionAsync()", ex.Message, false);
                throw new Exception(
                    $"Error while fetching SQL Anywhere table definition for {item.SchemaName}.{item.ObjectName}: {ex.Message}",
                    ex);
            }
        }

        public static async Task<string> GetSqlAnywhereViewDefinitionAsync(DbConnection connection, DbObjectItem item)
        {
            try
            {
                const string sql = @"
SELECT
    CAST(s.source AS LONG VARCHAR) AS source_text,
    CAST(sv.view_def AS LONG VARCHAR) AS view_def_text
FROM SYS.SYSSOURCE s
JOIN SYS.SYSTAB t
    ON s.object_id = t.object_id
JOIN SYS.SYSVIEW sv
    ON sv.view_object_id = t.object_id
JOIN SYS.SYSUSER u
    ON t.creator = u.user_id
WHERE u.user_name = ?
  AND t.table_name = ?;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.Value = item.SchemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.Value = item.ObjectName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();

                string sourceText = string.Empty;
                string viewDefText = string.Empty;

                if (await reader.ReadAsync())
                {
                    sourceText = reader["source_text"]?.ToString() ?? string.Empty;
                    viewDefText = reader["view_def_text"]?.ToString() ?? string.Empty;
                }

                var definition = !string.IsNullOrWhiteSpace(sourceText)
                    ? sourceText
                    : viewDefText;

                return NormalizeSqlAnywhereDefinition(definition);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereViewDefinitionAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere view definition for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GetSqlAnywhereProcedureDefinitionAsync(DbConnection connection, DbObjectItem item)
        {
            try
            {
                const string sql = @"
SELECT 
    CAST(sp.source AS LONG VARCHAR) AS source_text,
    CAST(sp.proc_defn AS LONG VARCHAR) AS proc_defn_text
FROM SYSPROCEDURE sp
INNER JOIN SYSUSER su ON sp.creator = su.user_id
WHERE su.user_name = ?
  AND sp.proc_name = ?;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.Value = item.SchemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.Value = item.ObjectName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();

                string sourceText = string.Empty;
                string procDefnText = string.Empty;

                if (await reader.ReadAsync())
                {
                    sourceText = reader["source_text"]?.ToString() ?? string.Empty;
                    procDefnText = reader["proc_defn_text"]?.ToString() ?? string.Empty;
                }

                var definition = !string.IsNullOrWhiteSpace(sourceText)
                    ? sourceText
                    : procDefnText;

                return NormalizeSqlAnywhereDefinition(definition);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereProcedureDefinitionAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere procedure/function definition for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GetSqlAnywhereIndexDefinitionAsync(DbConnection connection, DbObjectItem item)
        {
            try
            {
                const string sql = @"
SELECT CAST(
    'CREATE ' ||
    CASE WHEN si.""unique"" <> 0 THEN 'UNIQUE ' ELSE '' END ||
    'INDEX ""' || si.index_name || '"" ON ""' || su.user_name || '"".\""' || st.table_name || '""' || CHAR(10) ||
    '    ( ' ||
    LIST(
        '""' || sc.column_name || '""' ||
        CASE WHEN sic.""order"" = 'D' THEN ' DESC' ELSE '' END,
        ', '
        ORDER BY sic.sequence
    ) ||
    ' )'
AS LONG VARCHAR) AS index_def
FROM SYS.SYSIDX si
INNER JOIN SYS.SYSTAB st ON si.table_id = st.table_id
INNER JOIN SYS.SYSUSER su ON st.creator = su.user_id
INNER JOIN SYS.SYSIDXCOL sic ON si.table_id = sic.table_id AND si.index_id = sic.index_id
INNER JOIN SYS.SYSCOLUMN sc ON sic.table_id = sc.table_id AND sic.column_id = sc.column_id
WHERE su.user_name = ?
  AND si.index_name = ?
GROUP BY si.index_name, si.""unique"", su.user_name, st.table_name;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.Value = item.SchemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.Value = item.ObjectName;
                cmd.Parameters.Add(p2);

                var definition = (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
                return NormalizeSqlAnywhereDefinition(definition);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereIndexDefinitionAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere index definition for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GetSqlAnywhereTriggerDefinitionAsync(DbConnection connection, DbObjectItem item)
        {
            try
            {
                const string sql = @"
SELECT 
    CAST(trg.source AS LONG VARCHAR) AS source_text
FROM SYS.SYSTRIGGER trg
INNER JOIN SYS.SYSTAB st ON trg.table_id = st.table_id
INNER JOIN SYS.SYSUSER su ON st.creator = su.user_id
WHERE su.user_name = ?
  AND trg.trigger_name = ?;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.Value = item.SchemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.Value = item.ObjectName;
                cmd.Parameters.Add(p2);

                var definition = (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
                return NormalizeSqlAnywhereDefinition(definition);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereTriggerDefinitionAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere trigger definition for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<SourceTextResult> GetSqlAnywhereSourceResultAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, Func<Task<string>> metadataSourceFactory)
        {
            try
            {
                var storedSource = await DBObjectGenerationCommonHelper.TryGetStoredOriginalSourceAsync(_env, config, item);
                if (!string.IsNullOrWhiteSpace(storedSource))
                {
                    return new SourceTextResult
                    {
                        Definition = DBObjectGenerationCommonHelper.NormalizeStoredSource(storedSource),
                        IsStoredOriginal = true,
                        IsDatabaseOriginalSource = false,
                        SourceDisplay = "Stored original source"
                    };
                }

                var metadataDefinition = await metadataSourceFactory();
                return new SourceTextResult
                {
                    Definition = NormalizeSqlAnywhereDefinition(metadataDefinition),
                    IsStoredOriginal = false,
                    IsDatabaseOriginalSource = true,
                    SourceDisplay = "Database source/source metadata"
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereSourceResultAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere source result for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static string NormalizeSqlAnywhereDefinition(string definition)
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.NormalizeSqlAnywhereDefinition()", ex.Message, false);
                throw new Exception($"Error while normalizing SQL Anywhere definition: {ex.Message}", ex);
            }
        }

        public static string EscapeSqlAnywhereDynamicSqlForExecution(string value)
        {
            try
            {
                return DBObjectGenerationCommonHelper.NormalizeStoredSource(value).Replace("'", "''");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.EscapeSqlAnywhereDynamicSqlForExecution()", ex.Message, false);
                throw new Exception($"Error while escaping SQL Anywhere dynamic SQL for execution: {ex.Message}", ex);
            }
        }

        public static void AppendSqlAnywhereReadableSourceBlock(StringBuilder sb, string definition)
        {
            try
            {
                var readableDefinition = DBObjectGenerationCommonHelper.NormalizeStoredSource(definition);

                sb.AppendLine("-- Readable Source (Original Formatting)");
                sb.AppendLine("/*");
                sb.AppendLine(readableDefinition + ";");
                sb.AppendLine("*/");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.AppendSqlAnywhereReadableSourceBlock()", ex.Message, false);
                throw new Exception($"Error while appending SQL Anywhere readable source block: {ex.Message}", ex);
            }
        }

        public static string BuildSqlAnywhereStoredTableScript(DbObjectItem item, string definition, ScriptGenerationOptions options)
        {
            try
            {
                var normalizedDefinition = DBObjectGenerationCommonHelper.NormalizeStoredSource(definition);

                var sb = new StringBuilder();
                sb.AppendLine($"-- Table: {item.SchemaName}.{item.ObjectName}");
                sb.AppendLine("-- Source     : Stored original source");
                AppendSqlAnywhereWrappedStatements(sb, item, normalizedDefinition);
                sb.AppendLine();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.BuildSqlAnywhereStoredTableScript()", ex.Message, false);
                throw new Exception($"Error while building SQL Anywhere stored table script for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static string BuildSqlAnywhereDropIndexStatement(string schemaName, string indexName)
        {
            try
            {
                return $"DROP INDEX {EscapeSqlAnywhereIdentifier(schemaName)}.{EscapeSqlAnywhereIdentifier(indexName)}";
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.BuildSqlAnywhereDropIndexStatement()", ex.Message, false);
                throw new Exception($"Error while building SQL Anywhere drop index statement for {schemaName}.{indexName}: {ex.Message}", ex);
            }
        }

        public static void AppendSqlAnywhereWrappedStatements(StringBuilder sb, DbObjectItem item, string definition)
        {
            try
            {
                var statements = SplitSqlAnywhereStatements(definition);

                foreach (var statement in statements)
                {
                    var trimmedStatement = statement.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedStatement))
                    {
                        continue;
                    }

                    AppendSqlAnywhereWrappedStatement(sb, item, trimmedStatement);
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.AppendSqlAnywhereWrappedStatements()", ex.Message, false);
                throw new Exception($"Error while appending SQL Anywhere wrapped statements for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static List<string> SplitSqlAnywhereStatements(string definition)
        {
            try
            {
                var results = new List<string>();

                if (string.IsNullOrWhiteSpace(definition))
                {
                    return results;
                }

                var normalized = definition.Replace("\r\n", "\n").Replace("\r", "\n");
                var current = new StringBuilder();
                var lines = normalized.Split('\n');

                foreach (var rawLine in lines)
                {
                    var line = rawLine ?? string.Empty;
                    current.AppendLine(line);

                    if (line.Trim() == ";")
                    {
                        var statement = current.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(statement))
                        {
                            results.Add(statement);
                        }

                        current.Clear();
                    }
                }

                if (current.Length > 0)
                {
                    var lastStatement = current.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(lastStatement))
                    {
                        results.Add(lastStatement);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.SplitSqlAnywhereStatements()", ex.Message, false);
                throw new Exception($"Error while splitting SQL Anywhere statements: {ex.Message}", ex);
            }
        }

        public static void AppendSqlAnywhereWrappedStatement(StringBuilder sb, DbObjectItem item, string statement)
        {
            try
            {
                var normalized = statement.Trim();

                if (IsCreateSequenceStatement(normalized))
                {
                    var sequenceName = ExtractSqlAnywhereSequenceName(normalized);
                    if (string.IsNullOrWhiteSpace(sequenceName))
                    {
                        AppendSqlAnywhereExecuteImmediate(sb, normalized);
                        return;
                    }

                    sb.AppendLine(
                        $"IF NOT EXISTS (SELECT 1 FROM SYS.SYSSEQUENCE ss INNER JOIN SYS.SYSUSER su ON ss.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND ss.sequence_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(sequenceName)}')");
                    sb.AppendLine("THEN");
                    sb.AppendLine($"   EXECUTE IMMEDIATE '{EscapeSqlAnywhereDynamicSqlForExecution(normalized)}';");
                    sb.AppendLine("END IF;");
                    return;
                }

                if (IsCreateTableStatement(normalized))
                {
                    sb.AppendLine(
                        $"IF NOT EXISTS (SELECT 1 FROM SYSTAB st INNER JOIN SYSUSER su ON st.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND st.table_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
                    sb.AppendLine("THEN");
                    sb.AppendLine($"   EXECUTE IMMEDIATE '{EscapeSqlAnywhereDynamicSqlForExecution(normalized)}';");
                    sb.AppendLine("END IF;");
                    return;
                }

                if (IsCreateIndexStatement(normalized))
                {
                    var indexName = ExtractSqlAnywhereIndexName(normalized);
                    if (string.IsNullOrWhiteSpace(indexName))
                    {
                        AppendSqlAnywhereExecuteImmediate(sb, normalized);
                        return;
                    }

                    sb.AppendLine(
                        $"IF NOT EXISTS (SELECT 1 FROM SYS.SYSIDX si INNER JOIN SYS.SYSTAB st ON si.table_id = st.table_id INNER JOIN SYS.SYSUSER su ON st.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND st.table_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}' AND si.index_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(indexName)}')");
                    sb.AppendLine("THEN");
                    sb.AppendLine($"   EXECUTE IMMEDIATE '{EscapeSqlAnywhereDynamicSqlForExecution(normalized)}';");
                    sb.AppendLine("END IF;");
                    return;
                }

                if (IsCreateTriggerStatement(normalized))
                {
                    var triggerName = ExtractSqlAnywhereTriggerName(normalized);
                    if (string.IsNullOrWhiteSpace(triggerName))
                    {
                        AppendSqlAnywhereExecuteImmediate(sb, normalized);
                        return;
                    }

                    sb.AppendLine(
                        $"IF NOT EXISTS (SELECT 1 FROM SYS.SYSTRIGGER trg INNER JOIN SYS.SYSTAB st ON trg.table_id = st.table_id INNER JOIN SYS.SYSUSER su ON st.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND st.table_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}' AND trg.trigger_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(triggerName)}')");
                    sb.AppendLine("THEN");
                    sb.AppendLine($"   EXECUTE IMMEDIATE '{EscapeSqlAnywhereDynamicSqlForExecution(normalized)}';");
                    sb.AppendLine("END IF;");
                    return;
                }

                sb.AppendLine(normalized);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.AppendSqlAnywhereWrappedStatement()", ex.Message, false);
                throw new Exception($"Error while appending SQL Anywhere wrapped statement for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static void AppendSqlAnywhereExecuteImmediate(StringBuilder sb, string statement)
        {
            try
            {
                sb.AppendLine($"EXECUTE IMMEDIATE '{EscapeSqlAnywhereDynamicSqlForExecution(statement)}';");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.AppendSqlAnywhereExecuteImmediate()", ex.Message, false);
                throw new Exception($"Error while appending SQL Anywhere execute immediate statement: {ex.Message}", ex);
            }
        }

        public static bool IsCreateTableStatement(string statement)
        {
            try
            {
                return Regex.IsMatch(statement, @"^\s*CREATE\s+TABLE\b", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.IsCreateTableStatement()", ex.Message, false);
                throw new Exception($"Error while identifying SQL Anywhere CREATE TABLE statement: {ex.Message}", ex);
            }
        }

        public static bool IsCreateIndexStatement(string statement)
        {
            try
            {
                return Regex.IsMatch(statement, @"^\s*CREATE\s+(UNIQUE\s+)?INDEX\b", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.IsCreateIndexStatement()", ex.Message, false);
                throw new Exception($"Error while identifying SQL Anywhere CREATE INDEX statement: {ex.Message}", ex);
            }
        }

        public static bool IsCreateTriggerStatement(string statement)
        {
            try
            {
                return Regex.IsMatch(statement, @"^\s*CREATE\s+TRIGGER\b", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.IsCreateTriggerStatement()", ex.Message, false);
                throw new Exception($"Error while identifying SQL Anywhere CREATE TRIGGER statement: {ex.Message}", ex);
            }
        }

        public static bool IsCreateSequenceStatement(string statement)
        {
            try
            {
                return Regex.IsMatch(statement, @"^\s*CREATE\s+SEQUENCE\b", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.IsCreateSequenceStatement()", ex.Message, false);
                throw new Exception($"Error while identifying SQL Anywhere CREATE SEQUENCE statement: {ex.Message}", ex);
            }
        }

        public static string ExtractSqlAnywhereSequenceName(string statement)
        {
            try
            {
                var match = Regex.Match(
                    statement,
                    @"CREATE\s+SEQUENCE\s+(?:""(?<schema>[^""]+)""\.)?""(?<name>[^""]+)""",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                if (match.Success)
                {
                    return match.Groups["name"].Value.Trim();
                }

                match = Regex.Match(
                    statement,
                    @"CREATE\s+SEQUENCE\s+(?:([A-Za-z0-9_]+)\.)?([A-Za-z0-9_]+)",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                return match.Success ? match.Groups[2].Value.Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.ExtractSqlAnywhereSequenceName()", ex.Message, false);
                throw new Exception($"Error while extracting SQL Anywhere sequence name: {ex.Message}", ex);
            }
        }

        public static string ExtractSqlAnywhereIndexName(string statement)
        {
            try
            {
                var match = Regex.Match(
                    statement,
                    @"CREATE\s+(?:UNIQUE\s+)?INDEX\s+""(?<name>[^""]+)""",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                if (match.Success)
                {
                    return match.Groups["name"].Value.Trim();
                }

                match = Regex.Match(
                    statement,
                    @"CREATE\s+(?:UNIQUE\s+)?INDEX\s+(?<name>[A-Za-z0-9_]+)",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                return match.Success ? match.Groups["name"].Value.Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.ExtractSqlAnywhereIndexName()", ex.Message, false);
                throw new Exception($"Error while extracting SQL Anywhere index name: {ex.Message}", ex);
            }
        }

        public static string ExtractSqlAnywhereTriggerName(string statement)
        {
            try
            {
                var match = Regex.Match(
                    statement,
                    @"CREATE\s+TRIGGER\s+(?:""(?<schema>[^""]+)""\.)?""(?<name>[^""]+)""",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                if (match.Success)
                {
                    return match.Groups["name"].Value.Trim();
                }

                match = Regex.Match(
                    statement,
                    @"CREATE\s+TRIGGER\s+(?:([A-Za-z0-9_]+)\.)?([A-Za-z0-9_]+)",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                return match.Success ? match.Groups[2].Value.Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.ExtractSqlAnywhereTriggerName()", ex.Message, false);
                throw new Exception($"Error while extracting SQL Anywhere trigger name: {ex.Message}", ex);
            }
        }

        public static string EscapeSqlAnywhereIdentifier(string identifier)
        {
            try
            {
                return $"\"{(identifier ?? string.Empty).Replace("\"", "\"\"")}\"";
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.EscapeSqlAnywhereIdentifier()", ex.Message, false);
                throw new Exception($"Error while escaping SQL Anywhere identifier: {ex.Message}", ex);
            }
        }

        public static string ConvertToSqlAnywhereLiteral(object? value)
        {
            try
            {
                if (value == null || value == DBNull.Value)
                {
                    return "NULL";
                }

                return value switch
                {
                    string stringValue => $"'{stringValue.Replace("'", "''")}'",
                    char charValue => $"'{charValue.ToString().Replace("'", "''")}'",
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
                    DateTime dateTimeValue => $"'{dateTimeValue:yyyy-MM-dd HH:mm:ss.fff}'",
                    DateTimeOffset dateTimeOffsetValue => $"'{dateTimeOffsetValue:yyyy-MM-dd HH:mm:ss.fff zzz}'",
                    Guid guidValue => $"'{guidValue}'",
                    byte[] byteArrayValue => $"X'{BitConverter.ToString(byteArrayValue).Replace("-", string.Empty)}'",
                    _ => $"'{Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("'", "''")}'"
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.ConvertToSqlAnywhereLiteral()", ex.Message, false);
                throw new Exception($"Error while converting value to SQL Anywhere literal: {ex.Message}", ex);
            }
        }

        private static async Task<List<string>> GetSqlAnywherePrimaryKeyColumnsAsync(DbConnection connection, string schemaName, string tableName)
        {
            try
            {
                var pkColumns = await TryGetSqlAnywherePrimaryKeyColumnsAsync(connection, schemaName, tableName);
                if (pkColumns.Count > 0)
                {
                    return pkColumns;
                }

                pkColumns = await TryGetSqlAnywherePrimaryKeyColumnsAsync(connection, schemaName, tableName);
                return pkColumns;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywherePrimaryKeyColumnsAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere primary key columns for {schemaName}.{tableName}: {ex.Message}", ex);
            }
        }

        private static async Task<List<string>> TryGetSqlAnywherePrimaryKeyColumnsAsync(DbConnection connection, string schemaName, string tableName)
        {
            try
            {
                const string sql = @"
SELECT sc.column_name
FROM SYS.SYSIDX si
INNER JOIN SYS.SYSTAB st
    ON si.table_id = st.table_id
INNER JOIN SYS.SYSUSER su
    ON st.creator = su.user_id
INNER JOIN SYS.SYSIDXCOL sic
    ON si.table_id = sic.table_id
   AND si.index_id = sic.index_id
INNER JOIN SYS.SYSCOLUMN sc
    ON sic.table_id = sc.table_id
   AND sic.column_id = sc.column_id
WHERE su.user_name = ?
  AND st.table_name = ?
  AND si.index_category = 1
ORDER BY sic.sequence;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.Value = schemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.Value = tableName;
                cmd.Parameters.Add(p2);

                var result = new List<string>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var columnName = reader["column_name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        result.Add(columnName);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.TryGetSqlAnywherePrimaryKeyColumnsAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere primary key columns for {schemaName}.{tableName}: {ex.Message}", ex);
            }
        }

        private static string BuildSqlAnywhereWhereClause(IEnumerable<string> keyColumns, IReadOnlyDictionary<string, object?> row)
        {
            try
            {
                var predicates = new List<string>();

                foreach (var keyColumn in keyColumns)
                {
                    var identifier = EscapeSqlAnywhereIdentifier(keyColumn);
                    row.TryGetValue(keyColumn, out var value);

                    if (value == null || value == DBNull.Value)
                    {
                        predicates.Add($"{identifier} IS NULL");
                    }
                    else
                    {
                        predicates.Add($"{identifier} = {ConvertToSqlAnywhereLiteral(value)}");
                    }
                }

                return predicates.Count == 0 ? "1 = 0" : string.Join(" AND ", predicates);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.BuildSqlAnywhereWhereClause()", ex.Message, false);
                throw new Exception($"Error while building SQL Anywhere where clause: {ex.Message}", ex);
            }
        }
    }
}