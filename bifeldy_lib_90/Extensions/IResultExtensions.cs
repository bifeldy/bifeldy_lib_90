using bifeldy_lib_90.Libraries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace bifeldy_lib_90.Extensions {

    public static class IResultExtensions {

        public static IActionResult ToActionResult(this IResult result) {
            return new IActionResultWrapper(result);
        }

    }

}