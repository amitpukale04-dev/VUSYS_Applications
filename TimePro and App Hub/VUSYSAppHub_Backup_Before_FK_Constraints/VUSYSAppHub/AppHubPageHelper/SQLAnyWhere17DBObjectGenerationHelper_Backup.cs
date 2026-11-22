using System.Data.Common;
using System.Text;
using VUSYSAppHub.Models;

namespace VUSYSAppHub.AppHubPageHelper
{
    public static class SQLAnyWhere17DBObjectGenerationHelper_Backup
    {
        public static string BuildSqlAnywhereConnectionString(DbGeneratorConnectionConfig config)
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

        public static async Task<string> GenerateSqlAnywhereObjectScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            return item.ObjectType switch
            {
                "Table" => await GenerateSqlAnywhereTableScriptAsync(_env, connection, config, item, options),
                "View" => await GenerateSqlAnywhereViewScriptAsync(_env, connection, config, item, options),
                "Procedure" => await GenerateSqlAnywhereProcedureScriptAsync(_env, connection, config, item, options),
                "Function" => await GenerateSqlAnywhereFunctionScriptAsync(_env, connection, config, item, options),
                _ => $"-- Unsupported object: {item.SchemaName}.{item.ObjectName}"
            };
        }

        public static async Task<string> GenerateSqlAnywhereTableScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            var storedSource = await DBObjectGenerationCommonHelper.TryGetStoredOriginalSourceAsync(_env, config, item);
            if (!string.IsNullOrWhiteSpace(storedSource))
            {
                return BuildSqlAnywhereStoredTableScript(item, storedSource, options);
            }

            var definition = await GetSqlAnywhereTableDefinitionAsync(connection, item);
            var executionSql = EscapeSqlAnywhereDynamicSqlForExecution(definition);

            var sb = new StringBuilder();
            sb.AppendLine($"-- Table: {item.SchemaName}.{item.ObjectName}");
            sb.AppendLine("-- Source     : Database metadata");
            sb.AppendLine("-- Note       : Original formatting/casing may differ from authored source.");
            //AppendSqlAnywhereReadableSourceBlock(sb, definition);
            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM SYSTAB st INNER JOIN SYSUSER su ON st.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND st.table_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
            sb.AppendLine("THEN");
            sb.AppendLine($"   EXECUTE IMMEDIATE '{executionSql}';");
            sb.AppendLine("END IF;");
            sb.AppendLine();

            return sb.ToString();
        }

        public static async Task<string> GenerateSqlAnywhereViewScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
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

            //AppendSqlAnywhereReadableSourceBlock(sb, createDefinition);
            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM SYSTAB st INNER JOIN SYSUSER su ON st.creator = su.user_id INNER JOIN SYSVIEW sv ON st.object_id = sv.view_object_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND st.table_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
            sb.AppendLine("THEN");
            sb.AppendLine($"   EXECUTE IMMEDIATE '{createExecutionSql}';");
            sb.AppendLine("ELSE");
            sb.AppendLine($"   EXECUTE IMMEDIATE '{alterExecutionSql}';");
            sb.AppendLine("END IF;");
            sb.AppendLine();

            return sb.ToString();
        }

        public static async Task<string> GenerateSqlAnywhereProcedureScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
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

            //AppendSqlAnywhereReadableSourceBlock(sb, createDefinition);
            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM SYSPROCEDURE sp INNER JOIN SYSUSER su ON sp.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND sp.proc_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
            sb.AppendLine("THEN");
            sb.AppendLine($"   EXECUTE IMMEDIATE '{createExecutionSql}';");
            sb.AppendLine("ELSE");
            sb.AppendLine($"   EXECUTE IMMEDIATE '{alterExecutionSql}';");
            sb.AppendLine("END IF;");
            sb.AppendLine();

            return sb.ToString();
        }

        public static async Task<string> GenerateSqlAnywhereFunctionScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
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

            //AppendSqlAnywhereReadableSourceBlock(sb, createDefinition);
            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM SYSPROCEDURE sp INNER JOIN SYSUSER su ON sp.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND sp.proc_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
            sb.AppendLine("THEN");
            sb.AppendLine($"   EXECUTE IMMEDIATE '{createExecutionSql}';");
            sb.AppendLine("ELSE");
            sb.AppendLine($"   EXECUTE IMMEDIATE '{alterExecutionSql}';");
            sb.AppendLine("END IF;");
            sb.AppendLine();

            return sb.ToString();
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
                throw new Exception($"Error while fetching SQL Anywhere table definition for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<string> GetSqlAnywhereViewDefinitionAsync(DbConnection connection, DbObjectItem item)
        {
            try
            {
                const string sql = @"
SELECT 
    CAST(st.source AS LONG VARCHAR) AS source_text,
    CAST(sv.view_def AS LONG VARCHAR) AS view_def_text
FROM SYSVIEW sv
INNER JOIN SYSTAB st ON sv.view_object_id = st.object_id
INNER JOIN SYSUSER su ON st.creator = su.user_id
WHERE su.user_name = ?
  AND st.table_name = ?;";

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
                throw new Exception($"Error while fetching SQL Anywhere procedure/function definition for {item.SchemaName}.{item.ObjectName}: {ex.Message}", ex);
            }
        }

        public static async Task<SourceTextResult> GetSqlAnywhereSourceResultAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, Func<Task<string>> metadataSourceFactory)
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

        public static string NormalizeSqlAnywhereDefinition(string definition)
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

        public static string EscapeSqlAnywhereDynamicSqlForExecution(string value)
        {
            return DBObjectGenerationCommonHelper.NormalizeStoredSource(value).Replace("'", "''");
        }

        public static void AppendSqlAnywhereReadableSourceBlock(StringBuilder sb, string definition)
        {
            var readableDefinition = DBObjectGenerationCommonHelper.NormalizeStoredSource(definition);

            sb.AppendLine("-- Readable Source (Original Formatting)");
            sb.AppendLine("/*");
            sb.AppendLine(readableDefinition + ";");
            sb.AppendLine("*/");
        }

        public static string BuildSqlAnywhereStoredTableScript(DbObjectItem item, string definition, ScriptGenerationOptions options)
        {
            var normalizedDefinition = DBObjectGenerationCommonHelper.NormalizeStoredSource(definition);
            var executionSql = EscapeSqlAnywhereDynamicSqlForExecution(normalizedDefinition);
            var sb = new StringBuilder();

            sb.AppendLine($"-- Table: {item.SchemaName}.{item.ObjectName}");
            sb.AppendLine("-- Source     : Stored original source");
            //AppendSqlAnywhereReadableSourceBlock(sb, normalizedDefinition);
            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM SYSTAB st INNER JOIN SYSUSER su ON st.creator = su.user_id WHERE su.user_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.SchemaName)}' AND st.table_name = '{DBObjectGenerationCommonHelper.EscapeSqlLiteral(item.ObjectName)}')");
            sb.AppendLine("THEN");
            sb.AppendLine($"   EXECUTE IMMEDIATE '{executionSql}';");
            sb.AppendLine("END IF;");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
