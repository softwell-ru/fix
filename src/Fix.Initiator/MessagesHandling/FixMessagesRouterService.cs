using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SoftWell.Fix.Initiator.MessagesHandling;

public sealed class FixMessagesRouterService<TFixMessagesReader> : BackgroundService
    where TFixMessagesReader : IFixMessagesReader
{
    private readonly TFixMessagesReader _fixMessagesReader;

    private readonly IReadOnlyList<IFixMessagesHandlerInternal<TFixMessagesReader>> _handlers;

    private readonly ILogger<FixMessagesRouterService<TFixMessagesReader>> _logger;

    public FixMessagesRouterService(
        TFixMessagesReader fixMessagesReader,
        IEnumerable<IFixMessagesHandlerInternal<TFixMessagesReader>> handlers,
        ILogger<FixMessagesRouterService<TFixMessagesReader>> logger)
    {
        _fixMessagesReader = fixMessagesReader ?? throw new ArgumentNullException(nameof(fixMessagesReader));
        _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fix messages router service for {FixMessagesReader} starting...", typeof(TFixMessagesReader).Name);
        return base.StartAsync(cancellationToken);
    }

    public async override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fix messages router service for {FixMessagesReader} stopping...", typeof(TFixMessagesReader).Name);
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Fix messages router service for {FixMessagesReader} stopped", typeof(TFixMessagesReader).Name);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fix messages router service for {FixMessagesReader} started", typeof(TFixMessagesReader).Name);

        // TODO: тут можно всякими опциями настроить параллельность выполнения, раскидывание по разным каналам и тд
        // пока что так: все вместе хэндлим одно сообщение, только потом переходим к следующему
        await foreach (var m in _fixMessagesReader.WithCancellation(stoppingToken))
        {
            try
            {
                await Task.WhenAll(_handlers.Select(x => x.HandleMessageAsync(m, stoppingToken)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling fix message: {FixMessage}", m.ToString());
            }
        }
    }
}