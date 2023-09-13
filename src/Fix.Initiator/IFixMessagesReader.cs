using QuickFix;

namespace SoftWell.Fix.Initiator;

public interface IFixMessagesReader : IAsyncEnumerable<Message>
{
}
