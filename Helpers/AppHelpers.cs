using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace ZenDemo.DotNetFramework.Helpers
{
    public sealed class CommandExecutionResult
    {
        public CommandExecutionResult(string output, int exitCode)
        {
            Output = output ?? string.Empty;
            ExitCode = exitCode;
        }

        public string Output { get; private set; }

        public int ExitCode { get; private set; }

        public bool Succeeded
        {
            get { return ExitCode == 0; }
        }
    }

    public sealed class AppHelpers
    {
        private const int MaxDecodeUriPasses = 2;
        private static readonly string[] StoredSsrfUrls =
        {
            "http://evil-stored-ssrf-hostname/latest/api/token",
            "http://metadata.google.internal/latest/api/token",
            "http://metadata.goog/latest/api/token",
            "http://169.254.169.254/latest/api/token",
        };

        private static readonly Lazy<AppHelpers> LazyInstance = new Lazy<AppHelpers>(() => new AppHelpers());

        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, byte> _storedSsrfQueue;

        private AppHelpers()
        {
            _httpClient = new HttpClient();
            _storedSsrfQueue = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        }

        public static AppHelpers Instance
        {
            get { return LazyInstance.Value; }
        }

        public CommandExecutionResult ExecuteShellCommand(string command)
        {
            command = DecodeUriComponent(command);

            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var processInfo = new ProcessStartInfo
            {
                FileName = isWindows ? "cmd.exe" : "/bin/bash",
                Arguments = isWindows ? "/c " + command : "-c \"" + command.Replace("\"", "\\\"") + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();
                return new CommandExecutionResult(string.IsNullOrEmpty(error) ? output : error, process.ExitCode);
            }
        }

        public async Task<string> MakeHttpRequestAsync(string url)
        {
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public async Task<string> MakeHttpRequestDifferentPortAsync(string url, int port)
        {
            var uri = new Uri(url);
            var uriBuilder = new UriBuilder(uri)
            {
                Port = port
            };

            return await MakeHttpRequestAsync(uriBuilder.Uri.ToString()).ConfigureAwait(false);
        }

        public string ReadFile(string filePath)
        {
            var blogsDirectory = HostingEnvironment.MapPath("~/wwwroot/blogs") ?? string.Empty;
            var fullPath = Path.Combine(blogsDirectory, filePath);

            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : "File not found";
        }

        public string ReadFile2(string filePath)
        {
            var blogsDirectory = HostingEnvironment.MapPath("~/wwwroot/blogs") ?? string.Empty;
            var fullPath = Path.GetFullPath(Path.Combine(blogsDirectory, filePath));

            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : "File not found";
        }

        public async Task<string> MakeStoredSsrfRequestAsync(int? urlIndex)
        {
            var index = urlIndex.GetValueOrDefault();
            if (index < 0)
            {
                index = 0;
            }

            return await MakeHttpRequestAsync(StoredSsrfUrls[index % StoredSsrfUrls.Length]).ConfigureAwait(false);
        }

        public void QueueStoredSsrfUrl(string url)
        {
            _storedSsrfQueue.TryAdd(url, 0);
        }

        public async Task ProcessStoredSsrfUrlsAsync()
        {
            foreach (var url in _storedSsrfQueue.Keys.ToArray())
            {
                try
                {
                    await MakeHttpRequestAsync(url).ConfigureAwait(false);
                }
                catch
                {
                    // The demo intentionally ignores background SSRF failures.
                }
                finally
                {
                    byte removed;
                    _storedSsrfQueue.TryRemove(url, out removed);
                }
            }
        }

        private static string DecodeUriComponent(string input)
        {
            var decoded = input ?? string.Empty;

            for (var i = 0; i < MaxDecodeUriPasses; i++)
            {
                var next = Uri.UnescapeDataString(decoded);
                if (next == decoded)
                {
                    break;
                }

                decoded = next;
            }

            return decoded;
        }
    }
}
