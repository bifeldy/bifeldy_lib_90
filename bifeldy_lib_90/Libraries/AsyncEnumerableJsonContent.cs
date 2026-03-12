using bifeldy_lib_90.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Libraries {

    public sealed class AsyncEnumerableJsonContent<T> : HttpContent where T : JsonSerDe, new() {

        private readonly IAsyncEnumerable<T> _source;
        private readonly JsonTypeInfo<T> _typeInfo;
        private readonly bool _ndjson;
        private readonly CancellationToken _token;

        public AsyncEnumerableJsonContent(
            IAsyncEnumerable<T> source,
            string mediaType,
            JsonTypeInfo<T> typeInfo,
            bool ndjson,
            CancellationToken _token
        ) {
            this._source = source;
            this._typeInfo = typeInfo;
            this._ndjson = ndjson;
            this._token = _token;
            //
            this.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        public AsyncEnumerableJsonContent(
            IAsyncEnumerable<T> source,
            string mediaType,
            bool ndjson
        ) {
            this._source = source;
            this._ndjson = ndjson;
            //
            this.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context) {
            var jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.Converters.Add(new DecimalConverter());
            jsonSerializerOptions.Converters.Add(new NullableDecimalConverter());

            if (this._ndjson) {
                await foreach (T item in this._source.WithCancellation(this._token)) {
                    if (this._typeInfo != null) {
                        await JsonSerializer.SerializeAsync(stream, item, this._typeInfo, this._token);
                    }
                    else {
                        if (!RuntimeFeature.IsDynamicCodeSupported) {
                            throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
                        }

                        await JsonSerializer.SerializeAsync(stream, item, jsonSerializerOptions);
                    }

                    await stream.WriteAsync(new ReadOnlyMemory<byte>([(byte)'\n']));
                    await stream.FlushAsync();
                }
            }
            else {
                await using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() {
                    SkipValidation = true
                })) {
                    writer.WriteStartArray();

                    await foreach (T item in this._source.WithCancellation(this._token)) {
                        if (this._typeInfo != null) {
                            JsonSerializer.Serialize(writer, item, this._typeInfo);
                        }
                        else {
                            if (!RuntimeFeature.IsDynamicCodeSupported) {
                                throw new Exception("Hanya Bisa Dijalankan Menggunakan JIT, Bukan AOT");
                            }

                            JsonSerializer.Serialize(writer, item, jsonSerializerOptions);
                        }

                        await writer.FlushAsync();
                    }

                    writer.WriteEndArray();
                    await writer.FlushAsync();
                }
            }
        }

        protected override bool TryComputeLength(out long length) {
            length = 0;
            return false;
        }

    }

}