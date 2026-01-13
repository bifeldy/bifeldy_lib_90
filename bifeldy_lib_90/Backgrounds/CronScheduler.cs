using bifeldy_lib_90.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace bifeldy_lib_90.Backgrounds {

    public sealed class CronScheduler : BackgroundService {

        private readonly IServiceProvider _root;
        private readonly ILogger<CronScheduler> _logger;

        private readonly List<RuntimeJob> _cronJobs;
        private readonly ConcurrentDictionary<string, (Task Task, CancellationTokenSource Cts)> _runningJobs = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentQueue<CompletedJob> _completedJobs = new();
        private readonly ConcurrentQueue<DynamicJob> _dynamicJobs = new();

        public CronScheduler(
            IServiceProvider root,
            ILogger<CronScheduler> logger,
            IEnumerable<CronJob> jobs
        ) {
            this._root = root;
            this._logger = logger;

            this._cronJobs = [.. jobs.Select(j => new RuntimeJob() {
                Job = j,
                NextRunUtc = GetNext(j, DateTime.UtcNow)
            })];
        }

        public void EnqueueDynamicJob(string name, Func<IServiceProvider, CancellationToken, Task> execute, int maxRetries = 3, TimeSpan? retryDelay = null, DateTime? startedAt = null) {
            if (string.IsNullOrEmpty(name)) {
                name = Guid.NewGuid().ToString();
            }
            
            var dj = new DynamicJob() {
                Name = name,
                ExecuteAsync = execute,
                StartedAt = startedAt ?? DateTime.UtcNow,
                MaxRetries = maxRetries,
                RetryDelay = retryDelay ?? TimeSpan.FromSeconds(5)
            };


            this._dynamicJobs.Enqueue(dj);
        }

        public RuntimeJob[] GetAllRunningRecurringJobs() => [.. this._cronJobs];
        public DynamicJob[] GetAllRunningDynamicJobs() => [.. this._dynamicJobs];

        public IReadOnlyCollection<CompletedJob> GetAllCompletedJobs() => this._completedJobs.ToArray();

        public CompletedJob CheckJobIsCompleted(string name) => this._completedJobs.FirstOrDefault(j => j.Name == name);

        public bool CancelJob(string name) {
            if (this._runningJobs.TryGetValue(name, out (Task Task, CancellationTokenSource Cts) entry)) {
                entry.Cts.Cancel();
                return true;
            }

            return false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                DateTime now = DateTime.UtcNow;

                // Handle recurring cron jobs
                foreach (RuntimeJob job in this._cronJobs) {
                    if (job.NextRunUtc <= now) {
                        this.StartJob(job.Job.Name, job.Job.ExecuteAsync, job.MaxRetries, job.RetryDelay);
                        job.NextRunUtc = GetNext(job.Job, now);
                    }
                }

                // Handle dynamic jobs
                while (this._dynamicJobs.TryDequeue(out DynamicJob dynamicJob)) {
                    this.StartJob(dynamicJob.Name, dynamicJob.ExecuteAsync, dynamicJob.MaxRetries, dynamicJob.RetryDelay);
                }

                await Task.Delay(500, stoppingToken);
            }
        }

        private void StartJob(
            string name,
            Func<IServiceProvider, CancellationToken, Task> execute,
            int maxRetries = 3,
            TimeSpan? retryDelay = null
        ) {
            if (this._runningJobs.ContainsKey(name)) {
                return;
            }

            var cts = new CancellationTokenSource();
            TimeSpan delay = retryDelay ?? TimeSpan.FromSeconds(5);

            var task = Task.Run(async () => {
                DateTime started = DateTime.UtcNow;
                bool success = false;
                int attempt = 0;

                while (!success && attempt <= maxRetries) {
                    attempt++;

                    try {
                        using (IServiceScope scope = this._root.CreateScope()) {
                            await execute(scope.ServiceProvider, cts.Token);
                            success = true;
                        }
                    }
                    catch (OperationCanceledException) {
                        this._logger.LogWarning("Job cancelled: {Job}", name);
                        break; // do not retry if cancelled
                    }
                    catch (Exception ex) {
                        this._logger.LogError(ex, "Job failed (Attempt {Attempt}/{Max}): {Job}", attempt, maxRetries, name);
                        if (attempt <= maxRetries) {
                            await Task.Delay(delay, cts.Token);
                        }
                    }
                }

                DateTime ended = DateTime.UtcNow;

                var cj = new CompletedJob() {
                    Name = name,
                    StartedAt = started,
                    EndedAt = ended,
                    Success = success
                };

                this._completedJobs.Enqueue(cj);

                _ = this._runningJobs.TryRemove(name, out _);

                while (this._completedJobs.TryPeek(out CompletedJob oldJob) && oldJob.EndedAt < DateTime.UtcNow.AddMinutes(-10)) {
                    _ = this._completedJobs.TryDequeue(out _);
                }
            });

            _ = this._runningJobs.TryAdd(name, (task, cts));
        }

        private static DateTime GetNext(CronJob job, DateTime nowUtc) {
            return job.Expression.GetNextOccurrence(nowUtc, TimeZoneInfo.Utc) ?? nowUtc.AddMinutes(1);
        }
    }

}