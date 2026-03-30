using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace LightDataSuite
{
    public class KqlEngine : BaseDataEngine, IKqlEngine
    {
        public KqlEngine(EngineConfig config) : base(config) { }

        public async Task<string> Run(JsonElement root)
        {
            if (!root.TryGetProperty("query", out JsonElement q))
                throw new ArgumentException("json must have a 'query' field.");

            string kql = (q.GetString() ?? "").ToLowerInvariant().Trim();

            var sqlParams = new List<SqlParameter>();
            foreach (JsonProperty prop in root.EnumerateObject())
            {
                string name = prop.Name.ToLowerInvariant();
                if (name == "query") continue;
                sqlParams.Add(new SqlParameter("@" + name, prop.Value.GetRawText().Trim('"')));
            }

            return await QuerySql(ToSql(kql), sqlParams);
        }

        private string ToSql(string kql)
        {
            var segments = Split(kql, '|');
            if (segments.Count == 0)
                throw new ArgumentException("Invalid KQL: no table.");

            string table = segments[0].Trim();
            CheckTable(table);

            int top = Math.Min(10, _config.MaxRows);
            var wheres = new List<string>();
            var cols = new List<string>();
            string sort = "";

            foreach (string seg in segments.Skip(1).Select(s => s.Trim()))
            {
                if (seg.StartsWith("where "))
                {
                    string p = seg.Substring(6).Replace("==", "=").Trim();
                    p = Regex.Replace(p, @"\$(\w+)", "@$1");  // $id → @id

                    // ✅ Fixed: only allow "fieldname = @paramname" pattern
                    if (!Regex.IsMatch(p, @"^[\w]+\s*=\s*@\w+$"))
                        throw new ArgumentException("Invalid where clause: '" + p + "'. Only 'field = @param' allowed.");

                    CheckFields(p);
                    wheres.Add(p);
                }
                else if (seg.StartsWith("take "))
                {
                    int.TryParse(seg.Substring(5).Trim(), out top);
                    top = Math.Min(top, _config.MaxRows);
                }
                else if (seg.StartsWith("project "))
                {
                    ParseProject(seg.Substring(8), cols, table);
                }
                else if (seg.StartsWith("order by "))
                {
                    sort = seg.Substring(9).Trim();

                    // ✅ Fixed: only allow plain field name for ORDER BY
                    if (!Regex.IsMatch(sort, @"^[\w]+$"))
                        throw new ArgumentException("Invalid order by field: '" + sort + "'.");

                    CheckFields(sort);
                }
            }

            if (cols.Count > _config.MaxFields)
                throw new InvalidOperationException("Too many fields: " + _config.MaxFields + " limit.");

            var sb = new StringBuilder("select top " + top + " ");
            sb.Append(cols.Count > 0 ? string.Join(",", cols) : "*");
            sb.Append(" from [" + table + "]");
            if (wheres.Count > 0)
                sb.Append(" where " + string.Join(" and ", wheres));
            if (!string.IsNullOrEmpty(sort))
                sb.Append(" order by [" + sort + "]");
            return sb.Append(" for json path").ToString();
        }

        private void ParseProject(string text, List<string> cols, string mainTable)
        {
            foreach (var item in Split(text, ','))
            {
                string f = item.Trim();
                CheckFields(f);
                int eq = f.IndexOf('=');
                if (eq > 0)
                {
                    string alias = f.Substring(0, eq).Trim();
                    string expr = f.Substring(eq + 1).Trim();

                    if (expr.StartsWith("(") && expr.EndsWith(")"))
                    {
                        string subKql = expr.Substring(1, expr.Length - 2);
                        string subSql = ToSql(subKql);
                        cols.Add("(" + subSql + ") as [" + alias + "]");
                    }
                    else
                    {
                        // ✅ Fixed: only allow plain field name as expr
                        if (!Regex.IsMatch(expr, @"^[\w\.\[\]]+$"))
                            throw new ArgumentException("Invalid expression: '" + expr + "'.");

                        cols.Add(expr + " as [" + alias + "]");
                    }
                }
                else
                {
                    cols.Add("[" + f + "]");
                }
            }
        }

        private static List<string> Split(string text, char sep)
        {
            int lv = 0, start = 0;
            var res = new List<string>();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '(') lv++;
                else if (text[i] == ')') lv--;
                else if (text[i] == sep && lv == 0)
                {
                    res.Add(text.Substring(start, i - start));
                    start = i + 1;
                }
            }
            res.Add(text.Substring(start));
            return res;
        }
    }
}