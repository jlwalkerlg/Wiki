using MediatR;
using MediatRBackgroundJobs.Jobs;
using MediatRBackgroundJobs.Queues;
using Microsoft.AspNetCore.Mvc;

namespace MediatRBackgroundJobs.Controllers;

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
