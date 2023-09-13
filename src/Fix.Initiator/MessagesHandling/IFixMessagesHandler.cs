using QuickFix;

namespace SoftWell.Fix.Initiator.MessagesHandling;

public interface IFixMessagesHandler
{
    Task HandleMessageAsync(Message message, CancellationToken ct = default);
}

public interface IFixMessagesHandler<TMessage> where TMessage : Message
{
    Task HandleMessageAsync(TMessage message, CancellationToken ct = default);
}