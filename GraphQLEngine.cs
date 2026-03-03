using GraphQLParser;
using GraphQLParser.AST;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LightDataSuite
{
    public class GraphQLEngine : BaseDataEngine, IGraphQLEngine
    {
        public GraphQLEngine(string conn, string tables, string blocks) : base(conn, tables, blocks) { }

        public async Task<string> Run(JsonElement root)
        {
            if (!root.TryGetProperty("query", out JsonElement q))
                throw new ArgumentException("json must have a 'query' field.");

            // Force everything to lowercase immediately
            string gql = (q.GetString() ?? "").ToLower().Replace('\'', '"');
            var sqlParams = new List<SqlParameter>();

            foreach (JsonProperty prop in root.EnumerateObject())
            {
                string name = prop.Name.ToLower();
                if (name == "query") continue;

                string rawValue = prop.Value.GetRawText().Trim('"');
                string inject = prop.Value.ValueKind switch
                {
                    JsonValueKind.Number => rawValue,
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => $"\"{rawValue.ToLower()}\"" // Even data values lowercase
                };

                // Replace $id with value
                gql = Regex.Replace(gql, "\\$" + name, inject, RegexOptions.IgnoreCase);
                sqlParams.Add(new SqlParameter($"@{name}", rawValue.ToLower()));
            }

            var ast = Parser.Parse(gql);
            var field = ((GraphQLOperationDefinition)ast.Definitions[0]).SelectionSet.Selections.OfType<GraphQLField>().First();

            return await QuerySql(ToSql(field, null, sqlParams), sqlParams);
        }

        private string ToSql(GraphQLField field, string? parentTable, List<SqlParameter> sqlParams)
        {
            string table = field.Name.StringValue.ToLower();
            CheckTable(table);

            var args = new Dictionary<string, string>();
            if (field.Arguments != null)
            {
                foreach (var arg in field.Arguments)
                {
                    string val = arg.Value switch
                    {
                        GraphQLIntValue i => i.Value.ToString(),
                        GraphQLStringValue s => s.Value.ToString().ToLower(),
                        GraphQLBooleanValue b => b.Value.ToString().ToLower(),
                        _ => arg.Value.ToString().ToLower()
                    };
                    args[arg.Name.StringValue.ToLower()] = val.Trim('"');
                }
            }

            string top = args.GetValueOrDefault("first", "10");
            string sort = args.GetValueOrDefault("order", "");
            bool isDesc = args.GetValueOrDefault("desc", "false") == "true";
            CheckFields(sort);

            var cols = new List<string>();
            foreach (var sel in field.SelectionSet.Selections.OfType<GraphQLField>())
            {
                string colName = sel.Name.StringValue.ToLower();
                CheckFields(colName);
                if (sel.SelectionSet == null) cols.Add($"[{colName}]");
                else cols.Add($"({ToSql(sel, table, sqlParams)}) as [{sel.Alias?.Name.StringValue.ToLower() ?? colName}]");
            }

            var wheres = new List<string>();
            string on = args.GetValueOrDefault("on", ""), pkey = args.GetValueOrDefault("parentkey", "");
            if (!string.IsNullOrEmpty(on) && !string.IsNullOrEmpty(parentTable))
                wheres.Add($"[{table}].[{on}] = [{parentTable}].[{pkey}]");

            string[] reserved = { "first", "order", "desc", "on", "parentkey" };
            foreach (var arg in args.Where(x => !reserved.Contains(x.Key)))
            {
                CheckFields(arg.Key);
                wheres.Add($"[{table}].[{arg.Key}] = '{arg.Value}'");
            }

            string whereStr = wheres.Count > 0 ? " where " + string.Join(" and ", wheres) : "";
            string orderStr = string.IsNullOrEmpty(sort) ? "" : $" order by [{sort}]" + (isDesc ? " desc" : "");

            return $"select top {top} {string.Join(",", cols)} from [{table}]{whereStr}{orderStr} for json path";
        }
    }
}