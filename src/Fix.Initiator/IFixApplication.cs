using QuickFix;

namespace SoftWell.Fix.Initiator;

public interface IFixApplication : IApplication
{
    SessionSettings SessionSettings { get; }
}
