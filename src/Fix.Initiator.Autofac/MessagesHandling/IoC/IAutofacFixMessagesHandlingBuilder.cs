using QuickFix;
using SoftWell.Fix.Initiator.MessagesHandling;

namespace Autofac;

public interface IAutofacFixMessagesHandlingBuilder
{
    IAutofacFixMessagesHandlingBuilder RegisterMessagesHandler<TMessagesHandler>(Func<IComponentContext, TMessagesHandler> resolver)
        where TMessagesHandler : IFixMessagesHandler;

    IAutofacFixMessagesHandlingBuilder RegisterMessagesHandler<TMessage, TMessagesHandler>(Func<IComponentContext, TMessagesHandler> resolver)
        where TMessage : Message
        where TMessagesHandler : IFixMessagesHandler<TMessage>;
}
