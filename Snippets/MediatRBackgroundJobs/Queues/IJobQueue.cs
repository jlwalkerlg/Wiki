using MediatR;

namespace MediatRBackgroundJobs.Queues;

public interface IJobQueue
{
    Task PushAsync(IRequest job, CancellationToken cancellationToken = default);
    IAsyncEnumerable<IRequest> ReadAllAsync(CancellationToken cancellationToken);
}
