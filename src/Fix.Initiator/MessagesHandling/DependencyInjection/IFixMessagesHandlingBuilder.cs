using QuickFix;
using SoftWell.Fix.Initiator.MessagesHandling;

namespace Microsoft.Extensions.DependencyInjection;

public interface IFixMessagesHandlingBuilder
{
    IFixMessagesHandlingBuilder AddMessagesHandler<TMessagesHandler>()
        where TMessagesHandler : IFixMessagesHandler;

    IFixMessagesHandlingBuilder AddMessagesHandler<TMessage, TMessagesHandler>()
        where TMessage : Message
        where TMessagesHandler : IFixMessagesHandler<TMessage>;
}
