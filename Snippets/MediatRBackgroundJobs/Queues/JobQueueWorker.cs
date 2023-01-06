using MediatR;

namespace MediatRBackgroundJobs.Queues;

public class JobQueueWorker : BackgroundService
{
    private readonly ILogger<JobQueueWorker> _logger;
    private readonly IJobQueue _jobQueue;
    private readonly ISender _sender;

    public JobQueueWorker(ILogger<JobQueueWorker> logger, IJobQueue jobQueue, ISender sender)
    {
        _logger = logger;
        _jobQueue = jobQueue;
        _sender = sender;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Type} is starting.", nameof(JobQueueWorker));

        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Type} is stopping.", nameof(JobQueueWorker));

        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Type} is now running in the background.", nameof(JobQueueWorker));

        try
        {
            await ConsumeJobsAsync(stoppingToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogCritical(e, "An error occured while consuming jobs from the queue.");
        }
    }

    private async Task ConsumeJobsAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _jobQueue.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Received job {JobType}", job.GetType().Name);

                await _sender.Send(job, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "An error occurred while dispatching job {JobType}.", job.GetType().Name);
            }
        }
    }
}
