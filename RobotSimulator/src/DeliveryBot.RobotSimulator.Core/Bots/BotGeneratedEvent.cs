using DeliveryBot.RobotSimulator.Events;

namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed record BotGeneratedEvent(
    RobotEventEnvelope Envelope
);