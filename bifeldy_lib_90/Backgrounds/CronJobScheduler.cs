using bifeldy_lib_90.Libraries;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace bifeldy_lib_90.Backgrounds {

    public sealed class CronJobScheduler : BackgroundService {

        private readonly IServiceProvider _services;
        private readonly ILogger<CronJobScheduler> _logger;
        private readonly Dictionary<string, RuntimeJob> _jobs = new();

        public CronJobScheduler(IServiceProvider services, ILogger<CronJobScheduler> logger) {
            this._services = services;
            this._logger = logger;

            this.InitializeJobs();
        }

        private void InitializeJobs() {
            var now = DateTime.UtcNow;

            foreach (var reg in this._jobs.Values) {
                var cron = CronExpression.Parse(
                    reg.Cron,
                    CronFormat.IncludeSeconds);

                var nextRun = cron.GetNextOccurrence(
                    now,
                    TimeZoneInfo.Utc);

                if (nextRun is null)
                    continue; // invalid or disabled schedule

                var job = new RuntimeJob(
                    id: reg.Id,
                    name: reg.Name,
                    jobType: reg.JobType,
                    cron: cron,
                    nextRunUtc: nextRun.Value
                );

                _jobs.Add(job);
            }
        }

        private async Task ExecuteJobAsync(IServiceProvider sp, CancellationToken ct, RuntimeJob job) {
            using (IServiceScope scope = sp.CreateScope()) {
                var service = scope.ServiceProvider.GetRequiredService(job.JobType);
                var backgroundJob = (IBackgroundJob)service;
                await backgroundJob.ExecuteAsync(ct);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

            while (await timer.WaitForNextTickAsync(stoppingToken)) {
                DateTime now = DateTime.UtcNow;

                foreach (RuntimeJob job in this._jobs.Values) {
                    if (job.NextRunUtc > now) {
                        continue;
                    }

                    _ = this.RunJobAsync(job, stoppingToken);
                }
            }
        }

        private async Task RunJobAsync(RuntimeJob job, CancellationToken appToken) {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(job.Cts.Token, appToken)) {

                try {
                    await this.ExecuteJobAsync(this._services, linked.Token, job);

                    job.NextRunUtc = job.Cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc).Value;
                    job.RetryCount = 0;
                    job.Cts = new CancellationTokenSource();
                }
                catch (OperationCanceledException) {
                    // expected
                }
                catch (Exception ex) {
                    job.RetryCount++;

                    TimeSpan delay = Backoff.ComputeDelay(job.RetryCount);

                    job.NextRunUtc = DateTime.UtcNow.Add(delay);

                    this._logger.LogWarning(ex, "Job {Job} failed, retry {Retry}", job.Name, job.RetryCount);
                }
            }
        }

    }

}
