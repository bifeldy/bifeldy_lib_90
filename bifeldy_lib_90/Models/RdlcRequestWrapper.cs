using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Libraries;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Models {

    [JsonSourceGenerationOptions(Converters = new[] { typeof(DecimalConverter), typeof(NullableDecimalConverter) })]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public sealed class RdlcRequestWrapper<T> : JsonSerDe {
        public IAsyncEnumerable<T> DataRowList { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }

}
