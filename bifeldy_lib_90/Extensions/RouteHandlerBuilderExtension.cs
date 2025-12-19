using bifeldy_lib_90.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace bifeldy_lib_90.Extensions {

    public static class RouteHandlerBuilderExtension {

        public static RouteHandlerBuilder WithDefaultBadRequest(this RouteHandlerBuilder builder) {
            return builder .Produces<ResponseJsonSingle<ResponseJsonMessage>>(StatusCodes.Status400BadRequest);
        }

    }

}
