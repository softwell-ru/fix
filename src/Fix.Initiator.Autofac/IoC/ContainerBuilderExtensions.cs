using Autofac.Core;
using Microsoft.Extensions.Hosting;
using QuickFix;
using SoftWell.Fix.Initiator;

namespace Autofac;

public static class ContainerBuilderExtensions
{
    public static ContainerBuilder RegisterFixInitiatorStarter<TFixInitiator>(
        this ContainerBuilder builder,
        string? name = null)
        where TFixInitiator : IInitiator
    {
        ArgumentNullException.ThrowIfNull(builder);

        var reg = builder.RegisterType<FixInitiatorStarterService>()
            .WithParameter(new ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(IInitiator),
                (pi, ctx) => ctx.Resolve<TFixInitiator>()));

        if (name is not null)
        {
            reg = reg.WithParameter(new NamedParameter("name", name));
        }

        reg.As<IHostedService>().SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterFixInitiatorStarter(
        this ContainerBuilder builder,
        Func<IComponentContext, IInitiator> initiatorFactory,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(initiatorFactory);

        var reg = builder.RegisterType<FixInitiatorStarterService>()
            .WithParameter(new ResolvedParameter(
                (pi, ctx) => pi.ParameterType == typeof(IInitiator),
                (pi, ctx) => initiatorFactory(ctx)));

        if (name is not null)
        {
            reg = reg.WithParameter(new NamedParameter("name", name));
        }

        reg.As<IHostedService>().SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterFixInitiatorStarter(
        this ContainerBuilder builder,
        IInitiator initiator,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(initiator);

        var reg = builder.RegisterType<FixInitiatorStarterService>()
            .WithParameter(new TypedParameter(typeof(IInitiator), initiator));

        if (name is not null)
        {
            reg = reg.WithParameter(new NamedParameter("name", name));
        }

        reg.As<IHostedService>().SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterFixClientInitiatorStarter<TFixClient>(
        this ContainerBuilder builder,
        string? name = null)
            where TFixClient : IFixClient
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RegisterFixClientInitiatorStarter(
            ctx => ctx.Resolve<TFixClient>(),
            name);
    }

    public static ContainerBuilder RegisterFixClientInitiatorStarter<TFixClient>(
        this ContainerBuilder builder,
        Func<IComponentContext, TFixClient> resolver,
        string? name = null)
            where TFixClient : IFixClient
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(resolver);

        return builder.RegisterFixClientInitiatorStarter(
            resolver,
            ctx => new FileStoreFactory(resolver(ctx).SessionSettings),
            ctx => new FileLogFactory(resolver(ctx).SessionSettings),
            name);
    }

    public static ContainerBuilder RegisterFixClientInitiatorStarter<TFixClient>(
        this ContainerBuilder builder,
        Func<IComponentContext, IMessageStoreFactory> messageStoreFactoryFactory,
        Func<IComponentContext, ILogFactory> logFactoryFactory,
        string? name = null)
            where TFixClient : IFixClient
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RegisterFixClientInitiatorStarter(
            ctx => ctx.Resolve<TFixClient>(),
            messageStoreFactoryFactory,
            logFactoryFactory,
            name);
    }

    public static ContainerBuilder RegisterFixClientInitiatorStarter<TFixClient>(
        this ContainerBuilder builder,
        Func<IComponentContext, TFixClient> resolver,
        Func<IComponentContext, IMessageStoreFactory> messageStoreFactoryFactory,
        Func<IComponentContext, ILogFactory> logFactoryFactory,
        string? name = null)
            where TFixClient : IFixClient
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(messageStoreFactoryFactory);
        ArgumentNullException.ThrowIfNull(logFactoryFactory);

        builder.RegisterFixInitiatorStarter(
            ctx =>
            {
                var client = resolver(ctx);

                var initiator = new QuickFix.Transport.SocketInitiator(
                    client,
                    messageStoreFactoryFactory(ctx),
                    client.SessionSettings,
                    logFactoryFactory(ctx));

                return initiator;
            },
            name ?? typeof(TFixClient).Name);

        return builder;
    }
}