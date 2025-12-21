using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace bifeldy_lib_90.JobSchedulers {

    public sealed class ScheduleBuilder {

        private readonly string _cron;
        private readonly IServiceCollection _services;

        public readonly List<CronJob> _jobs = [];

        public ScheduleBuilder(string cron, IServiceCollection services) {
            this._cron = cron;
            this._services = services;
        }

        public ScheduleBuilder AddJob<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TJob>() where TJob : class, IBackgroundJob {
            if (_jobs.Any(j => j.Name == typeof(TJob).Name)) {
                return this;
            }

            _ = this._services.AddSingleton<TJob>();

            _jobs.Add(new CronJob() {
                Name = typeof(TJob).Name,
                Cron = _cron,
                ExecuteAsync = async (sp, ct) => {
                    TJob jobInstance = sp.GetRequiredService<TJob>();
                    if (jobInstance is IBackgroundJob bg) {
                        await bg.ExecuteAsync(ct);
                    }
                }
            });

            return this;
        }

    }

}
