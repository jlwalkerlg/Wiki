# MediatRBackgroundJobs

## Getting Started

Install MediatR and register the required services in your dependency injection container.

```csharp
builder.Services.AddMediatR(typeof(Program));
builder.Services.AddSingleton<IJobQueue, InMemoryJobQueue>();
builder.Services.AddHostedService<JobQueueWorker>();
```

Then, simply push jobs onto the queue to have them dispatched to MediatR handlers in the background. Alternatively, you can run them synchronously with MediatR as per usual, as the following snippet demonstrates.

```csharp
[ApiController]
public class ReportGeneratorController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IJobQueue _jobQueue;

    public ReportGeneratorController(ISender sender, IJobQueue jobQueue)
    {
        _sender = sender;
        _jobQueue = jobQueue;
    }

    [HttpPost("/reports/generate")]
    public async Task<OkResult> GenerateAsync()
    {
        var job = new GenerateReportsJob();
        await _sender.Send(job);
        return Ok();
    }

    [HttpPost("/reports/schedule")]
    public async Task<AcceptedResult> ScheduleAsync()
    {
        var job = new GenerateReportsJob();
        await _jobQueue.PushAsync(job);
        return Accepted();
    }
}
```
