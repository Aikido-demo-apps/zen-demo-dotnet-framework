using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Results;

namespace ZenDemo.DotNetFramework.Helpers
{
    public static class HttpResponseHelper
    {
        public static IHttpActionResult PlainText(ApiController controller, string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = controller.Request.CreateResponse(statusCode);
            response.Content = new StringContent(content ?? string.Empty, Encoding.UTF8, "text/plain");
            return new ResponseMessageResult(response);
        }

        public static IHttpActionResult File(ApiController controller, string virtualPath, string contentType = null)
        {
            var absolutePath = HostingEnvironment.MapPath(virtualPath);
            if (string.IsNullOrWhiteSpace(absolutePath) || !System.IO.File.Exists(absolutePath))
            {
                return new ResponseMessageResult(controller.Request.CreateResponse(HttpStatusCode.NotFound));
            }

            var response = controller.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(System.IO.File.OpenRead(absolutePath));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? MimeMapping.GetMimeMapping(absolutePath));
            return new ResponseMessageResult(response);
        }
    }
}
