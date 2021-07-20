using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NVSSMessaging.Models;

namespace NVSSMessaging.Services
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger<QueuedHostedService> _logger;
        private readonly IServiceProvider Services;

        public QueuedHostedService(
          IBackgroundTaskQueue taskQueue,
          ILogger<QueuedHostedService> logger,
          IServiceProvider services
        )
        {
            TaskQueue = taskQueue;
            _logger = logger;
            Services = services;
        }

        public IBackgroundTaskQueue TaskQueue { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"Background job processing has started {Environment.NewLine}"
            );

            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
              var workItem = await TaskQueue.DequeueAsync(stoppingToken);

              try
              {
                  await workItem(stoppingToken);
              }
              catch (Exception ex)
              {
                  _logger.LogError(ex,
                      "Error occurred executing {WorkItem}.", nameof(workItem));
              }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background job processing is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}
