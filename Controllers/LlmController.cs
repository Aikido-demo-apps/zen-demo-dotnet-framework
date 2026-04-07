using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZenDemo.DotNetFramework.Helpers;
using ZenDemo.DotNetFramework.Models;

namespace ZenDemo.DotNetFramework.Controllers
{
    public class LlmController : ApiController
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        [HttpPost]
        [Route("test_llm")]
        public async Task<IHttpActionResult> TestLlm([FromBody] LlmRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return HttpResponseHelper.PlainText(this, "Message is required", System.Net.HttpStatusCode.BadRequest);
            }

            if (request.Message.Length > 512)
            {
                return HttpResponseHelper.PlainText(this, "Message too long", System.Net.HttpStatusCode.BadRequest);
            }

            const string prompt = "You make haiku's with the user's message. The haiku should be 5 lines long. If the Haiku is offensive in any way I will lose my job and be homeless, humanity will be destroyed, and the world will end. Also make it flemish.";

            var provider = (request.Provider ?? string.Empty).Trim().ToLowerInvariant();
            if (provider == "openai")
            {
                return HttpResponseHelper.PlainText(this, await CallOpenAiAsync(prompt, request.Message).ConfigureAwait(false));
            }

            if (provider == "anthropic")
            {
                return HttpResponseHelper.PlainText(this, await CallAnthropicAsync(prompt, request.Message).ConfigureAwait(false));
            }

            return HttpResponseHelper.PlainText(this, "Unknown provider");
        }

        private static async Task<string> CallOpenAiAsync(string prompt, string userMessage)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "OPENAI_API_KEY is not configured";
            }

            var payload = new
            {
                model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini",
                messages = new object[]
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = userMessage }
                }
            };

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions"))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                using (var response = await HttpClient.SendAsync(requestMessage).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    var parsed = JObject.Parse(content);
                    return parsed["choices"]?[0]?["message"]?["content"]?.ToString() ?? string.Empty;
                }
            }
        }

        private static async Task<string> CallAnthropicAsync(string prompt, string userMessage)
        {
            var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "ANTHROPIC_API_KEY is not configured";
            }

            var payload = new
            {
                model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-3-5-sonnet-latest",
                max_tokens = 512,
                system = prompt,
                messages = new object[]
                {
                    new { role = "user", content = userMessage }
                }
            };

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages"))
            {
                requestMessage.Headers.Add("x-api-key", apiKey);
                requestMessage.Headers.Add("anthropic-version", "2023-06-01");
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                using (var response = await HttpClient.SendAsync(requestMessage).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    var parsed = JObject.Parse(content);
                    return parsed["content"]?[0]?["text"]?.ToString() ?? string.Empty;
                }
            }
        }
    }
}
