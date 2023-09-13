using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftWell.Fix.Initiator;
using SoftWell.Fix.Initiator.MessagesHandling;

namespace Autofac;

public static class FixMessagesHandlingContainerBuilderExtensions
{
    private static readonly ConcurrentDictionary<string, int> _registrationes = new(StringComparer.Ordinal);

    public static ContainerBuilder RegisterFixMessagesHandling<TFixMessagesReader>(
        this ContainerBuilder builder,
        Action<IAutofacFixMessagesHandlingBuilder> configure)
            where TFixMessagesReader : IFixMessagesReader
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RegisterFixMessagesHandling(
            ctx => ctx.Resolve<TFixMessagesReader>(),
            configure);
    }

    public static ContainerBuilder RegisterFixMessagesHandling<TFixMessagesReader>(
        this ContainerBuilder builder,
        Func<IComponentContext, TFixMessagesReader> resolver,
        Action<IAutofacFixMessagesHandlingBuilder> configure)
            where TFixMessagesReader : IFixMessagesReader
    {
        ArgumentNullException.ThrowIfNull(builder);

        var name = typeof(TFixMessagesReader).Name;

        var index = _registrationes.AddOrUpdate(name, 1, (_, cur) => cur + 1);

        var registrationName = $"{name}_{index}";

        var b = new AutofacFixMessagesHandlingBuilder<TFixMessagesReader>(builder, registrationName);

        configure?.Invoke(b);

        builder.Register(
            ctx => new FixMessagesRouterService<TFixMessagesReader>(
                resolver(ctx),
                ctx.ResolveNamed<IEnumerable<IFixMessagesHandlerInternal<TFixMessagesReader>>>(registrationName),
                ctx.Resolve<ILogger<FixMessagesRouterService<TFixMessagesReader>>>()))
            .As<IHostedService>()
            .SingleInstance();

        return builder;
    }
}
