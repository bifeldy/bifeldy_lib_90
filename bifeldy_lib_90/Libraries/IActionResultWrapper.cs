using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace bifeldy_lib_90.Libraries {

    public sealed class IActionResultWrapper : IActionResult {

        private readonly IResult _result;

        public IActionResultWrapper(IResult result) {
            this._result = result;
        }

        public Task ExecuteResultAsync(ActionContext context) {
            return this._result.ExecuteAsync(context.HttpContext);
        }

    }

}