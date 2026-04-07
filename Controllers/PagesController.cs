using System.Web.Http;
using ZenDemo.DotNetFramework.Helpers;

namespace ZenDemo.DotNetFramework.Controllers
{
    public class PagesController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Index()
        {
            return HttpResponseHelper.File(this, "~/wwwroot/index.html", "text/html");
        }

        [HttpGet]
        [Route("pages/execute")]
        public IHttpActionResult Execute()
        {
            return HttpResponseHelper.File(this, "~/wwwroot/execute_command.html", "text/html");
        }

        [HttpGet]
        [Route("pages/create")]
        public IHttpActionResult Create()
        {
            return HttpResponseHelper.File(this, "~/wwwroot/create.html", "text/html");
        }

        [HttpGet]
        [Route("pages/request")]
        public IHttpActionResult RequestPage()
        {
            return HttpResponseHelper.File(this, "~/wwwroot/request.html", "text/html");
        }

        [HttpGet]
        [Route("pages/read")]
        public IHttpActionResult Read()
        {
            return HttpResponseHelper.File(this, "~/wwwroot/read_file.html", "text/html");
        }
    }
}
