namespace VUSYSAppHub.Models
{
    public class DBObjectGenerator
    {
    }

    public class DbGeneratorConnectionConfig
    {
        public string ApplicationName { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = string.Empty;
        public string DatabaseType { get; set; } = string.Empty; // SqlServer / SqlAnywhere
        public string ServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string AuthenticationType { get; set; } = "Sql";  // Windows / Sql
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string OdbcDriverName { get; set; } = string.Empty;

        public string ConnectionString { get; set; } = string.Empty;
        public string DisplayName => $"{ApplicationName} - {EnvironmentName}";
    }

    public class DbObjectItem
    {
        public string Id => $"{SchemaName}.{ObjectName}.{ObjectType}";
        public string SchemaName { get; set; } = string.Empty;
        public string ObjectName { get; set; } = string.Empty;
        public string ObjectType { get; set; } = string.Empty; // Table, View, Procedure, Function
        public bool IsSelected { get; set; }
    }

    public class DbObjectGroup
    {
        public string ObjectType { get; set; } = string.Empty;
        public bool IsExpanded { get; set; } = true;
        public List<DbObjectItem> Items { get; set; } = new();
    }

    public class ScriptGenerationOptions
    {
        public bool GenerateInDependencyOrder { get; set; }
        public bool IncludeInsert { get; set; }
        public bool IncludeUpdate { get; set; }
        public bool IncludeDelete { get; set; }
    }

    public class ScriptHistoryEntry
    {
        public DateTime GeneratedOn { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = string.Empty;
        public int ObjectCount { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string PreviewText { get; set; } = string.Empty;
    }

    public sealed class SourceTextResult
    {
        public string Definition { get; set; } = string.Empty;
        public bool IsStoredOriginal { get; set; }
        public bool IsDatabaseOriginalSource { get; set; }
        public string SourceDisplay { get; set; } = string.Empty;
    }

    public sealed class ApplicationNameWrapper
    {
        public List<string> ApplicationNames { get; set; } = new();
    }

    public sealed class DbConnectionWrapper
    {
        public List<DbConnectionMapping> ConnectionMappings { get; set; } = new();
    }

    public sealed class DbConnectionMapping
    {
        public string Application { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
    }

    public sealed class StringComparerForAnonymousKey : IEqualityComparer<object>
    {
        public static readonly StringComparerForAnonymousKey Instance = new();

        public new bool Equals(object? x, object? y)
        {
            if (x == null || y == null) return false;

            var xType = x.GetType();
            var yType = y.GetType();

            var xApp = xType.GetProperty("ApplicationName")?.GetValue(x)?.ToString() ?? string.Empty;
            var xEnv = xType.GetProperty("EnvironmentName")?.GetValue(x)?.ToString() ?? string.Empty;

            var yApp = yType.GetProperty("ApplicationName")?.GetValue(y)?.ToString() ?? string.Empty;
            var yEnv = yType.GetProperty("EnvironmentName")?.GetValue(y)?.ToString() ?? string.Empty;

            return string.Equals(xApp, yApp, StringComparison.OrdinalIgnoreCase)
                && string.Equals(xEnv, yEnv, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(object obj)
        {
            var type = obj.GetType();
            var app = type.GetProperty("ApplicationName")?.GetValue(obj)?.ToString() ?? string.Empty;
            var env = type.GetProperty("EnvironmentName")?.GetValue(obj)?.ToString() ?? string.Empty;

            return HashCode.Combine(
                app.ToUpperInvariant(),
                env.ToUpperInvariant());
        }
    }

    public sealed class SqlServerIndexDefinitionResult
    {
        public string SchemaName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string IndexName { get; set; } = string.Empty;
        public string CreateStatement { get; set; } = string.Empty;
    }

    public sealed class SqlServerTriggerDefinitionResult
    {
        public string SchemaName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string TriggerName { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
    }
}
