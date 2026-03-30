namespace LightDataSuite
{
    public class EngineConfig
    {
        public string ConnectionString { get; set; } = "";

        // Our Deal: Default to Case-Insensitive to match SQL Server
        public bool CaseSensitive { get; set; } = false;

        // Allowed/Blocked Lists
        public HashSet<string> _AllowedTables { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public string AllowedTables
        {
            set
            {
                _AllowedTables = new HashSet<string>(
                    (value ?? "")
                        .ToLowerInvariant()
                        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    StringComparer.OrdinalIgnoreCase);
            }
            get;
        }

        public HashSet<string> _BlockedFields { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public string BlockedFields
        {
            set
            {
                _BlockedFields = new HashSet<string>(
                    (value ?? "")
                        .ToLowerInvariant()
                        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    StringComparer.OrdinalIgnoreCase);
            }
            get;
        }

        // Hard Limits for Maximum Security & Performance
        public int MaxNestedLevel { get; set; } = 3;
        public int MaxRows { get; set; } = 100;
        public int MaxFields { get; set; } = 30;     // Global limit across entire query
        public int MaxParameters { get; set; } = 10;
        public int MaxJsonLength { get; set; } = 262_144; // 256KB max response
        public int MaxQueryLength { get; set; } = 2_048;  // 2KB max request payload
        public int CommandTimeout { get; set; } = 15;     // 15 seconds max execution
    }
}