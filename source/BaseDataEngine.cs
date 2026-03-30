using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Collections.Generic;

namespace LightDataSuite
{
    public interface IKqlEngine { Task<string> Run(JsonElement root); }
    public interface IGraphQLEngine { Task<string> Run(JsonElement root); }

    public abstract class BaseDataEngine
    {
        protected readonly EngineConfig _config;
        protected readonly HashSet<string> _allowedTables;
        protected readonly HashSet<string> _blockedFields;

        protected BaseDataEngine(EngineConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(config.ConnectionString))
                throw new ArgumentException("ConnectionString is required.");

            // Use config setters for normalization
            config.AllowedTables +="";
            _allowedTables = config._AllowedTables;

            config.BlockedFields = config.BlockedFields ?? "";
            _blockedFields = config._BlockedFields;
        }

        protected void CheckTable(string table)
        {
            string t = table.ToLowerInvariant();
            if (_allowedTables.Count > 0 && !_allowedTables.Contains(t))
                throw new UnauthorizedAccessException($"Table '{t}' not allowed.");
            if (t.Contains(']') || t.Contains('[') || t.Length > 128)
                throw new ArgumentException("Invalid table name.");
        }

        protected void CheckFields(string? text)
        {
            if (string.IsNullOrWhiteSpace(text) || _blockedFields.Count == 0) return;
            string f = text.ToLowerInvariant();
            foreach (var block in _blockedFields)
                if (f.Contains(block))
                    throw new UnauthorizedAccessException($"Field '{block}' is blocked.");
        }

        protected async Task<string> QuerySql(string sql, List<SqlParameter> parameters)
        {
            if (sql.Length > _config.MaxQueryLength)
                throw new ArgumentException($"Query too long: {_config.MaxQueryLength} char limit.");
            if (parameters?.Count > _config.MaxParameters)
                throw new ArgumentException($"Too many parameters: {_config.MaxParameters} limit.");

            await using var conn = new SqlConnection(_config.ConnectionString);
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = _config.CommandTimeout };
            if (parameters != null) cmd.Parameters.AddRange(parameters.ToArray());

            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();

            var sb = new StringBuilder(4096);
            int rowCount = 0;
            while (await reader.ReadAsync())
            {
                if (++rowCount > _config.MaxRows)
                    throw new InvalidOperationException($"Too many rows: {_config.MaxRows} limit.");
                sb.Append(reader.GetString(0));
            }

            string res = sb.ToString();
            if (res.Length > _config.MaxJsonLength)
                throw new InvalidOperationException($"Response too large: {_config.MaxJsonLength} byte limit.");
            return string.IsNullOrEmpty(res) ? "[]" : res;
        }
    }
}