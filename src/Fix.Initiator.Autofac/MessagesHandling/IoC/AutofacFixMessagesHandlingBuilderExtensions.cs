using QuickFix;
using SoftWell.Fix.Initiator.MessagesHandling;

namespace Autofac;

public static class AutofacFixMessagesHandlingBuilderExtensions
{
    public static IAutofacFixMessagesHandlingBuilder RegisterMessagesHandler<TMessagesHandler>(this IAutofacFixMessagesHandlingBuilder builder)
        where TMessagesHandler : IFixMessagesHandler
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.RegisterMessagesHandler(ctx => ctx.Resolve<TMessagesHandler>());
    }

    public static IAutofacFixMessagesHandlingBuilder RegisterMessagesHandler<TMessage, TMessagesHandler>(this IAutofacFixMessagesHandlingBuilder builder)
        where TMessage : Message
        where TMessagesHandler : IFixMessagesHandler<TMessage>
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.RegisterMessagesHandler<TMessage, TMessagesHandler>(ctx => ctx.Resolve<TMessagesHandler>());
    }
}