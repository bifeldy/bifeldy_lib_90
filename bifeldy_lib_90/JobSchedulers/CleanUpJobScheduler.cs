using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Backgrounds;
using bifeldy_lib_90.Databases;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace bifeldy_lib_90.JobSchedulers {

    public sealed class CleanUpJobScheduler : IBackgroundJob {

        private readonly EnvVar _env;
        private readonly ILogger<CleanUpJobScheduler> _logger;
        private readonly IServiceProvider _sp;
        private readonly IServerConfigRepository _scr;
        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;
        private readonly IBerkasService _berkas;
        private readonly IHttpService _http;
        private readonly IConverterService _converter;
        private readonly CronScheduler _job;

        public CleanUpJobScheduler(
            IOptions<EnvVar> env,
            ILogger<CleanUpJobScheduler> logger,
            IServiceProvider sp,
            IServerConfigRepository scr,
            IApplicationService app,
            IGlobalService gs,
            IBerkasService berkas,
            IHttpService http,
            IConverterService converter,
            CronScheduler scheduler
        ) {
            this._env = env.Value;
            this._logger = logger;
            this._sp = sp;
            this._scr = scr;
            this._app = app;
            this._gs = gs;
            this._berkas = berkas;
            this._http = http;
            this._converter = converter;
            this._job = scheduler;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken) {
            try {
                this._berkas.CleanUp(maxOldHours: 8);

                IEnumerable<ServerConfigKunci> kunci = await this._scr.GetKodeServerKunciDc();
                foreach (ServerConfigKunci k in kunci) {
                    try {
                        using (IServiceScope scope = this._sp.CreateScope()) {
                            IServiceProvider sp = scope.ServiceProvider;
                            IServerConfigRepository scr = sp.GetRequiredService<IServerConfigRepository>();

                            _ = await scr.UseKodeServerKunciDc(k.kode_dc, k.kunci_gxxx);

                            IPostgres pg = sp.GetRequiredService<IPostgres>();
                            IGeneralRepository generalRepo = sp.GetRequiredService<IGeneralRepository>();

                            await this.CleanTables(pg);
                            await this.CheckRunningJob(pg);
                        }
                    }
                    catch (Exception ex) {
                        this._logger.LogError("{kodeDc} [{name}_ERROR] ⌚ {ex}", k.kode_dc, this.GetType().Name, ex.Message);
                    }
                }
            }
            catch (Exception e) {
                this._logger.LogError("[{name}_ERROR] ⌚ {ex}", this.GetType().Name, e.Message);
            }
        }

        private async Task CheckRunningJob(IDatabase db) {
            try {
                string sqlQuery = @"
                    SELECT job_name FROM api_quartz_job_queue
                    WHERE
                        UPPER(app_name) = :app_name
                        AND start_at < CURRENT_TIMESTAMP - '2 hours'::INTERVAL
                ";

                var sqlParam = new DynamicParameters();
                sqlParam.Add("app_name", this._app.AppName.ToUpper());

                IEnumerable<string> lsJob = await db.GetEnumerableAsync<string>(sqlQuery, sqlParam);

                IEnumerable<DynamicJob> ieJobs = this._job.GetAllRunningDynamicJobs().Where(j => {
                    bool ok = false;

                    foreach (string job in lsJob) {
                        if (j.Name == job) {
                            ok = true;
                            break;
                        }
                    }

                    return ok;
                });


                foreach (DynamicJob job in ieJobs) {
                    bool res = this._job.CancelJob(job.Name);
                    if (res) {
                        // "Job Dibatalkan Karena Sudah Nyangkut Terlalu Lama Lebih Dari 2 Jam"
                    }
                }

                sqlQuery = $@"
                    DELETE FROM api_quartz_job_queue
                    WHERE
                        app_name = :app_name
                        AND start_at < CURRENT_TIMESTAMP - '8 hours'::INTERVA
                ";

                _ = await db.ExecQueryAsync(sqlQuery, sqlParam);
            }
            catch (Exception e) {
                this._logger.LogError("[{name}_JOBS_CLEANER] ⌚ {ex}", this.GetType().Name, e.Message);
            }
        }

        private async Task CleanTables(IDatabase db) {
            try {
                _ = await db.ExecQueryAsync(
                    $@"
                        DELETE FROM api_log_send_t
                        WHERE
                            last_run < CURRENT_DATE - {this._env.MAX_RETENTIONS_DAYS}
                            OR last_run >= CURRENT_DATE + 1
                    "
                );
            }
            catch (Exception e) {
                this._logger.LogError("[{name}_CLEAN_TABLES] ⌚ {ex}", this.GetType().Name, e.Message);
            }
        }

    }

}
