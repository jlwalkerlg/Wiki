using MediatR;
using System.Threading.Channels;

namespace MediatRBackgroundJobs.Queues;

public class InMemoryJobQueue : IJobQueue
{
    private readonly Channel<IRequest> _channel = Channel.CreateUnbounded<IRequest>();

    public async Task PushAsync(IRequest job, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public IAsyncEnumerable<IRequest> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
