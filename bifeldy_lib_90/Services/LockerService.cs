using System.Collections.Concurrent;

namespace bifeldy_lib_90.Services {

    public interface ILockerService {
        SemaphoreSlim MutexGlobalApp { get; }
        SemaphoreSlim SemaphoreGlobalApp(string name, int initialCount = 1, int maximumCount = 1);
    }

    public sealed class CLockerService : ILockerService {

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new(StringComparer.OrdinalIgnoreCase);

        public CLockerService() {
            this.MutexGlobalApp = new SemaphoreSlim(1, 1);
        }

        public SemaphoreSlim MutexGlobalApp { get; } = null;

        public SemaphoreSlim SemaphoreGlobalApp(string name, int initialCount = 1, int maximumCount = 1) {
            _ = this.MutexGlobalApp.Wait(-1);

            try {
                return this._semaphores.GetOrAdd(name, _ => new SemaphoreSlim(initialCount, maximumCount));
            }
            finally {
                _ = this.MutexGlobalApp.Release();
            }
        }

    }

}