using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace ZenDemo.DotNetFramework.Filters
{
    public sealed class UnhandledExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            var exception = actionExecutedContext.Exception;
            var request = actionExecutedContext.Request;

            Trace.TraceError("Unhandled exception for {0} {1}: {2}", request.Method, request.RequestUri, exception);

            actionExecutedContext.Response = request.CreateResponse(HttpStatusCode.InternalServerError, new
            {
                success = false,
                output = exception.Message
            });
        }
    }
}
