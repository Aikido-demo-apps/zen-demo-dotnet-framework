using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using Aikido.Zen.DotNetFramework;
using ZenDemo.DotNetFramework.Helpers;

namespace ZenDemo.DotNetFramework.Controllers
{
    public class TestController : ApiController
    {
        [HttpGet]
        [Route("api/custom-event")]
        public IHttpActionResult TrackCustomEvent(int count = 1)
        {
            if (!ModelState.IsValid || count < 1 || count > 100)
            {
                return HttpResponseHelper.PlainText(this, "count must be an integer between 1 and 100", HttpStatusCode.BadRequest);
            }

            for (var index = 0; index < count; index++)
            {
                Zen.Track("user.login_failed");
            }
            return HttpResponseHelper.PlainText(this, "Events tracked");
        }

        [HttpGet]
        [Route("test_ratelimiting_1")]
        public IHttpActionResult TestRateLimiting1()
        {
            return HttpResponseHelper.PlainText(this, "Request successful (Ratelimiting 1)");
        }

        [HttpGet]
        [Route("test_ratelimiting_2")]
        public IHttpActionResult TestRateLimiting2()
        {
            return HttpResponseHelper.PlainText(this, "Request successful (Ratelimiting 2)");
        }

        [HttpGet]
        [Route("test_bot_blocking")]
        public IHttpActionResult TestBotBlocking()
        {
            return HttpResponseHelper.PlainText(this, "Hello World! Bot blocking enabled on this route.");
        }

        [HttpGet]
        [Route("test_user_blocking")]
        public IHttpActionResult TestUserBlocking()
        {
            IEnumerable<string> users;
            Request.Headers.TryGetValues("user", out users);
            return HttpResponseHelper.PlainText(this, "Hello User with id: " + users.FirstOrDefault());
        }

        [HttpGet]
        [Route("test_endpoint_ip_blocking/{id:int}")]
        public IHttpActionResult TestEndpointIpBlocking(int id)
        {
            return HttpResponseHelper.PlainText(this, "Hello endpoint with route parameter: " + id);
        }
    }
}
