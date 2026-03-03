using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Text;

namespace LightDataSuite
{
    public class KqlEngine : BaseDataEngine, IKqlEngine
    {
        public KqlEngine(string conn, string tables, string blocks) : base(conn, tables, blocks) { }

        public async Task<string> Run(JsonElement root)
        {
            if (!root.TryGetProperty("query", out JsonElement q))
                throw new ArgumentException("json must have a 'query' field.");

            // Force query to lowercase
            string kql = (q.GetString() ?? "").ToLower().Trim();
            var sqlParams = new List<SqlParameter>();

            foreach (JsonProperty prop in root.EnumerateObject())
            {
                string name = prop.Name.ToLower();
                if (name == "query") continue;
                sqlParams.Add(new SqlParameter($"@{name}", prop.Value.GetRawText().Trim('"')));
            }

            return await QuerySql(ToSql(kql), sqlParams);
        }

        private string ToSql(string kql)
        {
            var segments = Split(kql, '|');
            string table = segments[0].Trim(); // Already lowercase
            CheckTable(table);

            int top = 10;
            var wheres = new List<string>();
            var cols = new List<string>();
            string sort = "";

            foreach (string seg in segments.Skip(1).Select(s => s.Trim()))
            {
                if (seg.StartsWith("where "))
                {
                    var p = seg[6..].Replace("==", "=").Trim();
                    CheckFields(p); wheres.Add(p);
                }
                else if (seg.StartsWith("take ")) int.TryParse(seg[5..], out top);
                else if (seg.StartsWith("project ")) ParseProject(seg[8..], cols);
                else if (seg.StartsWith("order by ")) { sort = seg[9..].Trim(); CheckFields(sort); }
            }

            var sb = new StringBuilder($"select top {top} ");
            sb.Append(cols.Count > 0 ? string.Join(",", cols) : "*");
            sb.Append($" from [{table}]");
            if (wheres.Count > 0) sb.Append(" where " + string.Join(" and ", wheres));
            if (!string.IsNullOrEmpty(sort)) sb.Append($" order by {sort}");
            return sb.Append(" for json path").ToString();
        }

        private void ParseProject(string text, List<string> cols)
        {
            foreach (var item in Split(text, ','))
            {
                string f = item.Trim(); CheckFields(f);
                int eq = f.IndexOf('=');
                if (eq > 0)
                {
                    string n = f[..eq].Trim(), r = f[(eq + 1)..].Trim();
                    cols.Add(r.StartsWith('(') ? $"({ToSql(r[1..^1])}) as [{n}]" : $"{r} as [{n}]");
                }
                else cols.Add($"[{f}]");
            }
        }

        private static List<string> Split(string text, char sep)
        {
            int lv = 0, start = 0; var res = new List<string>();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '(') lv++;
                else if (text[i] == ')') lv--;
                else if (text[i] == sep && lv == 0) { res.Add(text[start..i]); start = i + 1; }
            }
            res.Add(text[start..]); return res;
        }
    }
}