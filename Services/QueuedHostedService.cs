using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NVSSMessaging.Models;
using System.Linq;

namespace NVSSMessaging.Services
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger<QueuedHostedService> _logger;
        private readonly IServiceProvider _services;

        public QueuedHostedService(
          IBackgroundTaskQueue taskQueue,
          ILogger<QueuedHostedService> logger,
          IServiceProvider services
        )
        {
            TaskQueue = taskQueue;
            _logger = logger;
            _services = services;
        }

        public IBackgroundTaskQueue TaskQueue { get; }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                $"Background job processing has started {Environment.NewLine}"
            );

            await BackgroundProcessing(cancellationToken);
        }

        private async Task BackgroundProcessing(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
              var workItem = await TaskQueue.DequeueAsync(cancellationToken);

            try
            {
                using (var scope = this._services.CreateScope())
                {
                    var workerType = workItem
                        .GetType()
                        .GetInterfaces()
                        .First(t => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(IBackgroundWorkOrder<,>))
                        .GetGenericArguments()
                        .Last();

                    var worker = scope.ServiceProvider
                        .GetRequiredService(workerType);

                    var task = (Task)workerType
                        .GetMethod("DoWork")
                        .Invoke(worker, new object[] { workItem, cancellationToken });
                    await task;
                }
            }
              catch (Exception ex)
              {
                  _logger.LogError(ex,
                      "Error occurred executing {WorkItem}.", nameof(workItem));
              }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background job processing is stopping.");

            await base.StopAsync(cancellationToken);
        }
    }
}
