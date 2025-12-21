using bifeldy_lib_90.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using System.Net.Mime;

namespace bifeldy_lib_90.Extensions {

    public sealed record ApiTagDescription(string Tag, string Description);

    public sealed class DefaultBadRequestProducesMetadata : IProducesResponseTypeMetadata {
        public Type Type => typeof(ResponseJsonSingle<ResponseJsonMessage>);
        public int StatusCode => StatusCodes.Status400BadRequest;
        public IEnumerable<string> ContentTypes => [MediaTypeNames.Application.Json];
    }

    public static class RouteGroupBuilderExtension {

        public static RouteGroupBuilder MapGroupTagDescription(this RouteGroupBuilder group, string path, string tag, string description = null) {
            RouteGroupBuilder gp = group.MapGroup(path)
                .WithTags(tag);

            if (!string.IsNullOrEmpty(description)) {
                _ = gp.WithMetadata(new ApiTagDescription(tag, description));
            }

            return gp.WithMetadata(new DefaultBadRequestProducesMetadata());
        }

    }

}
