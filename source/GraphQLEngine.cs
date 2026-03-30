using GraphQLParser;
using GraphQLParser.AST;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace LightDataSuite
{
    public class GraphQLEngine : BaseDataEngine, IGraphQLEngine
    {
        public GraphQLEngine(EngineConfig config) : base(config) { }

        public async Task<string> Run(JsonElement root)
        {
            if (!root.TryGetProperty("query", out JsonElement q))
                throw new ArgumentException("json must have a 'query' field.");

            string gql = (q.GetString() ?? "").ToLowerInvariant().Replace('\'', '"');

            var sqlParams = new List<SqlParameter>();
            foreach (JsonProperty prop in root.EnumerateObject())
            {
                string name = prop.Name.ToLowerInvariant();
                if (name == "query") continue;

                string rawValue = prop.Value.GetRawText().Trim('"');
                string inject = prop.Value.ValueKind switch
                {
                    JsonValueKind.Number => rawValue,
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => $"\"{rawValue.ToLowerInvariant()}\""
                };

                gql = Regex.Replace(gql, @"\$" + name, inject, RegexOptions.IgnoreCase);
                sqlParams.Add(new SqlParameter("@" + name, rawValue.ToLowerInvariant()));
            }

            var ast = Parser.Parse(gql);
            var field = ((GraphQLOperationDefinition)ast.Definitions[0])
                            .SelectionSet.Selections
                            .OfType<GraphQLField>()
                            .First();

            return await QuerySql(ToSql(field, null, sqlParams), sqlParams);
        }

        private string ToSql(GraphQLField field, string? parentTable, List<SqlParameter> sqlParams)
        {
            int nestLevel = 0;
            if (++nestLevel > _config.MaxNestedLevel)
                throw new InvalidOperationException("Nesting too deep: " + _config.MaxNestedLevel + " limit.");

            string table = field.Name.StringValue.ToLowerInvariant();
            CheckTable(table);

            var args = new Dictionary<string, string>();
            if (field.Arguments != null)
            {
                foreach (var arg in field.Arguments)
                {
                    string val = arg.Value switch
                    {
                        GraphQLIntValue i => i.Value.ToString(),
                        GraphQLStringValue s => s.Value.ToString().ToLowerInvariant(),
                        GraphQLBooleanValue b => b.Value.ToString().ToLowerInvariant(),
                        _ => arg.Value.ToString().ToLowerInvariant()
                    };
                    args[arg.Name.StringValue.ToLowerInvariant()] = val.Trim('"');
                }
            }

            string top = args.GetValueOrDefault("first", "10");
            string sort = args.GetValueOrDefault("order", "");
            bool isDesc = args.GetValueOrDefault("desc", "false") == "true";
            CheckFields(sort);

            var cols = new List<string>();
            foreach (var sel in field.SelectionSet.Selections.OfType<GraphQLField>())
            {
                string colName = sel.Name.StringValue.ToLowerInvariant();
                CheckFields(colName);
                if (sel.SelectionSet == null)
                    cols.Add($"[{colName}]");
                else
                    cols.Add($"({ToSql(sel, table, sqlParams)}) as [{sel.Alias?.Name.StringValue.ToLowerInvariant() ?? colName}]");
            }

            if (cols.Count > _config.MaxFields)
                throw new InvalidOperationException("Too many fields: " + _config.MaxFields + " limit.");

            var wheres = new List<string>();

            // ✅ Fixed: SqlParameter binding instead of raw string injection
            string[] reserved = { "first", "order", "desc" };
            foreach (var arg in args.Where(x => !reserved.Contains(x.Key)))
            {
                CheckFields(arg.Key);
                string paramName = "@w_" + table + "_" + arg.Key;   // unique param per table
                wheres.Add($"[{table}].[{arg.Key}] = {paramName}");
                sqlParams.Add(new SqlParameter(paramName, arg.Value));
            }

            string whereStr = wheres.Count > 0 ? " where " + string.Join(" and ", wheres) : "";
            string orderStr = string.IsNullOrEmpty(sort) ? "" : $" order by [{sort}]" + (isDesc ? " desc" : "");

            return $"select top {top} {string.Join(",", cols)} from [{table}]{whereStr}{orderStr} for json path";
        }
    }
}