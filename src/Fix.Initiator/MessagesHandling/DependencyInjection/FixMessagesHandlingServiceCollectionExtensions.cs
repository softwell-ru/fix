using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SoftWell.Fix.Initiator;
using SoftWell.Fix.Initiator.MessagesHandling;

namespace Microsoft.Extensions.DependencyInjection;

public static class FixMessagesHandlingServiceCollectionExtensions
{
    public static IServiceCollection AddFixMessagesHandling<TFixMessagesReader>(
        this IServiceCollection services,
        Action<IFixMessagesHandlingBuilder> configure)
            where TFixMessagesReader : IFixMessagesReader
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = new FixMessagesHandlingBuilder<TFixMessagesReader>(services);

        configure?.Invoke(builder);

        services.AddSingleton<IHostedService, FixMessagesRouterService<TFixMessagesReader>>();

        return services;
    }
}
