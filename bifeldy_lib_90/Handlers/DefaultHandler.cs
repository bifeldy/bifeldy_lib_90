using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Exceptions;
using Dapper;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Handlers {

    public interface IDefaultHandler {
        Task<(List<T>, ulong, ulong)> GetListDataPaging<T>(IDatabase db, string sqlQuery, DynamicParameters sqlParam, JsonTypeInfo<T> jsonTypeInfo, string page, string row, string sort, string order) where T : JsonSerDe, new();
    }

    public sealed class CDefaultHandler : IDefaultHandler {

        public CDefaultHandler() {
            //
        }

        public async Task<(List<T>, ulong, ulong)> GetListDataPaging<T>(
            IDatabase db, string sqlQuery, DynamicParameters sqlParam,
            JsonTypeInfo<T> jsonTypeInfo,
            string page, string row, string sort, string order
        ) where T : JsonSerDe, new() {
            JsonPropertyInfo pi = jsonTypeInfo.Properties
                .FirstOrDefault(ak => ak.Name.Equals(sort, StringComparison.OrdinalIgnoreCase));

            if (pi == null) {
                throw new TidakMemenuhiException($"Tidak Dapat Mengurutkan Berdasarkan `{sort}`");
            }

            string orderSort = string.Empty;
            if (!string.IsNullOrEmpty(sort) && !string.IsNullOrEmpty(order)) {
                string qs = sort;
                string qo = order.ToLower() == "desc" ? "DESC" : "ASC";
                qs = qs.ToLower().Replace("distinct", string.Empty);
                orderSort = $@"
                    ORDER BY
                        {qs} {qo}
                ";
            }

            ulong queryPage = string.IsNullOrEmpty(page) ? 1 : ulong.Parse(page);
            ulong queryRow = string.IsNullOrEmpty(row) ? 10 : ulong.Parse(row);

            ulong qp = (queryRow > 0 && queryRow <= 500) ? queryRow : 10;
            ulong qr = queryPage > 0 ? (queryPage * queryRow) - queryRow : 0;

            string limitOffset = $@"
                LIMIT
                    {qp}
                OFFSET
                    {qr}
            ";

            sqlParam.Add("page_num", (decimal)qp);
            sqlParam.Add("row_num", (decimal)qr);

            ulong count = await db.ExecScalarAsync<ulong>($"SELECT COUNT(*) FROM ({sqlQuery}) temp_{DateTime.Now.Ticks}", sqlParam);
            ulong pages = (ulong)Math.Ceiling((decimal)count / ((queryRow is > 0 and <= 500) ? queryRow : 10));

            List<T> ls = await db.GetListAsync(
                jsonTypeInfo,
                $@"
                    SELECT
                        *
                    FROM
                        ({sqlQuery}) temp_{DateTime.Now.Ticks}
                    {orderSort}
                    {limitOffset}
                ",
                sqlParam
            );

            return (ls, count, pages);
        }

    }

}