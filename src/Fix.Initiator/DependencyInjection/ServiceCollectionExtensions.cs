using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickFix;
using SoftWell.Fix.Initiator;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFixInitiatorStarter<TFixInitiator>(
        this IServiceCollection services,
        string? name = null)
        where TFixInitiator : IInitiator
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSingleton<IHostedService>(
            sp => new FixInitiatorStarterService(
                sp.GetRequiredService<TFixInitiator>(),
                sp.GetRequiredService<ILogger<FixInitiatorStarterService>>(),
                name));
    }

    public static IServiceCollection AddFixInitiatorStarter(
        this IServiceCollection services,
        Func<IServiceProvider, IInitiator> initiatorFactory,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(initiatorFactory);

        return services.AddSingleton<IHostedService>(
            sp => new FixInitiatorStarterService(
                initiatorFactory(sp),
                sp.GetRequiredService<ILogger<FixInitiatorStarterService>>(),
                name));
    }

    public static IServiceCollection AddFixInitiatorStarter(
        this IServiceCollection services,
        IInitiator initiator,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(initiator);

        return services.AddSingleton<IHostedService>(
            sp => new FixInitiatorStarterService(
                initiator,
                sp.GetRequiredService<ILogger<FixInitiatorStarterService>>(),
                name));
    }

    public static IServiceCollection AddFixClientInitiatorStarter<TFixClient>(
        this IServiceCollection services,
        string? name = null)
            where TFixClient : IFixClient
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddFixClientInitiatorStarter<TFixClient>(
            sp => new FileStoreFactory(sp.GetRequiredService<TFixClient>().SessionSettings),
            sp => new FileLogFactory(sp.GetRequiredService<TFixClient>().SessionSettings),
            name);
    }

    public static IServiceCollection AddFixClientInitiatorStarter<TFixClient>(
        this IServiceCollection services,
        Func<IServiceProvider, IMessageStoreFactory> messageStoreFactoryFactory,
        Func<IServiceProvider, ILogFactory> logFactoryFactory,
        string? name = null)
            where TFixClient : IFixClient
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageStoreFactoryFactory);
        ArgumentNullException.ThrowIfNull(logFactoryFactory);

        services.AddFixInitiatorStarter(
            sp =>
            {
                var client = sp.GetRequiredService<TFixClient>();

                var initiator = new QuickFix.Transport.SocketInitiator(
                    client,
                    messageStoreFactoryFactory(sp),
                    client.SessionSettings,
                    logFactoryFactory(sp));

                return initiator;
            },
            name ?? typeof(TFixClient).Name);

        return services;
    }
}