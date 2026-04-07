using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Aikido.Zen.Core;
using Aikido.Zen.DotNetFramework;
using Newtonsoft.Json;
using ZenDemo.DotNetFramework.Helpers;
using ZenDemo.DotNetFramework.Models;
using ZenDemo.DotNetFramework.Services;

namespace ZenDemo.DotNetFramework.Controllers
{
    public class DemoApiController : ApiController
    {
        [HttpGet]
        [Route("api/pets")]
        public async Task<IHttpActionResult> GetPets()
        {
            return Ok(await DatabaseHelper.Instance.GetAllPetsAsync().ConfigureAwait(false));
        }

        [HttpGet]
        [Route("api/pets/{id}")]
        public async Task<IHttpActionResult> GetPetById(string id)
        {
            var pet = await DatabaseHelper.Instance.GetPetByIdAsync(id).ConfigureAwait(false);
            if (pet == null)
            {
                return Content(HttpStatusCode.NotFound, new { error = "Pet not found" });
            }

            return Ok(pet);
        }

        [HttpPost]
        [Route("api/create")]
        public async Task<IHttpActionResult> CreatePet([FromBody] CreateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return HttpResponseHelper.PlainText(this, "Name is required", HttpStatusCode.BadRequest);
            }

            var rowsCreated = await DatabaseHelper.Instance.CreatePetByNameAsync(request.Name).ConfigureAwait(false);
            if (rowsCreated == -1)
            {
                return HttpResponseHelper.PlainText(this, "Database error occurred", HttpStatusCode.InternalServerError);
            }

            return HttpResponseHelper.PlainText(this, "Success!");
        }

        [HttpPost]
        [Route("api/execute")]
        public IHttpActionResult ExecuteCommandPost([FromBody] CommandRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserCommand))
            {
                return HttpResponseHelper.PlainText(this, "Command is required", HttpStatusCode.BadRequest);
            }

            return HttpResponseHelper.PlainText(this, AppHelpers.Instance.ExecuteShellCommand(request.UserCommand));
        }

        [HttpGet]
        [Route("api/execute/{command}")]
        public IHttpActionResult ExecuteCommandGet(string command)
        {
            return HttpResponseHelper.PlainText(this, AppHelpers.Instance.ExecuteShellCommand(command));
        }

        [HttpPost]
        [Route("api/request")]
        public async Task<IHttpActionResult> MakeRequest([FromBody] RequestRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                return HttpResponseHelper.PlainText(this, "URL is required", HttpStatusCode.BadRequest);
            }

            return HttpResponseHelper.PlainText(this, await AppHelpers.Instance.MakeHttpRequestAsync(request.Url).ConfigureAwait(false));
        }

        [HttpPost]
        [Route("api/request2")]
        public async Task<IHttpActionResult> MakeRequest2([FromBody] RequestRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                return HttpResponseHelper.PlainText(this, "URL is required", HttpStatusCode.BadRequest);
            }

            return HttpResponseHelper.PlainText(this, await AppHelpers.Instance.MakeHttpRequestAsync(request.Url).ConfigureAwait(false));
        }

        [HttpPost]
        [Route("api/request_different_port")]
        public async Task<IHttpActionResult> MakeRequestDifferentPort([FromBody] RequestDifferentPortRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                return HttpResponseHelper.PlainText(this, "URL is required", HttpStatusCode.BadRequest);
            }

            if (request.Port == 0)
            {
                return HttpResponseHelper.PlainText(this, "Port is required", HttpStatusCode.BadRequest);
            }

            return HttpResponseHelper.PlainText(this, await AppHelpers.Instance.MakeHttpRequestDifferentPortAsync(request.Url, request.Port).ConfigureAwait(false));
        }

        [HttpPost]
        [Route("api/stored_ssrf")]
        public async Task<IHttpActionResult> StoredSsrf()
        {
            StoredSsrfRequest request = null;
            if (Request.Content != null)
            {
                var rawBody = await Request.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(rawBody))
                {
                    request = JsonConvert.DeserializeObject<StoredSsrfRequest>(rawBody);
                }
            }

            return HttpResponseHelper.PlainText(this, await AppHelpers.Instance.MakeStoredSsrfRequestAsync(request != null ? request.UrlIndex : (int?)null).ConfigureAwait(false));
        }

        [HttpPost]
        [Route("api/stored_ssrf_2")]
        public IHttpActionResult StoredSsrfWithoutContext()
        {
            AppHelpers.Instance.QueueStoredSsrfUrl("http://evil-stored-ssrf-hostname/latest/api/token");
            return HttpResponseHelper.PlainText(this, "Request successful (Stored SSRF 2)");
        }

        [HttpGet]
        [Route("api/read")]
        public IHttpActionResult ReadFile([FromUri] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return HttpResponseHelper.PlainText(this, "Path is required", HttpStatusCode.BadRequest);
            }

            return HttpResponseHelper.PlainText(this, AppHelpers.Instance.ReadFile(path));
        }

        [HttpGet]
        [Route("api/read2")]
        public IHttpActionResult ReadFile2([FromUri] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return HttpResponseHelper.PlainText(this, "Path is required", HttpStatusCode.BadRequest);
            }

            return HttpResponseHelper.PlainText(this, AppHelpers.Instance.ReadFile2(path));
        }

        [HttpGet]
        [Route("api/client-ip")]
        public IHttpActionResult GetClientIp()
        {
            var ip = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            var aikidoIp = Zen.GetContext() != null ? Zen.GetContext().RemoteAddress : null;
            IEnumerable<string> forwardedFor;
            Request.Headers.TryGetValues("X-Forwarded-For", out forwardedFor);

            return HttpResponseHelper.PlainText(this, string.Join("\r\n", new[]
            {
                ip ?? string.Empty,
                aikidoIp ?? string.Empty,
                forwardedFor != null ? string.Join(",", forwardedFor.ToArray()) : string.Empty
            }));
        }

        [HttpGet]
        [Route("clear")]
        public async Task<IHttpActionResult> Clear()
        {
            await DatabaseHelper.Instance.ClearAllAsync().ConfigureAwait(false);
            return HttpResponseHelper.PlainText(this, "Cleared successfully.");
        }

        [HttpGet]
        [Route("healthz")]
        public IHttpActionResult Health()
        {
            var status = Zen.Status();
            var heartbeat = status != null ? status.Heartbeat : ReportingStatusResult.NotReported;

            string entryStatus;
            string description;
            HttpStatusCode httpStatus;

            switch (heartbeat)
            {
                case ReportingStatusResult.Ok:
                    entryStatus = "Healthy";
                    description = "Zen firewall is reporting heartbeats successfully.";
                    httpStatus = HttpStatusCode.OK;
                    break;
                case ReportingStatusResult.NotReported:
                    entryStatus = "Degraded";
                    description = "Zen firewall has not reported heartbeats yet.";
                    httpStatus = HttpStatusCode.OK;
                    break;
                case ReportingStatusResult.Expired:
                    entryStatus = "Degraded";
                    description = "Zen firewall heartbeat reports have expired.";
                    httpStatus = HttpStatusCode.OK;
                    break;
                default:
                    entryStatus = "Unhealthy";
                    description = "Zen firewall is not reporting heartbeats successfully.";
                    httpStatus = HttpStatusCode.ServiceUnavailable;
                    break;
            }

            return Content(httpStatus, new
            {
                status = entryStatus,
                entries = new
                {
                    zenFirewallReportingStatus = new
                    {
                        status = entryStatus,
                        description = description
                    }
                }
            });
        }
    }
}
