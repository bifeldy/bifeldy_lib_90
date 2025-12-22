using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Backgrounds;
using bifeldy_lib_90.Databases;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace bifeldy_lib_90.JobSchedulers {

    public sealed class CleanUpJobScheduler : IBackgroundJob {

        private readonly EnvVar _env;
        private readonly ILogger<CleanUpJobScheduler> _logger;
        private readonly IServiceProvider _sp;
        private readonly IServerConfigRepository _scr;
        private readonly IBerkasService _berkas;
        private readonly CronScheduler _job;

        public CleanUpJobScheduler(
            IOptions<EnvVar> env,
            ILogger<CleanUpJobScheduler> logger,
            IServiceProvider sp,
            IServerConfigRepository scr,
            IBerkasService berkas,
            CronScheduler scheduler
        ) {
            this._env = env.Value;
            this._logger = logger;
            this._sp = sp;
            this._scr = scr;
            this._berkas = berkas;
            this._job = scheduler;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken) {
            try {
                this._berkas.CleanUp(maxOldHours: 8);

                this.CheckRunningJob();

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

        private void CheckRunningJob() {
            try {
                IEnumerable<DynamicJob> ieJobs = this._job
                    .GetAllRunningDynamicJobs()
                    .Where(j => j.StartedAt <= DateTime.UtcNow.AddHours(-2));

                // Job Dibatalkan Karena Sudah Nyangkut Terlalu Lama Lebih Dari 2 Jam
                foreach (DynamicJob job in ieJobs) {
                    _ = this._job.CancelJob(job.Name);
                }
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
