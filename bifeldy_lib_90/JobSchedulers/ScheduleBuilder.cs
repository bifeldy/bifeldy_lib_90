using bifeldy_lib_90.Models;
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
            if (this._jobs.Any(j => j.Name == typeof(TJob).Name)) {
                return this;
            }

            _ = this._services.AddScoped<TJob>();

            this._jobs.Add(new CronJob() {
                Name = typeof(TJob).Name,
                Cron = this._cron,
                ExecuteAsync = async (sp, ct) => {
                    TJob jobInstance = sp.GetRequiredService<TJob>();
                    IJobTracker tracker = sp.GetRequiredService<IJobTracker>();

                    DateTime startTime = DateTime.UtcNow;
                    bool success = false;
                    string errorMessage = null;

                    try {
                        if (jobInstance is IBackgroundJob bg) {
                            await bg.ExecuteAsync(ct);
                        }

                        success = true;
                    }
                    catch (Exception ex) {
                        errorMessage = ex.Message;
                        throw;
                    }
                    finally {
                        DateTime endTime = DateTime.UtcNow;
                        await tracker.RecordJobHistoryAsync(typeof(TJob).Name, startTime, endTime, success, errorMessage, ct);
                    }
                }
            });

            return this;
        }

    }

}