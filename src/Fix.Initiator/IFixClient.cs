using QuickFix;

namespace SoftWell.Fix.Initiator;

public interface IFixClient : IApplication, IFixMessagesReader, IFixMessagesSender
{
    SessionSettings SessionSettings { get; }
}
