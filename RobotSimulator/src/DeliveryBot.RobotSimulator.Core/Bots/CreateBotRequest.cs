using DeliveryBot.RobotSimulator.Core.Simulation;

namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed class CreateBotRequest
{
    public string? BotId { get; init; }
    public string? Model { get; init; }
    public GeoLocation? CurrentLocation { get; init; }
}