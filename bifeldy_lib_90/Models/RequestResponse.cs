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

    [JsonSerializable(typeof(ResponseJsonSingle<ResponseRedirect>))]
    [JsonSerializable(typeof(ResponseJsonSingle<ResponseJsonMessage>))]
    [JsonSerializable(typeof(ResponseJsonSingle<ResponseJsonErrorApiKeyIpOrigin>))]
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
    public partial class RequestJsonSerializerContext : JsonSerializerContext {
        // This class is used for source generation of JSON serialization metadata
    }

}