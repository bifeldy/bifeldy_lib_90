using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Models {

    public readonly struct JsonKeyMap {

        public readonly JsonPropertyInfo Property;
        public readonly int Index;

        public JsonKeyMap(JsonPropertyInfo property, int index) {
            this.Property = property;
            this.Index = index;
        }

    }

}
