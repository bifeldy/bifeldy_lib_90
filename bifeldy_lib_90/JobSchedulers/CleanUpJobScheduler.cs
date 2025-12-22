using bifeldy_lib_90.Backgrounds;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using Microsoft.Extensions.Logging;

namespace bifeldy_lib_90.JobSchedulers {

    public sealed class CleanUpJobScheduler : IBackgroundJob {

        private readonly ILogger<CleanUpJobScheduler> _logger;
        private readonly IBerkasService _berkas;
        private readonly CronScheduler _job;

        public CleanUpJobScheduler(
            ILogger<CleanUpJobScheduler> logger,
            IBerkasService berkas,
            CronScheduler scheduler
        ) {
            this._logger = logger;
            this._berkas = berkas;
            this._job = scheduler;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken) {
            try {
                this._berkas.CleanUp(maxOldHours: 8);

                IEnumerable<DynamicJob> ieJobs = this._job
                    .GetAllRunningDynamicJobs()
                    .Where(j => j.StartedAt <= DateTime.UtcNow.AddHours(-4));

                // Job Dibatalkan Karena Sudah Nyangkut Terlalu Lama Lebih Dari 2 Jam
                foreach (DynamicJob job in ieJobs) {
                    _ = this._job.CancelJob(job.Name);
                }

                await Task.CompletedTask;
            }
            catch (Exception e) {
                this._logger.LogError("[{name}_ERROR] ⌚ {ex}", this.GetType().Name, e.Message);
            }
        }

    }

}
