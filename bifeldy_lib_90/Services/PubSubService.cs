using System.Reactive.Subjects;

namespace bifeldy_lib_90.Services {

    public interface IPubSubService {
        bool IsExist(string key);
        BehaviorSubject<T> CreateNewBehaviorSubject<T>(T initialValue);
        BehaviorSubject<T> GetGlobalAppBehaviorSubject<T>(string key);
        BehaviorSubject<T> CreateGlobalAppBehaviorSubject<T>(string key, T initialValue);
        void DisposeAndRemoveSubscriber(string key);
        IEnumerable<string> ListAllKeys();
    }

    public sealed class CPubSubService : IPubSubService {

        private readonly IDictionary<string, object> keyValuePairs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public CPubSubService() {
            //
        }

        public bool IsExist(string key) => this.keyValuePairs.ContainsKey(key);

        public BehaviorSubject<T> CreateNewBehaviorSubject<T>(T initialValue) => new(initialValue);

        public BehaviorSubject<T> GetGlobalAppBehaviorSubject<T>(string key) {
            if (string.IsNullOrEmpty(key)) {
                throw new Exception("Nama Key Wajib Diisi");
            }

            if (!this.keyValuePairs.TryGetValue(key, out object value)) {
                return this.CreateGlobalAppBehaviorSubject(key, default(T));
            }

            return (BehaviorSubject<T>)value;
        }

        public BehaviorSubject<T> CreateGlobalAppBehaviorSubject<T>(string key, T initialValue) {
            if (string.IsNullOrEmpty(key)) {
                throw new Exception("Nama Key Wajib Diisi");
            }

            if (!this.keyValuePairs.TryGetValue(key, out object value)) {
                value = this.CreateNewBehaviorSubject(initialValue);
                this.keyValuePairs.Add(key, value);
            }

            return (BehaviorSubject<T>)value;
        }

        public void DisposeAndRemoveSubscriber(string key) {
            if (this.keyValuePairs.ContainsKey(key)) {
                if (this.keyValuePairs[key] is IDisposable disposable) {
                    disposable.Dispose();
                }

                _ = this.keyValuePairs.Remove(key);
            }
        }

        public IEnumerable<string> ListAllKeys() {
            return this.keyValuePairs.Keys;
        }

    }

}