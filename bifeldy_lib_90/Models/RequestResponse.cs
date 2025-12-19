using bifeldy_lib_90.Abstractions;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    public abstract class ResponseJson : JsonSerDe {
        [JsonPropertyOrder(1)] public string info { get; set; }
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class ResponseRedirect : ResponseJson {
        [JsonPropertyOrder(2)] public string url { get; set; }
    }

    public sealed class ResponseJsonSingle<T> : ResponseJson {
        [JsonPropertyOrder(2)] public T result { get; set; }
    }

    public sealed class ResponseJsonMulti<T> : ResponseJson {
        [JsonPropertyOrder(2)] public IEnumerable<T> results { get; set; }
        [JsonPropertyOrder(3)] public decimal? pages { get; set; }
        [JsonPropertyOrder(4)] public decimal? count { get; set; }
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public class ResponseJsonMessage : JsonSerDe {
        [JsonPropertyOrder(1)] public string message { get; set; }
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class ResponseJsonErrorApiKeyIpOrigin : ResponseJsonMessage {
        [JsonPropertyOrder(2)] public string api_key { get; set; }
        [JsonPropertyOrder(3)] public string ip_origin { get; set; }
    }

    [JsonSerializable(typeof(ResponseJsonSingle<string>))]
    [JsonSerializable(typeof(ResponseJsonSingle<int>))]
    [JsonSerializable(typeof(ResponseJsonSingle<long>))]
    [JsonSerializable(typeof(ResponseJsonSingle<bool>))]
    [JsonSerializable(typeof(ResponseJsonSingle<decimal>))]
    [JsonSerializable(typeof(ResponseJsonSingle<double>))]
    [JsonSerializable(typeof(ResponseJsonSingle<float>))]
    [JsonSerializable(typeof(ResponseJsonSingle<DateTime>))]
    [JsonSerializable(typeof(ResponseJsonSingle<DateTimeOffset>))]
    [JsonSerializable(typeof(ResponseJsonSingle<Guid>))]
    [JsonSerializable(typeof(ResponseJsonSingle<byte[]>))]
    [JsonSerializable(typeof(ResponseJsonSingle<ResponseRedirect>))]
    [JsonSerializable(typeof(ResponseJsonSingle<ResponseJsonMessage>))]
    [JsonSerializable(typeof(ResponseJsonSingle<ResponseJsonErrorApiKeyIpOrigin>))]
    [JsonSerializable(typeof(ResponseJsonMulti<string>))]
    [JsonSerializable(typeof(ResponseJsonMulti<int>))]
    [JsonSerializable(typeof(ResponseJsonMulti<long>))]
    [JsonSerializable(typeof(ResponseJsonMulti<bool>))]
    [JsonSerializable(typeof(ResponseJsonMulti<decimal>))]
    [JsonSerializable(typeof(ResponseJsonMulti<double>))]
    [JsonSerializable(typeof(ResponseJsonMulti<float>))]
    [JsonSerializable(typeof(ResponseJsonMulti<DateTime>))]
    [JsonSerializable(typeof(ResponseJsonMulti<DateTimeOffset>))]
    [JsonSerializable(typeof(ResponseJsonMulti<Guid>))]
    [JsonSerializable(typeof(ResponseJsonMulti<byte[]>))]
    public partial class ResponseJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

    /* ** */

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public class RequestJson : JsonSerDe {
        [JsonPropertyOrder(1)] public string secret { get; set; }
        [JsonPropertyOrder(2)] public string key { get; set; }
        [JsonPropertyOrder(3)] public string token { get; set; }
        [JsonPropertyOrder(4)] public string server { get; set; }

        public override string[] HiddenProperties() => [
            nameof(this.secret),
            nameof(this.key),
            nameof(this.token),
            nameof(this.server)
        ];
    }

    // Kosongan Bisa Buat Kirim JWT Via Body (POST, PUT, PATCH, Etc.)
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public class InputJson : RequestJson { }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public class InputJsonNonDc : InputJson { }

    public class InputJsonNonDcDataSingle<T> : InputJsonNonDc {
        [JsonPropertyOrder(5)] public T data { get; set; }
    }

    public class InputJsonNonDcDataMulti<T> : InputJsonNonDc {
        [JsonPropertyOrder(5)] public T[] data { get; set; }
    }

    public class InputJsonHoDataSingle<T> : InputJsonNonDcDataSingle<T> { }

    public class InputJsonHoDataMulti<T> : InputJsonNonDcDataMulti<T> { }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public class InputJsonDc : InputJson {
        [JsonPropertyOrder(5)] public string kode_dc { get; set; }
    }

    public class InputJsonDcDataSingle<T> : InputJsonDc {
        [JsonPropertyOrder(6)] public T data { get; set; }
    }

    public class InputJsonDcDataMulti<T> : InputJsonDc {
        [JsonPropertyOrder(6)] public T[] data { get; set; }
    }

    [JsonSerializable(typeof(RequestJson))]
    [JsonSerializable(typeof(InputJson))]
    [JsonSerializable(typeof(InputJsonNonDc))]
    [JsonSerializable(typeof(InputJsonDc))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<string>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<int>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<long>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<bool>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<decimal>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<double>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<float>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<DateTime>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<DateTimeOffset>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<Guid>))]
    [JsonSerializable(typeof(InputJsonDcDataSingle<byte[]>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<string>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<int>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<long>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<bool>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<decimal>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<double>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<float>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<DateTime>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<DateTimeOffset>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<Guid>))]
    [JsonSerializable(typeof(InputJsonHoDataSingle<byte[]>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<string>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<int>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<long>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<bool>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<decimal>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<double>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<float>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<DateTime>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<DateTimeOffset>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<Guid>))]
    [JsonSerializable(typeof(InputJsonNonDcDataSingle<byte[]>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<string>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<int>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<long>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<bool>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<decimal>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<double>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<float>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<DateTime>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<DateTimeOffset>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<Guid>))]
    [JsonSerializable(typeof(InputJsonDcDataMulti<byte[]>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<string>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<int>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<long>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<bool>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<decimal>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<double>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<float>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<DateTime>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<DateTimeOffset>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<Guid>))]
    [JsonSerializable(typeof(InputJsonHoDataMulti<byte[]>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<string>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<int>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<long>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<bool>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<decimal>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<double>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<float>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<DateTime>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<DateTimeOffset>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<Guid>))]
    [JsonSerializable(typeof(InputJsonNonDcDataMulti<byte[]>))]
    public partial class RequestJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}