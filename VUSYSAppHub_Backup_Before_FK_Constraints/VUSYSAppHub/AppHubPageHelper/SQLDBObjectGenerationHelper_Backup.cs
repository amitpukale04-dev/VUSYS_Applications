using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using VUSYSAppHub.Models;

namespace VUSYSAppHub.AppHubPageHelper
{
    public static class SQLDBObjectGenerationHelper_Backup
    {

        public static string BuildSqlServerConnectionString(DbGeneratorConnectionConfig config)
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

        public static async Task<string> GenerateSqlServerObjectScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            return item.ObjectType switch
            {
                "Table" => await GenerateSqlServerTableScriptAsync(_env, connection, config, item, options),
                "View" => await GenerateSqlServerViewScriptAsync(_env, connection, config, item, options),
                "Procedure" => await GenerateSqlServerProcedureScriptAsync(_env, connection, config, item, options),
                "Function" => await GenerateSqlServerFunctionScriptAsync(_env, connection, config, item, options),
                _ => $"-- Unsupported object: [{item.SchemaName}].[{item.ObjectName}]"
            };
        }

        public static async Task<string> GenerateSqlServerTableScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            var storedSource = await DBObjectGenerationCommonHelper.TryGetStoredOriginalSourceAsync(_env, config, item);
            if (!string.IsNullOrWhiteSpace(storedSource))
            {
                return BuildSqlServerStoredTableScript(item, storedSource, options);
            }

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

            return sb.ToString();
        }

        public static async Task<string> GenerateSqlServerViewScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
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

        public static async Task<string> GenerateSqlServerProcedureScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
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

        public static async Task<string> GenerateSqlServerFunctionScriptAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, ScriptGenerationOptions options)
        {
            var definition = await GetSqlServerStoredSourceOrModuleDefinitionAsync(_env, connection, config, item, "FN", "TF", "IF");

            var sb = new StringBuilder();
            sb.AppendLine($"-- Function: [{item.SchemaName}].[{item.ObjectName}]");
            sb.AppendLine(definition.Trim());
            sb.AppendLine("GO");

            return sb.ToString();
        }

        public static async Task<string> GetSqlServerStoredSourceOrModuleDefinitionAsync(IWebHostEnvironment _env, DbConnection connection, DbGeneratorConnectionConfig config, DbObjectItem item, params string[] objectTypes)
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

        public static string BuildSqlServerStoredTableScript(DbObjectItem item, string definition, ScriptGenerationOptions options)
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

        public static string BuildSqlServerDataType(string dataType, int? maxLength, int? precision, int? scale)
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

        public static async Task<string> GenerateSqlServerModuleScriptAsync(DbConnection connection, DbObjectItem item, ScriptGenerationOptions options, string objectKeyword)
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
    }
}
