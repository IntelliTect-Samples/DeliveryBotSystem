using DeliveryBot.RobotSimulator.Infrastructure.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeliveryBot.RobotSimulator.Infrastructure.Configuration;

public static class RobotSimulatorInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRobotEventTransport(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection("EventTransport")
            .Get<EventTransportOptions>() ?? new EventTransportOptions();

        services.AddSingleton(options);
        services.AddSingleton<RecentRobotEventStore>();

        if (string.Equals(options.Mode, EventTransportModes.Local, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IRobotEventPublisher, LocalRobotEventPublisher>();
            services.AddSingleton<IRobotEventConsumer, NoOpRobotEventConsumer>();
            return services;
        }

        if (string.Equals(options.Mode, EventTransportModes.AzureEventHub, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<AzureRobotEventPublisher>();
            services.AddSingleton<RecentRobotEventPublisher>();

            services.AddSingleton<IRobotEventPublisher>(sp =>
            {
                var azurePublisher = sp.GetRequiredService<AzureRobotEventPublisher>();
                var recentPublisher = sp.GetRequiredService<RecentRobotEventPublisher>();
                var logger = sp.GetRequiredService<ILogger<CompositeRobotEventPublisher>>();

                return new CompositeRobotEventPublisher(
                    new IRobotEventPublisher[]
                    {
                        azurePublisher,
                        recentPublisher
                    },
                    logger);
            });

            if (options.EnableInputConsumer)
            {
                services.AddSingleton<IRobotEventConsumer, AzureRobotEventConsumer>();
            }
            else
            {
                services.AddSingleton<IRobotEventConsumer, NoOpRobotEventConsumer>();
            }

            return services;
        }

        throw new InvalidOperationException(
            $"Unsupported event transport mode '{options.Mode}'. Supported modes: {EventTransportModes.Local}, {EventTransportModes.AzureEventHub}.");
    }
}