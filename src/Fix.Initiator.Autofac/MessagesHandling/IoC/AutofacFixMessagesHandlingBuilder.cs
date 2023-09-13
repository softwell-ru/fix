using QuickFix;
using SoftWell.Fix.Initiator;
using SoftWell.Fix.Initiator.MessagesHandling;

namespace Autofac;

internal class AutofacFixMessagesHandlingBuilder<TFixMessagesReader> : IAutofacFixMessagesHandlingBuilder
    where TFixMessagesReader : IFixMessagesReader
{
    private readonly ContainerBuilder _builder;

    private readonly string _registrationName;

    public AutofacFixMessagesHandlingBuilder(ContainerBuilder builder, string registrationName)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _registrationName = registrationName ?? throw new ArgumentNullException(nameof(registrationName));
    }

    public IAutofacFixMessagesHandlingBuilder RegisterMessagesHandler<TMessagesHandler>(Func<IComponentContext, TMessagesHandler> resolver)
        where TMessagesHandler : IFixMessagesHandler
    {
        ArgumentNullException.ThrowIfNull(resolver);

        _builder.Register(ctx => new FixMessagesHandlerInternal<TFixMessagesReader, TMessagesHandler>(resolver(ctx)))
            .Named<IFixMessagesHandlerInternal<TFixMessagesReader>>(_registrationName)
            .SingleInstance();

        return this;
    }

    public IAutofacFixMessagesHandlingBuilder RegisterMessagesHandler<TMessage, TMessagesHandler>(Func<IComponentContext, TMessagesHandler> resolver)
        where TMessage : Message
        where TMessagesHandler : IFixMessagesHandler<TMessage>
    {
        ArgumentNullException.ThrowIfNull(resolver);

        _builder.Register(ctx => new FixMessagesHandlerInternal<TFixMessagesReader, TMessage, TMessagesHandler>(resolver(ctx)))
            .Named<IFixMessagesHandlerInternal<TFixMessagesReader>>(_registrationName)
            .SingleInstance();

        return this;
    }
}
