using DeliveryBot.RobotSimulator.Core.Bots;
using DeliveryBot.RobotSimulator.Core.Orders;
using DeliveryBot.RobotSimulator.Events;
using DeliveryBot.RobotSimulator.Infrastructure.Events;

namespace DeliveryBot.RobotSimulator.Api;

public sealed class OrderAssignmentHandler
{
    private readonly BotFleet _botFleet;
    private readonly IRobotEventPublisher _eventPublisher;
    private readonly ILogger<OrderAssignmentHandler> _logger;

    public OrderAssignmentHandler(
        BotFleet botFleet,
        IRobotEventPublisher eventPublisher,
        ILogger<OrderAssignmentHandler> logger)
    {
        _botFleet = botFleet;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<object> HandleAsync(
        OrderAssignment assignment,
        CancellationToken cancellationToken)
    {
        if (!_botFleet.TryGetBot(assignment.BotId, out var bot))
        {
            var notFoundResponse = new
            {
                assignment.OrderId,
                assignment.BotId,
                Result = "Rejected",
                Reason = "BotNotFound"
            };

            var notFoundEvent = RobotEventEnvelope.Create(
                RobotEventTypes.RobotOrderAssignmentResponse,
                notFoundResponse,
                assignment.BotId);

            await _eventPublisher.PublishAsync(
                notFoundEvent,
                cancellationToken);

            _logger.LogWarning(
                "Order assignment rejected because bot was not found. OrderId={OrderId} BotId={BotId}",
                assignment.OrderId,
                assignment.BotId);

            return notFoundResponse;
        }

        var result = bot.AssignOrder(assignment);

        var responseEvent = RobotEventEnvelope.Create(
            RobotEventTypes.RobotOrderAssignmentResponse,
            result,
            assignment.BotId);

        await _eventPublisher.PublishAsync(
            responseEvent,
            cancellationToken);

        foreach (var generatedEvent in result.GeneratedEvents)
        {
            await _eventPublisher.PublishAsync(
                generatedEvent,
                cancellationToken);
        }

        _logger.LogInformation(
            "Order assignment processed. OrderId={OrderId} BotId={BotId} Result={Result}",
            assignment.OrderId,
            assignment.BotId,
            result.Result);

        return result;
    }
}