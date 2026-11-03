using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CommonLib;
using CommonLib.Constants;
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
                    "ForeignKey" => await GenerateSqlAnywhereForeignKeyScriptAsync(_env, connection, config, item, options),
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
                var indexMetadata = await GetSqlAnywhereIndexMetadataAsync(connection, item);

                var sourceResult = await GetSqlAnywhereSourceResultAsync(_env, connection, config, item, async () =>
                {
                    return await GetSqlAnywhereIndexDefinitionAsync(connection, item);
                });

                var createDefinition = sourceResult.Definition;
                var createExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(createDefinition);

                var dropDefinition = BuildSqlAnywhereDropIndexStatement(
                    indexMetadata.SchemaName,
                    indexMetadata.TableName,
                    indexMetadata.IndexName,
                    indexMetadata.IsPrimaryKeyIndex,
                    indexMetadata.IsUniqueConstraint);

                var dropExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(dropDefinition);

                var sb = new StringBuilder();
                sb.AppendLine($"-- Index: {item.SchemaName}.{item.ObjectName}");
                sb.AppendLine($"-- Table      : {indexMetadata.SchemaName}.{indexMetadata.TableName}");
                sb.AppendLine($"-- Source     : {sourceResult.SourceDisplay}");

                if (indexMetadata.IsPrimaryKeyIndex)
                {
                    sb.AppendLine("-- Index Type : Primary key index");
                }
                else if (indexMetadata.IsUniqueConstraint)
                {
                    sb.AppendLine("-- Index Type : Unique constraint");
                }
                else
                {
                    sb.AppendLine("-- Index Type : Normal index");
                }

                if (!sourceResult.IsStoredOriginal && !sourceResult.IsDatabaseOriginalSource)
                {
                    sb.AppendLine("-- Note       : Original formatting/casing may differ from authored source.");
                }

                sb.AppendLine("IF NOT EXISTS (");
                sb.AppendLine("   SELECT 1");
                sb.AppendLine("   FROM SYS.SYSIDX si");
                sb.AppendLine("   INNER JOIN SYS.SYSTAB st ON si.table_id = st.table_id");
                sb.AppendLine("   INNER JOIN SYS.SYSUSER su ON st.creator = su.user_id");
                sb.AppendLine($"   WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(indexMetadata.SchemaName)}'");
                sb.AppendLine($"     AND st.table_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(indexMetadata.TableName)}'");
                sb.AppendLine($"     AND si.index_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(indexMetadata.IndexName)}'");

                if (indexMetadata.IsPrimaryKeyIndex)
                {
                    sb.AppendLine("     AND si.index_category = 1");
                }
                else
                {
                    sb.AppendLine("     AND si.index_category <> 1");
                    sb.AppendLine("     AND NOT EXISTS (");
                    sb.AppendLine("         SELECT 1");
                    sb.AppendLine("         FROM SYS.SYSFOREIGNKEY sfk");
                    sb.AppendLine("         WHERE sfk.foreign_table_id = si.table_id");
                    sb.AppendLine("           AND sfk.role = si.index_name");
                    sb.AppendLine("     )");
                }

                sb.AppendLine(")");
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

        public static async Task<string> GenerateSqlAnywhereForeignKeyScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            try
            {
                var foreignKeyMetadata = await GetSqlAnywhereForeignKeyMetadataAsync(connection, item);

                var sourceResult = await GetSqlAnywhereSourceResultAsync(_env, connection, config, item, async () =>
                {
                    return await GetSqlAnywhereForeignKeyDefinitionAsync(connection, item);
                });

                var createDefinition = sourceResult.Definition;
                var createExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(createDefinition);

                var dropDefinition =
                    $"ALTER TABLE \"{foreignKeyMetadata.ForeignSchemaName}\".\"{foreignKeyMetadata.ForeignTableName}\" " +
                    $"DROP CONSTRAINT \"{foreignKeyMetadata.ForeignKeyName}\"";

                var dropExecutionSql = EscapeSqlAnywhereDynamicSqlForExecution(dropDefinition);

                var sb = new StringBuilder();
                sb.AppendLine($"-- Foreign Key: {item.SchemaName}.{item.ObjectName}");
                sb.AppendLine($"-- Foreign Table : {foreignKeyMetadata.ForeignSchemaName}.{foreignKeyMetadata.ForeignTableName}");
                sb.AppendLine($"-- Primary Table : {foreignKeyMetadata.PrimarySchemaName}.{foreignKeyMetadata.PrimaryTableName}");
                sb.AppendLine($"-- Source        : {sourceResult.SourceDisplay}");

                if (!sourceResult.IsStoredOriginal && !sourceResult.IsDatabaseOriginalSource)
                {
                    sb.AppendLine("-- Note          : Original formatting/casing may differ from authored source.");
                }

                sb.AppendLine("IF NOT EXISTS (");
                sb.AppendLine("   SELECT 1");
                sb.AppendLine("   FROM SYS.SYSFOREIGNKEY sfk");
                sb.AppendLine("   INNER JOIN SYS.SYSTAB st ON sfk.foreign_table_id = st.table_id");
                sb.AppendLine("   INNER JOIN SYS.SYSUSER su ON st.creator = su.user_id");
                sb.AppendLine($"   WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(foreignKeyMetadata.ForeignSchemaName)}'");
                sb.AppendLine($"     AND st.table_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(foreignKeyMetadata.ForeignTableName)}'");
                sb.AppendLine($"     AND sfk.role = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(foreignKeyMetadata.ForeignKeyName)}'");
                sb.AppendLine(")");
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
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GenerateSqlAnywhereForeignKeyScriptAsync()", ex.Message, false);
                throw new Exception($"Error while generating SQL Anywhere foreign key script for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
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

                var value = definition.Replace("\r\n", "\n").Replace('\r', '\n');

                var lines = value.Split('\n').ToList();
                var cleanedLines = new List<string>();

                bool commitWorkAdded = false;

                foreach (var rawLine in lines)
                {
                    var line = rawLine.Trim();

                    // ❌ Remove standalone semicolon lines
                    if (line == ";")
                    {
                        continue;
                    }

                    // Normalize commit work
                    if (string.Equals(line, "commit work", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(line, "commit work;", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!commitWorkAdded)
                        {
                            cleanedLines.Add("commit work;");
                            commitWorkAdded = true;
                        }

                        continue; // skip duplicates
                    }

                    cleanedLines.Add(rawLine);
                }

                var cleanedDefinition = string.Join(Environment.NewLine, cleanedLines);

                return NormalizeSqlAnywhereDefinition(cleanedDefinition);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereTableDefinitionAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere table definition for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
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
                var indexMetadata = await GetSqlAnywhereIndexMetadataAsync(connection, item);

                string sql;

                if (indexMetadata.IsPrimaryKeyIndex)
                {
                    sql = @"
SELECT CAST(
    'ALTER TABLE ""' || su.user_name || '"".\""' || st.table_name || '""' || CHAR(10) ||
    'ADD PRIMARY KEY ( ' ||
    LIST(
        '""' || sc.column_name || '""' ||
        CASE WHEN sic.""order"" = 'D' THEN ' DESC' ELSE ' ASC' END,
        ', '
        ORDER BY sic.sequence
    ) ||
    ' )'
AS LONG VARCHAR) AS index_def
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
  AND si.index_name = ?
  AND si.index_category = 1
GROUP BY si.index_name, su.user_name, st.table_name;";
                }
                else
                {
                    sql = @"
SELECT CAST(
    CASE 
        WHEN si.""unique"" = 2 THEN
            'ALTER TABLE ""' || su.user_name || '"".""' || st.table_name || '"" ADD CONSTRAINT ""' || si.index_name || '"" UNIQUE ( ' ||
            LIST(
                '""' || sc.column_name || '""' ||
                CASE WHEN sic.""order"" = 'D' THEN ' DESC' ELSE ' ASC' END,
                ', '
                ORDER BY sic.sequence
            ) ||
            ' );'
        ELSE
            'CREATE ' ||
            CASE WHEN si.""unique"" = 1 THEN 'UNIQUE ' ELSE '' END ||
            'INDEX ""' || si.index_name || '"" ON ""' || su.user_name || '"".""' || st.table_name || '""' || CHAR(10) ||
            '    ( ' ||
            LIST(
                '""' || sc.column_name || '""' ||
                CASE WHEN sic.""order"" = 'D' THEN ' DESC' ELSE ' ASC' END,
                ', '
                ORDER BY sic.sequence
            ) ||
            ' ) IN ""SYSTEM"";'
    END
AS LONG VARCHAR) AS index_def
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
  AND si.index_name = ?
  AND si.index_category <> 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM SYS.SYSFOREIGNKEY sfk
      WHERE sfk.foreign_table_id = si.table_id
        AND sfk.role = si.index_name
  )
GROUP BY 
    si.index_name, 
    si.""unique"", 
    su.user_name, 
    st.table_name;";
                }

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.Value = indexMetadata.SchemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.Value = indexMetadata.TableName;
                cmd.Parameters.Add(p2);

                var p3 = cmd.CreateParameter();
                p3.Value = indexMetadata.IndexName;
                cmd.Parameters.Add(p3);

                var definition = (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(definition))
                {
                    throw new Exception($"Index definition not found for {item.SchemaName}.{item.ObjectName}. The selected object may be a foreign key or named constraint instead of a standalone index.");
                }

                return NormalizeSqlAnywhereDefinition(definition);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereIndexDefinitionAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere index definition for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GetSqlAnywhereForeignKeyDefinitionAsync(DbConnection connection, DbObjectItem item)
        {
            try
            {
                var metadata = await GetSqlAnywhereForeignKeyMetadataAsync(connection, item);

                if (string.IsNullOrWhiteSpace(metadata.ForeignSchemaName) ||
                    string.IsNullOrWhiteSpace(metadata.ForeignTableName) ||
                    string.IsNullOrWhiteSpace(metadata.PrimarySchemaName) ||
                    string.IsNullOrWhiteSpace(metadata.PrimaryTableName) ||
                    string.IsNullOrWhiteSpace(metadata.ForeignKeyName) ||
                    metadata.ForeignColumns.Count == 0 ||
                    metadata.PrimaryColumns.Count == 0)
                {
                    throw new Exception($"Incomplete foreign key metadata found for {item.SchemaName}.{item.ObjectName}.");
                }

                var foreignColumns = string.Join(", ", metadata.ForeignColumns.Select(x => $"\"{x}\" ASC"));
                var primaryColumns = string.Join(", ", metadata.PrimaryColumns.Select(x => $"\"{x}\""));

                var sb = new StringBuilder();
                sb.Append($"ALTER TABLE \"{metadata.ForeignSchemaName}\".\"{metadata.ForeignTableName}\" ");
                sb.Append($"ADD CONSTRAINT \"{metadata.ForeignKeyName}\" ");

                if (metadata.IsNotNull)
                {
                    sb.Append("NOT NULL ");
                }

                sb.Append($"FOREIGN KEY ( {foreignColumns} ) ");
                sb.Append($"REFERENCES \"{metadata.PrimarySchemaName}\".\"{metadata.PrimaryTableName}\" ( {primaryColumns} )");

                var matchClause = NormalizeSqlAnywhereForeignKeyMatch(metadata.MatchType);
                if (!string.IsNullOrWhiteSpace(matchClause))
                {
                    sb.Append($" {matchClause}");
                }

                var updateRule = NormalizeSqlAnywhereForeignKeyAction(metadata.UpdateAction);
                var deleteRule = NormalizeSqlAnywhereForeignKeyAction(metadata.DeleteAction);

                if (!string.IsNullOrWhiteSpace(updateRule))
                {
                    sb.Append($" ON UPDATE {updateRule}");
                }

                if (!string.IsNullOrWhiteSpace(deleteRule))
                {
                    sb.Append($" ON DELETE {deleteRule}");
                }

                sb.Append(";");

                return NormalizeSqlAnywhereDefinition(sb.ToString());
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereForeignKeyDefinitionAsync()", ex.Message, false);
                throw new Exception($"Error while building SQL Anywhere foreign key definition for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
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
                value = value.Replace("\\\"", "\"");

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

        private static string NormalizeSqlAnywhereForeignKeyAction(string? actionValue)
        {
            if (string.IsNullOrWhiteSpace(actionValue))
            {
                return string.Empty;
            }

            var value = actionValue.Trim().ToUpperInvariant();

            return value switch
            {
                "CASCADE" => "CASCADE",
                "RESTRICT" => "RESTRICT",
                "SET NULL" => "SET NULL",
                "SET DEFAULT" => "SET DEFAULT",
                "C" => "CASCADE",
                "R" => "RESTRICT",
                "N" => "SET NULL",
                "D" => "SET DEFAULT",
                _ => string.Empty
            };
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

        public static string BuildSqlAnywhereDropIndexStatement(string schemaName, string tableName, string indexName, bool isPrimaryKeyIndex, bool isUniqueConstraint)
        {
            try
            {
                if (isPrimaryKeyIndex || isUniqueConstraint)
                {
                    return $"ALTER TABLE {EscapeSqlAnywhereIdentifier(schemaName)}.{EscapeSqlAnywhereIdentifier(tableName)} DROP PRIMARY KEY";
                }

                return $"DROP INDEX {EscapeSqlAnywhereIdentifier(indexName)}";
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.BuildSqlAnywhereDropIndexStatement()", ex.Message, false);
                throw new Exception($"Error while building SQL Anywhere drop index statement for {schemaName}.{tableName}.{indexName}: {ex.Message}", ex);
            }
        }

        public static async Task<SqlAnywhereIndexMetadata> GetSqlAnywhereIndexMetadataAsync(DbConnection connection, DbObjectItem item)
        {
            try
            {
                const string sql = @"
SELECT TOP 1
    su.user_name AS SchemaName,
    st.table_name AS TableName,
    si.index_name AS IndexName,
    si.index_category AS IndexCategory,
    CASE WHEN si.""unique"" = 1 THEN 1 ELSE 0 END AS IsUnique,
    CASE 
        WHEN EXISTS
        (
            SELECT 1
            FROM SYS.SYSFOREIGNKEY sfk
            WHERE sfk.foreign_table_id = si.table_id
              AND sfk.role = si.index_name
        )
        THEN 1 ELSE 0
    END AS IsForeignKeyBacked
FROM SYS.SYSIDX si
INNER JOIN SYS.SYSTAB st
    ON si.table_id = st.table_id
INNER JOIN SYS.SYSUSER su
    ON st.creator = su.user_id
WHERE su.user_name = ?
  AND si.index_name = ?;";

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.Value = item.SchemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.Value = item.ObjectName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();

                if (reader != null && await reader.ReadAsync())
                {
                    var indexCategory = Convert.ToInt32(reader["IndexCategory"]);
                    var isUnique = Convert.ToInt32(reader["IsUnique"]) == 1;
                    var isForeignKeyBacked = Convert.ToInt32(reader["IsForeignKeyBacked"]) == 1;

                    if (isForeignKeyBacked)
                    {
                        throw new Exception($"The object {item.SchemaName}.{item.ObjectName} is a foreign key constraint, not a standalone index.");
                    }

                    return new SqlAnywhereIndexMetadata
                    {
                        SchemaName = reader["SchemaName"]?.ToString()?.Trim() ?? string.Empty,
                        TableName = reader["TableName"]?.ToString()?.Trim() ?? string.Empty,
                        IndexName = reader["IndexName"]?.ToString()?.Trim() ?? string.Empty,
                        IndexCategory = indexCategory,
                        IsUnique = isUnique,

                        // Keep your existing inference pattern.
                        // PK index => index_category = 1
                        // Unique + not PK => constraint-like index in your current model
                        IsConstraint = indexCategory != 1 && isUnique
                    };
                }

                throw new Exception($"Index metadata not found for {item.SchemaName}.{item.ObjectName}.");
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereIndexMetadataAsync()", ex.Message, false);
                throw new Exception($"Error while fetching SQL Anywhere index metadata for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<SqlAnywhereForeignKeyMetadata> GetSqlAnywhereForeignKeyMetadataAsync(DbConnection connection, DbObjectItem item)
        {
            try
            {
                var row = await GetSqlAnywhereForeignKeyRowAsync(connection, item);

                return new SqlAnywhereForeignKeyMetadata
                {
                    ForeignKeyName = row.ForeignKeyName,
                    ForeignSchemaName = row.ForeignSchemaName,
                    ForeignTableName = row.ForeignTableName,
                    PrimarySchemaName = row.PrimarySchemaName,
                    PrimaryTableName = row.PrimaryTableName,
                    ForeignColumns = row.ForeignColumns,
                    PrimaryColumns = row.PrimaryColumns,
                    MatchType = row.MatchType,
                    UpdateAction = row.UpdateAction,
                    DeleteAction = row.DeleteAction,
                    IsNotNull = row.IsNotNull
                };
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubPageHelper", "SQLAnyWhere17DBObjectGenerationHelper.GetSqlAnywhereForeignKeyMetadataAsync()", ex.Message, false);
                throw new Exception($"Error while reading SQL Anywhere foreign key metadata for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        private static async Task<SqlAnywhereForeignKeyRow> GetSqlAnywhereForeignKeyRowAsync(DbConnection connection, DbObjectItem item)
        {
            var schemaTable = connection.GetSchema("Columns", new[] { null, "SYS", "SYSFOREIGNKEYS", null });

            var availableColumns = schemaTable.Rows
                .Cast<DataRow>()
                .Select(r => r["COLUMN_NAME"]?.ToString() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (availableColumns.Count == 0)
            {
                throw new Exception("Unable to read SYS.SYSFOREIGNKEYS schema information.");
            }

            string GetCol(params string[] candidates)
            {
                return availableColumns.FirstOrDefault(c =>
                    candidates.Any(x => string.Equals(c, x, StringComparison.OrdinalIgnoreCase))) ?? string.Empty;
            }

            var fkNameCol = GetCol("role", "name", "constraint_name", "foreign_key_name");
            var foreignSchemaCol = GetCol("foreign_creator", "creator", "owner", "schema_name", "table_schema");
            var foreignTableCol = GetCol("foreign_tname", "tname", "table_name", "foreign_table_name");
            var primarySchemaCol = GetCol("primary_creator", "ref_creator", "referenced_creator", "primary_schema", "referenced_schema");
            var primaryTableCol = GetCol("primary_tname", "ref_tname", "referenced_tname", "primary_table_name", "referenced_table_name");
            var foreignColsCol = GetCol("columns", "foreign_columns", "fkey_columns", "foreign_column_list");

            if (string.IsNullOrWhiteSpace(fkNameCol) ||
                string.IsNullOrWhiteSpace(foreignSchemaCol) ||
                string.IsNullOrWhiteSpace(foreignTableCol) ||
                string.IsNullOrWhiteSpace(primarySchemaCol) ||
                string.IsNullOrWhiteSpace(primaryTableCol) ||
                string.IsNullOrWhiteSpace(foreignColsCol))
            {
                throw new Exception("Unable to resolve required SYS.SYSFOREIGNKEYS columns. Available columns: " + string.Join(", ", availableColumns));
            }

            var distinctColumns = new List<string>
    {
        fkNameCol,
        foreignSchemaCol,
        foreignTableCol,
        primarySchemaCol,
        primaryTableCol,
        foreignColsCol
    }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(c => $"\"{c}\"")
            .ToList();

            var querySql = $"SELECT {string.Join(", ", distinctColumns)} FROM SYS.SYSFOREIGNKEYS WHERE \"{fkNameCol}\" = ?";

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = querySql;

            var p1 = cmd.CreateParameter();
            p1.Value = item.ObjectName;
            cmd.Parameters.Add(p1);

            await using var reader = await cmd.ExecuteReaderAsync();

            SqlAnywhereForeignKeyRow? row = null;

            while (await reader.ReadAsync())
            {
                var fkName = reader[fkNameCol]?.ToString()?.Trim() ?? string.Empty;
                if (!string.Equals(fkName, item.ObjectName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var foreignSchema = reader[foreignSchemaCol]?.ToString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(item.SchemaName) &&
                    !string.Equals(foreignSchema, item.SchemaName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var primarySchema = reader[primarySchemaCol]?.ToString()?.Trim() ?? string.Empty;
                var primaryTable = reader[primaryTableCol]?.ToString()?.Trim() ?? string.Empty;

                row = new SqlAnywhereForeignKeyRow
                {
                    ForeignKeyName = fkName,
                    ForeignSchemaName = foreignSchema,
                    ForeignTableName = reader[foreignTableCol]?.ToString()?.Trim() ?? string.Empty,
                    PrimarySchemaName = primarySchema,
                    PrimaryTableName = primaryTable,
                    ForeignColumns = ParseSqlAnywhereColumnList(reader[foreignColsCol]?.ToString()),
                    PrimaryColumns = await GetSqlAnywherePrimaryKeyColumnsAsync(connection, primarySchema, primaryTable),
                    MatchType = string.Empty,
                    UpdateAction = string.Empty,
                    DeleteAction = string.Empty,
                    IsNotNull = false
                };

                break;
            }

            if (row == null)
            {
                throw new Exception($"Foreign key metadata not found in SYS.SYSFOREIGNKEYS for {item.SchemaName}.{item.ObjectName}.");
            }

            if (row.ForeignColumns.Count == 0)
            {
                throw new Exception($"Foreign key columns not found for {item.SchemaName}.{item.ObjectName}.");
            }

            if (row.PrimaryColumns.Count == 0)
            {
                throw new Exception($"Primary key columns not found for referenced table {row.PrimarySchemaName}.{row.PrimaryTableName}.");
            }

            if (row.ForeignColumns.Count != row.PrimaryColumns.Count)
            {
                throw new Exception(
                    $"Foreign key column count mismatch for {item.SchemaName}.{item.ObjectName}. " +
                    $"Foreign columns: {row.ForeignColumns.Count}, Primary columns: {row.PrimaryColumns.Count}.");
            }

            return row;
        }

        private static List<string> ParseSqlAnywhereColumnList(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<string>();
            }

            var cleaned = raw.Trim();

            if (cleaned.StartsWith("(") && cleaned.EndsWith(")"))
            {
                cleaned = cleaned.Substring(1, cleaned.Length - 2);
            }

            return cleaned
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                {
                    var part = x.Trim().Trim('"');

                    // 🔥 FIX: handle "col1 IS col2"
                    if (part.Contains(" IS ", StringComparison.OrdinalIgnoreCase))
                    {
                        part = part.Split(new[] { " IS " }, StringSplitOptions.None)[0];
                    }

                    return part.Trim();
                })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private static string NormalizeSqlAnywhereForeignKeyMatch(string? matchType)
        {
            if (string.IsNullOrWhiteSpace(matchType))
            {
                return string.Empty;
            }

            var value = matchType.Trim().ToUpperInvariant();

            return value switch
            {
                "SIMPLE" => "MATCH SIMPLE",
                "FULL" => "MATCH FULL",
                "UNIQUE SIMPLE" => "MATCH UNIQUE SIMPLE",
                "UNIQUE FULL" => "MATCH UNIQUE FULL",
                _ => string.Empty
            };
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
                    string abc = sb.ToString();
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

                var results = new List<string>();

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var p1 = cmd.CreateParameter();
                p1.Value = schemaName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.Value = tableName;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var col = reader["column_name"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(col))
                    {
                        results.Add(col);
                    }
                }

                return results;
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