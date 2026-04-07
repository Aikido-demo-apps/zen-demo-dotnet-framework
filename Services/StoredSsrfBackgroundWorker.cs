using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using ZenDemo.DotNetFramework.Helpers;

namespace ZenDemo.DotNetFramework.Services
{
    public sealed class StoredSsrfBackgroundWorker : IRegisteredObject
    {
        private static readonly object SyncRoot = new object();
        private static StoredSsrfBackgroundWorker _instance;

        private readonly Timer _timer;
        private int _running;

        private StoredSsrfBackgroundWorker()
        {
            HostingEnvironment.RegisterObject(this);
            _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        public static void Start()
        {
            lock (SyncRoot)
            {
                if (_instance == null)
                {
                    _instance = new StoredSsrfBackgroundWorker();
                }
            }
        }

        public static void Stop()
        {
            lock (SyncRoot)
            {
                if (_instance != null)
                {
                    _instance.Stop(true);
                    _instance = null;
                }
            }
        }

        public void Stop(bool immediate)
        {
            _timer.Dispose();
            HostingEnvironment.UnregisterObject(this);
        }

        private void OnTimerTick(object state)
        {
            if (Interlocked.Exchange(ref _running, 1) == 1)
            {
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    await AppHelpers.Instance.ProcessStoredSsrfUrlsAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Stored SSRF background worker failed: " + ex);
                }
                finally
                {
                    Interlocked.Exchange(ref _running, 0);
                }
            });
        }
    }
}
