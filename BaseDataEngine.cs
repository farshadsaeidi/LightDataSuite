using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Text;

namespace LightDataSuite
{
    public interface IKqlEngine { Task<string> Run(JsonElement root); }
    public interface IGraphQLEngine { Task<string> Run(JsonElement root); }

    public abstract class BaseDataEngine
    {
        protected readonly string _connection;
        protected readonly HashSet<string> _tables;
        protected readonly HashSet<string> _blockFields;

        protected BaseDataEngine(string connection, string tables, string blockFields)
        {
            _connection = connection;
            // Force configuration to lowercase
            _tables = new HashSet<string>((tables ?? "").ToLower().Split('|', StringSplitOptions.RemoveEmptyEntries));
            _blockFields = new HashSet<string>((blockFields ?? "").ToLower().Split('|', StringSplitOptions.RemoveEmptyEntries));
        }

        protected void CheckTable(string table)
        {
            string t = table.ToLower(); // Force incoming table to lowercase
            if (_tables.Count > 0 && !_tables.Contains(t))
                throw new UnauthorizedAccessException($"table '{t}' is blocked.");
            if (t.Contains(']')) throw new ArgumentException("bad table name.");
        }

        protected void CheckFields(string? text)
        {
            if (string.IsNullOrWhiteSpace(text) || _blockFields.Count == 0) return;
            string f = text.ToLower(); // Force incoming field check to lowercase
            foreach (var block in _blockFields)
                if (f.Contains(block)) throw new UnauthorizedAccessException($"field '{block}' is blocked.");
        }

        protected async Task<string> QuerySql(string sql, List<SqlParameter> parameters)
        {
            await using var conn = new SqlConnection(_connection);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            var sb = new StringBuilder(4096);
            while (await reader.ReadAsync()) sb.Append(reader.GetString(0));
            string res = sb.ToString();
            return string.IsNullOrEmpty(res) ? "[]" : res;
        }
    }
}