using Microsoft.Extensions.DependencyInjection.Extensions;
using QuickFix;
using SoftWell.Fix.Initiator;
using SoftWell.Fix.Initiator.MessagesHandling;

namespace Microsoft.Extensions.DependencyInjection;

internal class FixMessagesHandlingBuilder<TFixMessagesReader> : IFixMessagesHandlingBuilder
    where TFixMessagesReader : IFixMessagesReader
{
    private readonly IServiceCollection _services;

    public FixMessagesHandlingBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IFixMessagesHandlingBuilder AddMessagesHandler<TMessagesHandler>()
        where TMessagesHandler : IFixMessagesHandler
    {
        _services.AddSingleton<
            IFixMessagesHandlerInternal<TFixMessagesReader>,
            FixMessagesHandlerInternal<TFixMessagesReader, TMessagesHandler>>();

        return this;
    }

    public IFixMessagesHandlingBuilder AddMessagesHandler<TMessage, TMessagesHandler>()
        where TMessage : Message
        where TMessagesHandler : IFixMessagesHandler<TMessage>
    {
        _services.AddSingleton<
            IFixMessagesHandlerInternal<TFixMessagesReader>,
            FixMessagesHandlerInternal<TFixMessagesReader, TMessage, TMessagesHandler>>();

        return this;
    }
}
