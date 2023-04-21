using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

// Background tasks in dotnet core MVC are quite difficult to implement due to the amount of
// boilerplate required along with very poor examples. This implementation is derived
// from the following sources:
// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-3.0&tabs=visual-studio#queued-background-tasks
// https://github.com/dotnet/AspNetCore.Docs/issues/22702
// https://github.com/dotnet/extensions/issues/805#issuecomment-410539073
namespace messaging.Services
{
    public interface IBackgroundWorkOrder { }

    public interface IBackgroundWorkOrder<TWorkItem, TWorker> : IBackgroundWorkOrder
        where TWorker : IBackgroundWorker<TWorkItem, TWorker>
        where TWorkItem : IBackgroundWorkOrder<TWorkItem, TWorker>
    {
    }

    public interface IBackgroundWorker { }

    public interface IBackgroundWorker<TWorkItem, TWorker> : IBackgroundWorker
        where TWorker : IBackgroundWorker<TWorkItem, TWorker>
        where TWorkItem : IBackgroundWorkOrder<TWorkItem, TWorker>
    {
        Task DoWork(TWorkItem item, CancellationToken cancellationToken);
    }

    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItemAsync<TWorkItem, TWorker>(IBackgroundWorkOrder<TWorkItem, TWorker> message)
        where TWorker : IBackgroundWorker<TWorkItem, TWorker>
        where TWorkItem : IBackgroundWorkOrder<TWorkItem, TWorker>;

        Task<IBackgroundWorkOrder> DequeueAsync(CancellationToken cancellationToken);
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<IBackgroundWorkOrder> _queue;

        public BackgroundTaskQueue(int capacity)
        {
            // Capacity should be set based on the expected application load and
            // number of concurrent threads accessing the queue.
            // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
            // which completes only when space became available. This leads to backpressure,
            // in case too many publishers/calls start accumulating.
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<IBackgroundWorkOrder>(options);
        }

        public async void QueueBackgroundWorkItemAsync<TWorkItem, TWorker>(IBackgroundWorkOrder<TWorkItem, TWorker> workItem)
            where TWorkItem : IBackgroundWorkOrder<TWorkItem, TWorker>
            where TWorker : IBackgroundWorker<TWorkItem, TWorker>
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            await _queue.Writer.WriteAsync(workItem);
        }

        public async Task<IBackgroundWorkOrder> DequeueAsync(CancellationToken cancellationToken)
        {
            var workItem = await _queue.Reader.ReadAsync(cancellationToken);

            return workItem;
        }
    }
}
