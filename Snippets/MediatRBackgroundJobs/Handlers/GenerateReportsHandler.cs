using MediatR;
using MediatRBackgroundJobs.Jobs;

namespace MediatRBackgroundJobs.Handlers;

public class GenerateReportsHandler : AsyncRequestHandler<GenerateReportsJob>
{
    private readonly ILogger<GenerateReportsHandler> _logger;

    public GenerateReportsHandler(ILogger<GenerateReportsHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task Handle(GenerateReportsJob request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating reports...");
        await Task.Delay(1000, cancellationToken);
        _logger.LogInformation("Reports generated.");
    }
}
