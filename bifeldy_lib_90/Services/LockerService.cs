namespace bifeldy_lib_90.Services {

    public interface ILockerService {
        SemaphoreSlim MutexGlobalApp { get; }
        SemaphoreSlim SemaphoreGlobalApp(string name, int initialCount = 1, int maximumCount = 1);
        void ClearAndRemove(string name);
    }

    public sealed class CLockerService : ILockerService {

        private readonly IDictionary<string, SemaphoreSlim> semaphore_global_app = new Dictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

        public CLockerService() {
            this.MutexGlobalApp = new SemaphoreSlim(1, 1);
        }

        public SemaphoreSlim MutexGlobalApp { get; } = null;

        public SemaphoreSlim SemaphoreGlobalApp(string name, int initialCount = 1, int maximumCount = 1) {
            try {
                _ = this.MutexGlobalApp.Wait(-1);

                if (!this.semaphore_global_app.TryGetValue(name, out SemaphoreSlim value)) {
                    value = new SemaphoreSlim(initialCount, maximumCount);
                    this.semaphore_global_app.Add(name, value);
                }

                return value;
            }
            finally {
                _ = this.MutexGlobalApp.Release();
            }
        }

        public void ClearAndRemove(string name) {
            try {
                _ = this.MutexGlobalApp.Wait(-1);

                if (this.semaphore_global_app.ContainsKey(name)) {
                    if (this.semaphore_global_app[name].CurrentCount <= 0) {
                        _ = this.semaphore_global_app[name].Release();
                    }

                    _ = this.semaphore_global_app.Remove(name);
                }
            }
            finally {
                _ = this.MutexGlobalApp.Release();
            }
        }

    }

}