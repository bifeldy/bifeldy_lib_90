using bifeldy_lib_90.Extensions;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace bifeldy_lib_90.Libraries {

    public sealed class DecimalConverter : JsonConverter<decimal> {

        public override decimal Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) {
            switch (reader.TokenType) {
                case JsonTokenType.Number:
                    return reader.GetDecimal().RemoveTrail();

                case JsonTokenType.String:
                    string s = reader.GetString();

                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d)) {
                        return d.RemoveTrail();
                    }

                    break;
            }

            throw new JsonException($"Unexpected token {reader.TokenType} when parsing decimal");
        }

        public override void Write(
            Utf8JsonWriter writer,
            decimal value,
            JsonSerializerOptions options
        ) {
            writer.WriteNumberValue(value.RemoveTrail());
        }

    }

    public sealed class NullableDecimalConverter : JsonConverter<decimal?> {

        public override decimal? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) {
            switch (reader.TokenType) {
                case JsonTokenType.Null:
                    return null;

                case JsonTokenType.Number:
                    return reader.GetDecimal().RemoveTrail();

                case JsonTokenType.String:
                    string s = reader.GetString();

                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d)) {
                        return d.RemoveTrail();
                    }

                    return null;
            }

            throw new JsonException($"Unexpected token {reader.TokenType} when parsing decimal?");
        }

        public override void Write(
            Utf8JsonWriter writer,
            decimal? value,
            JsonSerializerOptions options
        ) {
            if (value.HasValue) {
                writer.WriteNumberValue(value.Value.RemoveTrail());
            }
            else {
                writer.WriteNullValue();
            }
        }

    }

}