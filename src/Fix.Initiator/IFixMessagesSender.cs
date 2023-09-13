using QuickFix;

namespace SoftWell.Fix.Initiator;

public interface IFixMessagesSender
{
    Task SendMessageAsync(Message message, CancellationToken ct = default);
}
