using System;
using System.IO;
using System.Web;

namespace ZenDemo.DotNetFramework.HttpModules
{
    public sealed class PublicFallbackModule : IHttpModule
    {
        public void Init(HttpApplication application)
        {
            application.EndRequest += OnEndRequest;
        }

        public void Dispose()
        {
        }

        private static void OnEndRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            if (context == null || context.Response.StatusCode != 404)
            {
                return;
            }

            var relativePath = context.Request.AppRelativeCurrentExecutionFilePath;
            if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                return;
            }

            relativePath = relativePath.Substring(2);
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath.StartsWith("public/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var publicRoot = Path.GetFullPath(context.Server.MapPath("~/wwwroot/public"));
            var publicFilePath = Path.GetFullPath(Path.Combine(publicRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!publicFilePath.StartsWith(publicRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!File.Exists(publicFilePath))
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = 200;
            context.Response.TrySkipIisCustomErrors = true;
            context.Response.ContentType = MimeMapping.GetMimeMapping(publicFilePath);
            context.Response.TransmitFile(publicFilePath);
        }
    }
}
