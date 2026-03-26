using bifeldy_lib_90.Libraries;
using Cronos;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    public sealed class CronJob {
        public required string Name { get; init; }
        public required string Cron { get; init; }
        public required Func<IServiceProvider, CancellationToken, Task> ExecuteAsync { get; init; }
        internal CronExpression Expression => CronExpression.Parse(this.Cron, CronFormat.Standard);
    }

    public abstract class RetryJob {
        public int MaxRetries { get; init; } = 3;
        public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(10);
    }

    public sealed class RuntimeJob : RetryJob {
        public CronJob Job { get; init; } = null!;
        public DateTime NextRunUtc { get; set; }
    }

    public sealed class DynamicJob : RetryJob {
        public string Name { get; init; } = null!;
        public Func<IServiceProvider, CancellationToken, Task> ExecuteAsync { get; init; } = null!;
        public DateTime StartedAt { get; init; }
        public CancellationTokenSource Cts { get; set; } = new();
    }

    public sealed class CompletedJob {
        public string Name { get; init; } = null!;
        public DateTime StartedAt { get; init; }
        public DateTime EndedAt { get; init; }
        public bool Success { get; init; }
    }

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class JobTrackerState {
        public DateTime? LastSuccessfulRunUtc { get; set; }
        public string LastSuccessfulRunLocal { get; set; }
        public DateTime? LastRunUtc { get; set; }
        public string LastRunLocal { get; set; }
        public bool LastRunSuccess { get; set; }
        public string LastErrorMessage { get; set; }
    }

    [JsonSerializable(typeof(Dictionary<string, JobTrackerState>))]
    internal partial class JobTrackerJsonContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}