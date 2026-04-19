using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AthkarApp.Services
{
    // Run heavy, non-UI startup work here so MAUI main thread stays responsive.
    public class StartupBackgroundService : IHostedService
    {
        private readonly CancellationTokenSource _cts = new();

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                try
                {
                    Debug.WriteLine("StartupBackgroundService: background initialization starting");

                    // Move heavy work here:
                    // - Database migrations
                    // - Large image/asset decoding
                    // - Remote network calls needed for non-UI startup
                    // Example: await MyDatabase.MigrateAsync();

                    await Task.Delay(1); // no-op placeholder; replace with real work
                    Debug.WriteLine("StartupBackgroundService: background initialization complete");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"StartupBackgroundService: error: {ex}");
                }
            }, _cts.Token);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }
    }
}