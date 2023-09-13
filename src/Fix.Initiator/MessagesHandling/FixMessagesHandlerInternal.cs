using QuickFix;

namespace SoftWell.Fix.Initiator.MessagesHandling;

public sealed class FixMessagesHandlerInternal<TFixMessagesReader, TFixMessagesHandler> : IFixMessagesHandlerInternal<TFixMessagesReader>
    where TFixMessagesReader : IFixMessagesReader
    where TFixMessagesHandler : IFixMessagesHandler
{
    private readonly TFixMessagesHandler _handler;

    public FixMessagesHandlerInternal(TFixMessagesHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public Task HandleMessageAsync(Message message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return _handler.HandleMessageAsync(message, ct);
    }
}

public class FixMessagesHandlerInternal<TFixMessagesReader, TMessage, TFixMessagesHandler> : IFixMessagesHandlerInternal<TFixMessagesReader>
    where TFixMessagesReader : IFixMessagesReader
    where TMessage : Message
    where TFixMessagesHandler : IFixMessagesHandler<TMessage>
{
    private readonly TFixMessagesHandler _handler;

    public FixMessagesHandlerInternal(TFixMessagesHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public Task HandleMessageAsync(Message message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message is not TMessage typedMessage) return Task.CompletedTask;

        return _handler.HandleMessageAsync(typedMessage, ct);
    }
}