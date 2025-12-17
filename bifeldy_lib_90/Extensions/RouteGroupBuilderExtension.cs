using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace bifeldy_lib_90.Extensions {

    public static class RouteGroupBuilderExtension {

        // TODO: Add additional MapEndpoints created here
        public static void MapDefaultEndpoints(this RouteGroupBuilder routeGroupBuilder) {
            _ = routeGroupBuilder.MapGroup("/");
        }

    }

}
