namespace ZenDemo.DotNetFramework.Models
{
    public class CreateRequest
    {
        public string Name { get; set; }
    }

    public class CommandRequest
    {
        public string UserCommand { get; set; }
    }

    public class RequestRequest
    {
        public string Url { get; set; }
    }

    public class RequestDifferentPortRequest
    {
        public string Url { get; set; }

        public int Port { get; set; }
    }

    public class StoredSsrfRequest
    {
        public int? UrlIndex { get; set; }
    }

    public class LlmRequest
    {
        public string Message { get; set; }

        public string Provider { get; set; }
    }
}
