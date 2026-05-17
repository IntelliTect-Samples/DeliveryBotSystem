using DeliveryBot.RobotSimulator.Core.Simulation;

namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed class UpdateBotRequest
{
    public string? Model { get; init; }
    public GeoLocation? CurrentLocation { get; init; }
    public double? PowerLevel { get; init; }
    public double? ExternalTemperature { get; init; }
    public double? InternalStorageTemperature { get; init; }
}