using bifeldy_lib_90.Abstractions;
using bifeldy_lib_90.Exceptions;
using bifeldy_lib_90.Models;
using bifeldy_lib_90.Repositories;
using bifeldy_lib_90.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace bifeldy_lib_90.Handlers {

    public interface IServiceProsesDataHandler : IServiceBaseHandler {
        Task<(int, ResponseJson, JsonTypeInfo)> Run<TFormDataStream>(HttpContext http, IDatabase db, object formDataStream, JsonTypeInfo<TFormDataStream> jsonTypeInfo, [CallerMemberName] string callerMemberName = null);
        Task<(int, ResponseJson, JsonTypeInfo)> ProsesData(HttpContext http, IDatabase db, InputJson fd, [CallerMemberName] string callerMemberName = null);
        Task<(int, ResponseJson, JsonTypeInfo)> StreamData(HttpContext http, IDatabase db, Stream stream, [CallerMemberName] string callerMemberName = null);
    }

    public abstract class CServiceProsesDataHandler : CServiceBaseHandler, IServiceProsesDataHandler {

        public CServiceProsesDataHandler(
            ILogger<CServiceProsesDataHandler> logger,
            IServiceProvider sp,
            IConverterService cs,
            IGeneralRepository generalRepo
        ) : base(logger, sp, cs, generalRepo) {
            //
        }

        public virtual async Task<(int, ResponseJson, JsonTypeInfo)> Run<TFormDataStream>(HttpContext http, IDatabase db, object formDataStream, JsonTypeInfo<TFormDataStream> jsonTypeInfo = null, [CallerMemberName] string callerMemberName = null) {
            string statusErrMsg = null;

            try {
                int statusCode = 0;
                ResponseJson response = null;
                JsonTypeInfo jti = null;

                if (formDataStream != null && typeof(Stream).IsAssignableFrom(formDataStream.GetType())) {
                    (statusCode, response, jti) = await this.StreamData(http, db, (Stream)formDataStream);
                }
                else {
                    (statusCode, response, jti) = await this.ProsesData(http, db, (InputJson)formDataStream);
                }

                if (typeof(ResponseRedirect).IsAssignableFrom(response.GetType()) && statusCode >= 300 && statusCode < 400) {
                    var redir = (ResponseRedirect)response;

                    switch (statusCode) {
                        case StatusCodes.Status306SwitchProxy:
                            statusErrMsg = $"FORWARD => {redir.url} ({redir.info})";
                            break;
                    }
                }

                return (statusCode, response, jti);
            }
            catch (TidakMemenuhiException e) {
                statusErrMsg = e.Message;
                throw;
            }
            catch (Exception e) {
                statusErrMsg = e.Message + Environment.NewLine + e.StackTrace;
                throw;
            }
            finally {
                if (!string.IsNullOrEmpty(statusErrMsg)) {
                    this._logger.LogError("[{name}] (LogTfApi) {e}", this.LogTfApiName, statusErrMsg);

                    if (statusErrMsg.Length > 1000) {
                        statusErrMsg = statusErrMsg[..1000];
                    }
                }

                await this.CatatLogTfApi(http, db, formDataStream, jsonTypeInfo, statusErrMsg);
            }
        }

        public virtual Task<(int, ResponseJson, JsonTypeInfo)> ProsesData(HttpContext http, IDatabase db, InputJson fd, [CallerMemberName] string callerMemberName = null) {
            throw new Exception($"Fitur {callerMemberName ?? this.LogTfApiName} Belum Tersedia!");
        }

        // The input JSON must be a root-level array (e.g., [{"Id":1,...}, {"Id":2,...}])
        // The input JSON must be an each line (e.g., {"Id":1,...})
        public virtual Task<(int, ResponseJson, JsonTypeInfo)> StreamData(HttpContext http, IDatabase db, Stream stream, [CallerMemberName] string callerMemberName = null) {
            throw new Exception($"Fitur {callerMemberName ?? this.LogTfApiName} Belum Tersedia!");
        }

    }

}
