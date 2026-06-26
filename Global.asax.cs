using System;
using System.Diagnostics;
using System.Net;
using System.Web;
using System.Web.Http;
using Aikido.Zen.Core.Models;
using Aikido.Zen.DotNetFramework;
using Newtonsoft.Json;
using ZenDemo.DotNetFramework.Helpers;
using ZenDemo.DotNetFramework.Services;

namespace ZenDemo.DotNetFramework
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(App_Start.WebApiConfig.Register);
            Environment.SetEnvironmentVariable("AIKIDO_DEBUG", "true");

            Zen.SetUser(context =>
            {
                var primaryUser = context.Request.Headers["user"];
                int primaryUserId;
                if (!string.IsNullOrWhiteSpace(primaryUser) && int.TryParse(primaryUser, out primaryUserId))
                {
                    return new User(primaryUserId.ToString(), UserHelper.GetName(primaryUserId));
                }

                var fallbackUserId = context.Request.Headers["X-User-ID"];
                var fallbackUserName = context.Request.Headers["X-User-Name"];
                int secondaryUserId;
                if (!string.IsNullOrWhiteSpace(fallbackUserId) && int.TryParse(fallbackUserId, out secondaryUserId))
                {
                    return new User(secondaryUserId.ToString(), string.IsNullOrWhiteSpace(fallbackUserName) ? UserHelper.GetName(secondaryUserId) : fallbackUserName);
                }

                return null;
            });

            Zen.SetRateLimitGroup(context =>
            {
                var rateLimitGroupCookie = context.Request.Cookies["RateLimitingGroupID"];
                return rateLimitGroupCookie?.Value;
            });

            try
            {
                Zen.Start();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Aikido Zen failed to start: " + ex);
            }

            try
            {
                DatabaseHelper.Instance.Initialize();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Database initialization failed: " + ex);
            }

            StoredSsrfBackgroundWorker.Start();
        }

        protected void Application_End()
        {
            StoredSsrfBackgroundWorker.Stop();
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            if (exception == null)
            {
                return;
            }

            Trace.TraceError("Unhandled exception for {0} {1}: {2}", Request.HttpMethod, Request.RawUrl, exception);

            Server.ClearError();
            Response.Clear();
            Response.TrySkipIisCustomErrors = true;
            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Response.ContentType = "application/json";
            Response.Write(JsonConvert.SerializeObject(new
            {
                success = false,
                output = exception.Message
            }));
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
