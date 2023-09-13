using QuickFix;

namespace SoftWell.Fix.Initiator.MessagesHandling;

public interface IFixMessagesHandlerInternal<TFixMessagesReader>
    where TFixMessagesReader : IFixMessagesReader
{
    Task HandleMessageAsync(Message message, CancellationToken ct = default);
}
