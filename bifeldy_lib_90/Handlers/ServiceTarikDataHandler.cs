using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Handlers {

    public interface IServiceTarikDataHandler : IServiceBaseHandler {
        string GetSqlQueryFrom();
        string GetAllColumnSelectAsString(IDictionary<string, string> jsonKeysTableCustomColumns = null);
        string GetFullQuery(string sqlCustomQuery = null, IDictionary<string, string> jsonKeysTableCustomColumns = null);
        Task<(decimal, decimal, IAsyncEnumerable<TOutputJson>)> TarikDataPaging<TOutputJson, TInputJson>(JsonTypeInfo<TOutputJson> jsonTypeInfo, HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order, string page, string row) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        IAsyncEnumerable<TOutputJson> TarikDataFullStream<TOutputJson, TInputJson>(JsonTypeInfo<TOutputJson> jsonTypeInfo, HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new();
        Task<(IDictionary<string, string>, string, DynamicParameters)> GetCustomQueryParam<TInputJson>(HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order, string page = null, string row = null) where TInputJson : JsonSerDe, new();
        Task<(string, string, string, IDictionary<string, string>, IDictionary<string, string>, string, DynamicParameters)> GetCustomQueryParamExportDocs<TInputJson>(string rdlcPath, string dsName, string exportAs, HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order, string page = null, string row = null) where TInputJson : JsonSerDe, new();
        Task<bool> ExecuteSebelumTarik<TInputJson>(HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order, string page = null, string row = null) where TInputJson : JsonSerDe, new();
        Task<bool> ExecuteSesudahTarik<TInputJson>(HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order, string page = null, string row = null)where TInputJson : JsonSerDe, new();
    }

    public abstract class CServiceTarikDataHandler : CServiceBaseHandler, IServiceTarikDataHandler {

        protected readonly IApplicationService _app;
        protected readonly IGlobalService _gs;
        protected readonly IHttpService _http;

        //
        // { "key_json", "column_name" }
        // SELECT column_name AS key_json
        //
        // Contoh ::
        // { "tgl_sekarang_tanpa_jam", "TRUNC(COALESCE(b.updrec_date, CURRENT_TIMESTAMP))" }
        // SELECT TRUNC(COALESCE(b.tbl_updrec_date, CURRENT_TIMESTAMP)) AS tgl_sekarang_tanpa_jam
        //
        protected IDictionary<string, string> jsonKeysTableColumns = null;
        //
        // Contoh Hasil Json Akhir 1 & 2
        //
        // {
        //     tgl_sekarang_tanpa_jam: ...
        // }
        //
        // Universal Query Support Oracle & Postgre Database
        // Jangan Pakai Query Spesifik Kusus Database :: Ex. SYSDATE, NOW()
        //
        // Akan Di Gabung Dengan Key Tabel Atas
        //
        // Contoh 1 ::
        // sqlQuery = "FROM dc_tabel_dc_t b";
        // SELECT TRUNC(COALESCE(b.tbl_updrec_date, CURRENT_TIMESTAMP)) AS tgl_sekarang_tanpa_jam
        // FROM dc_tabel_dc_t b
        //
        protected string sqlQuery = null;

        public CServiceTarikDataHandler(
            ILogger<CServiceTarikDataHandler> logger,
            IServiceProvider sp,
            IGlobalService gs,
            IApplicationService app,
            IHttpService http,
            IConverterService cs,
            IGeneralRepository generalRepo
        ) : base(logger, sp, cs, generalRepo) {
            this._gs = gs;
            this._app = app;
            this._http = http;
        }

        private DynamicParameters GetPageRowParamList(decimal qp = 0, decimal qr = 0) {
            var sqlParam = new DynamicParameters();
            sqlParam.Add("page_num", qp);
            sqlParam.Add("row_num", qr);
            return sqlParam;
        }

        private async Task<(decimal, decimal, IAsyncEnumerable<T>)> GetDataPagingWithParam<T>(JsonTypeInfo<T> jsonTypeInfo, IDatabase db, string sort, string order, string page, string row, DynamicParameters sqlParam = null, string sqlCustomQuery = null, IDictionary<string, string> jsonKeysTableCustomColumns = null) where T : JsonSerDe, new() {
            string orderSort = string.Empty;
            if (!string.IsNullOrEmpty(sort) && !string.IsNullOrEmpty(order)) {
                string qs = (jsonKeysTableCustomColumns == null) ? this.jsonKeysTableColumns[sort.ToLower()] : jsonKeysTableCustomColumns[sort.ToLower()];
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

            DynamicParameters defaultSqlParam = this.GetPageRowParamList(qp, qr);
            if (sqlParam == null) {
                sqlParam = defaultSqlParam;
            }
            else {
                foreach (string dsp in defaultSqlParam.ParameterNames) {
                    if (!sqlParam.ParameterNames.Contains(dsp, StringComparer.OrdinalIgnoreCase)) {
                        sqlParam.Add(dsp, defaultSqlParam.Get<object>(dsp));
                    }
                }
            }

            decimal count = await db.ExecScalarAsync<decimal>($"SELECT COUNT(*) {sqlCustomQuery ?? this.sqlQuery}", sqlParam);
            decimal pages = Math.Ceiling(count / ((queryRow is > 0 and <= 500) ? queryRow : 10));

            string query = $@"
                {this.GetFullQuery(sqlCustomQuery, jsonKeysTableCustomColumns)}
                {orderSort}
                LIMIT
                    {qp}
                OFFSET
                    {qr}
            ";

            string alias = $"alias_{DateTime.Now.Ticks}";

            IAsyncEnumerable<T> ls = db.GetAsyncEnumerable(jsonTypeInfo, query, sqlParam);

            return (pages, count, ls);
        }

        private IAsyncEnumerable<T> GetDataFullStreamWithParam<T>(JsonTypeInfo<T> jsonTypeInfo, IDatabase db, DynamicParameters sqlParam = null, string sqlCustomQuery = null, IDictionary<string, string> jsonKeysTableCustomColumns = null, CancellationToken token = default) where T : JsonSerDe, new() {
            string sqlQuery = this.GetFullQuery(sqlCustomQuery, jsonKeysTableCustomColumns);
            return db.GetAsyncEnumerable(jsonTypeInfo, sqlQuery, sqlParam, token: token);
        }

        public string GetSqlQueryFrom() => this.sqlQuery;

        public string GetAllColumnSelectAsString(IDictionary<string, string> jsonKeysTableCustomColumns = null) {
            IDictionary<string, string> jk = jsonKeysTableCustomColumns ?? this.jsonKeysTableColumns;
            return string.Join(", ", jk.Select(p => $"{p.Value} AS {p.Key}"));
        }

        public string GetFullQuery(string sqlCustomQuery = null, IDictionary<string, string> jsonKeysTableCustomColumns = null) {
            return $@"
                SELECT
                    {this.GetAllColumnSelectAsString(jsonKeysTableCustomColumns)}
                    {sqlCustomQuery ?? this.sqlQuery}
            ";
        }

        public async Task<(decimal, decimal, IAsyncEnumerable<TOutputJson>)> TarikDataPaging<TOutputJson, TInputJson>(JsonTypeInfo<TOutputJson> jsonTypeInfo, HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order, string page, string row) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            (IDictionary<string, string> jsonKeysTableCustomColumns, string sqlCustomQuery, DynamicParameters sqlParam) = await this.GetCustomQueryParam(ht, db, fd, searchQuery, sort, order, page, row);
            return await this.GetDataPagingWithParam(jsonTypeInfo, db, sort, order, page, row, sqlParam, sqlCustomQuery, jsonKeysTableCustomColumns);
        }

        public async IAsyncEnumerable<TOutputJson> TarikDataFullStream<TOutputJson, TInputJson>(JsonTypeInfo<TOutputJson> jsonTypeInfo, HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order) where TInputJson : JsonSerDe, new() where TOutputJson : JsonSerDe, new() {
            (IDictionary<string, string> jsonKeysTableCustomColumns, string sqlCustomQuery, DynamicParameters sqlParam) = await this.GetCustomQueryParam(ht, db, fd, searchQuery, sort, order);
            IAsyncEnumerable<TOutputJson> iae = this.GetDataFullStreamWithParam(jsonTypeInfo, db, sqlParam, sqlCustomQuery, jsonKeysTableCustomColumns, ht.RequestAborted);
            await foreach (TOutputJson item in iae) {
                yield return item;
            }
        }

        public virtual Task<(IDictionary<string, string>, string, DynamicParameters)> GetCustomQueryParam<TInputJson>(HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order, string page = null, string row = null) where TInputJson : JsonSerDe, new() {
            (IDictionary<string, string>, string, DynamicParameters) res = (this.jsonKeysTableColumns, this.sqlQuery, null);
            return Task.FromResult(res);
        }

        public virtual async Task<(string, string, string, IDictionary<string, string>, IDictionary<string, string>, string, DynamicParameters)> GetCustomQueryParamExportDocs<TInputJson>(string rdlcPath, string dsName, string exportAs, HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order, string page = null, string row = null) where TInputJson : JsonSerDe, new() {
            (IDictionary<string, string> jsonKeysTableCustomColumns, string sqlCustomQuery, DynamicParameters sqlParam) = await this.GetCustomQueryParam(ht, db, fd, searchQuery, sort, order, page, row);
            (string, string, string, IDictionary<string, string>, IDictionary<string, string>, string, DynamicParameters) result = (rdlcPath, dsName, exportAs, null, jsonKeysTableCustomColumns, sqlCustomQuery, sqlParam);
            return await Task.FromResult(result);
        }

        public virtual Task<bool> ExecuteSebelumTarik<TInputJson>(HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order, string page = null, string row = null) where TInputJson : JsonSerDe, new() {
            return Task.FromResult(false);
        }

        // Tidak Akan Di Panggil Jika Membuat Export File Di Background Job ~
        public virtual Task<bool> ExecuteSesudahTarik<TInputJson>(HttpContext ht, IDatabase db, TInputJson fd, string searchQuery, string sort, string order, string page = null, string row = null) where TInputJson : JsonSerDe, new() {
            return Task.FromResult(false);
        }

    }

}
