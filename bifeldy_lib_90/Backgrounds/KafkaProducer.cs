using bifeldy_lib_90.Databases;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using bifeldy_lib_90.TableView;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;

namespace bifeldy_lib_90.Backgrounds {

    public sealed class KafkaProducer : BackgroundService {

        private readonly ILogger<KafkaProducer> _logger;
        private readonly IPubSubService _pubSub;
        private readonly IKafkaService _kafka;
        private readonly ILockerService _locker;

        private readonly string _hostPort;
        private readonly string _topicName;
        private readonly short _replication;
        private readonly int _partition;

        private readonly bool _suffixKodeDc;
        private readonly string _pubSubName;

        private readonly List<EJenisDc> _excludeJenisDc;

        private readonly IServiceScope _scopedService = null;

        private string KAFKA_NAME => "KAFKA_" + this._pubSubName ?? $"PRODUCER_{this._hostPort.ToUpper()}#{this._topicName.ToUpper()}";

        public KafkaProducer(
            IServiceProvider serviceProvider,
            string hostPort, string topicName, short replication = 1, int partition = 1,
            bool suffixKodeDc = false, List<EJenisDc> excludeJenisDc = null, string pubSubName = null
        ) {
            this._logger = serviceProvider.GetRequiredService<ILogger<KafkaProducer>>();
            this._pubSub = serviceProvider.GetRequiredService<IPubSubService>();
            this._kafka = serviceProvider.GetRequiredService<IKafkaService>();
            this._locker = serviceProvider.GetRequiredService<ILockerService>();

            this._scopedService = serviceProvider.CreateScope();

            this._hostPort = hostPort;
            this._topicName = topicName;
            this._replication = replication;
            this._partition = partition;

            this._suffixKodeDc = suffixKodeDc;
            this._pubSubName = pubSubName;
            this._excludeJenisDc = excludeJenisDc;
        }

        public override void Dispose() {
            this._pubSub?.DisposeAndRemoveSubscriber(this.KAFKA_NAME);
            this._scopedService?.Dispose();
            base.Dispose();
        }

        private async Task DoWorkMultiDc(IServiceProvider sp, CancellationToken stoppingToken) {
            BehaviorSubject<Message<string, string>> observeable = null;
            IProducer<string, string> producer = null;

            IDisposable subs = null;
            List<Message<string, string>> msgs = [];

            try {
                IPostgres pg = sp.GetRequiredService<IPostgres>();
                IGeneralRepository generalRepo = sp.GetRequiredService<IGeneralRepository>();

                if (this._excludeJenisDc != null) {
                    EJenisDc jenisDc = await generalRepo.GetJenisDc(pg);
                    if (this._excludeJenisDc.Contains(jenisDc)) {
                        return;
                    }
                }

                string hostPort = this._hostPort;
                string topicName = this._topicName;
                short replication = this._replication;
                int partition = this._partition;

                if (
                    string.IsNullOrEmpty(hostPort) ||
                    string.IsNullOrEmpty(topicName)
                ) {
                    KAFKA_SERVER_T kafka = await generalRepo.GetKafkaServerInfo(pg, topicName);
                    if (kafka == null) {
                        throw new Exception("KAFKA Tidak Tersedia");
                    }

                    if (string.IsNullOrEmpty(hostPort)) {
                        hostPort = $"{kafka.HOST}:{kafka.PORT}";
                    }

                    if (string.IsNullOrEmpty(topicName)) {
                        topicName = kafka.TOPIC;
                    }

                    if (replication <= 0) {
                        replication = (short)kafka.REPLI;
                    }

                    if (partition <= 0) {
                        partition = (int)kafka.PARTI;
                    }
                }

                if (this._suffixKodeDc) {
                    string kodeDc = await generalRepo.GetKodeDc(pg);
                    if (!topicName.ToLower().EndsWith($"_{kodeDc.ToLower()}")) {
                        if (!topicName.EndsWith("_")) {
                            topicName += "_";
                        }

                        topicName += kodeDc;
                    }
                }

                await this._kafka.CreateTopicIfNotExist(hostPort, topicName, replication, partition);
                producer = this._kafka.CreateKafkaProducerInstance<string, string>(hostPort);

                observeable = this._pubSub.GetGlobalAppBehaviorSubject<Message<string, string>>(this.KAFKA_NAME);
                subs = observeable.Subscribe(async data => {
                    if (data != null) {
                        _ = await this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).WaitAsync(-1, stoppingToken);

                        try {
                            var msg = new Message<string, string>() {
                                Key = data.Key,
                                Value = data.Value
                            };

                            msgs.Add(msg);
                        }
                        finally {
                            _ = this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).Release();
                        }
                    }
                });

                while (!stoppingToken.IsCancellationRequested) {
                    await Task.Yield();

                    if (msgs.Count > 0) {
                        _ = await this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).WaitAsync(-1, stoppingToken);

                        try {
                            Message<string, string>[] cpMsgs = [.. msgs];
                            msgs.Clear();

                            foreach (Message<string, string> msg in cpMsgs) {
                                try {
                                    _ = await producer.ProduceAsync(topicName, msg, stoppingToken);
                                }
                                catch (Exception e) {
                                    this._logger.LogError("[KAFKA_PRODUCER_MESSAGE] {e}", e.Message);
                                }
                            }
                        }
                        finally {
                            _ = this._locker.SemaphoreGlobalApp(this.KAFKA_NAME).Release();
                        }
                    }
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[KAFKA_PRODUCER_ERROR] 🏗 {ex}", ex.Message);
            }
            finally {
                subs?.Dispose();
                producer?.Dispose();
                observeable?.Dispose();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                var tasks = new List<Task>();

                IServerConfigRepository scr = this._scopedService.ServiceProvider.GetRequiredService<IServerConfigRepository>();

                IEnumerable<ServerConfigKunci> kunci = await scr.GetKodeServerKunciDc();
                foreach (ServerConfigKunci k in kunci) {
                    Task task = await Task.Factory.StartNew(async () => {
                        using (IServiceScope scope = this._scopedService.ServiceProvider.CreateScope()) {
                            IServiceProvider _sp = scope.ServiceProvider;
                            IServerConfigRepository _scr = _sp.GetRequiredService<IServerConfigRepository>();

                            _ = await _scr.UseKodeServerKunciDc(k.kode_dc, k.kunci_gxxx);

                            await this.DoWorkMultiDc(_sp, stoppingToken);
                        }
                    }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex) {
                this._logger.LogError("[KAFKA_PRODUCER_HOST] 💉 {ex}", ex.Message);
            }
        }

    }

}