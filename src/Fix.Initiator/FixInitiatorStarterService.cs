using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickFix;

namespace SoftWell.Fix.Initiator;

public sealed class FixInitiatorStarterService : IHostedService, IDisposable
{
    private readonly IInitiator _fixInitiator;

    private readonly ILogger<FixInitiatorStarterService> _logger;

    private readonly string _name;

    public FixInitiatorStarterService(
        IInitiator fixInitiator,
        ILogger<FixInitiatorStarterService> logger,
        string? name = null)
    {
        _fixInitiator = fixInitiator ?? throw new ArgumentNullException(nameof(fixInitiator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _name = name ?? _fixInitiator.GetType().FullName!;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fix initiator {FixInitiatorName} is starting..", _name);
        _fixInitiator.Start();
        _logger.LogInformation("Fix initiator {FixInitiatorName} started..", _name);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fix initiator {FixInitiatorName} is stopping..", _name);
        _fixInitiator.Stop();
        _logger.LogInformation("Fix initiator {FixInitiatorName} stopped..", _name);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _fixInitiator.Dispose();
    }
}
