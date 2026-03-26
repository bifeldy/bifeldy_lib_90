using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Services;
using System.Text.Json;

namespace bifeldy_lib_90.JobSchedulers {

    public interface IJobTracker {
        Task<DateTime?> GetLastSuccessfulRunAsync(string jobName, CancellationToken ct);
        Task RecordJobHistoryAsync(string jobName, DateTime startedAt, DateTime endedAt, bool success, string errorMessage, CancellationToken ct);
    }

    public sealed class CJobTracker : IJobTracker {

        private readonly string _filePath = null;
        private readonly JobTrackerJsonContext _jsonContext = null;

        private readonly ILockerService _locker;

        public CJobTracker(
            IApplicationService app,
            ILockerService locker
        ) {
            this._locker = locker;

            this._filePath = Path.Combine(app.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "JobScheduler.json");

            var jsonSerializerOptions = new JsonSerializerOptions() {
                WriteIndented = true
            };
            jsonSerializerOptions.Converters.Add(new DecimalConverter());
            jsonSerializerOptions.Converters.Add(new NullableDecimalConverter());

            this._jsonContext = new JobTrackerJsonContext(jsonSerializerOptions);
        }

        private async Task<Dictionary<string, JobTrackerState>> LoadStateAsync(CancellationToken ct) {
            if (!File.Exists(this._filePath)) {
                return [];
            }

            try {
                using (var stream = new FileStream(this._filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    Dictionary<string, JobTrackerState> state = await JsonSerializer.DeserializeAsync(
                        stream,
                        this._jsonContext.DictionaryStringJobTrackerState,
                        ct
                    );

                    return state ?? [];
                }
            }
            catch {
                return [];
            }
        }

        private async Task SaveStateAsync(Dictionary<string, JobTrackerState> state, CancellationToken ct) {
            using (var stream = new FileStream(this._filePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                await JsonSerializer.SerializeAsync(
                    stream,
                    state,
                    this._jsonContext.DictionaryStringJobTrackerState,
                    ct
                );
            }
        }

        public async Task<DateTime?> GetLastSuccessfulRunAsync(string jobName, CancellationToken ct) {
            _ = await this._locker.MutexGlobalApp.WaitAsync(-1, ct);

            try {
                Dictionary<string, JobTrackerState> state = await this.LoadStateAsync(ct);
                if (state.TryGetValue(jobName, out JobTrackerState jobState)) {
                    return jobState.LastSuccessfulRunUtc;
                }

                return null;
            }
            finally {
                _ = this._locker.MutexGlobalApp.Release();
            }
        }

        public async Task RecordJobHistoryAsync(string jobName, DateTime startedAt, DateTime endedAt, bool success, string errorMessage, CancellationToken ct) {
            _ = await this._locker.MutexGlobalApp.WaitAsync(-1, ct);

            try {
                Dictionary<string, JobTrackerState> state = await this.LoadStateAsync(ct);

                if (!state.TryGetValue(jobName, out JobTrackerState jobState)) {
                    jobState = new JobTrackerState();
                    state[jobName] = jobState;
                }

                jobState.LastRunUtc = endedAt;
                jobState.LastRunLocal = endedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                jobState.LastRunSuccess = success;
                jobState.LastErrorMessage = errorMessage;

                if (success) {
                    jobState.LastSuccessfulRunUtc = endedAt;
                    jobState.LastSuccessfulRunLocal = endedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                }

                await this.SaveStateAsync(state, ct);
            }
            finally {
                _ = this._locker.MutexGlobalApp.Release();
            }
        }

    }

}