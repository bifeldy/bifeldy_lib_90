using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace bifeldy_lib_90.Extensions {

    public sealed record ApiTagDescription(string Tag, string Description);

    public static class RouteGroupBuilderExtension {

        public static RouteGroupBuilder MapGroupTagDescription(this RouteGroupBuilder group, string path, string tag, string description = null) {
            RouteGroupBuilder gp = group.MapGroup(path)
                .WithTags(tag);

            if (!string.IsNullOrEmpty(description)) {
                _ = gp.WithMetadata(new ApiTagDescription(tag, description));
            }

            return gp;
        }

    }

}
