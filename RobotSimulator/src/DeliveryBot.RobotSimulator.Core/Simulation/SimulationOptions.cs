namespace DeliveryBot.RobotSimulator.Core.Simulation;

public sealed class SimulationOptions
{
    public int TickIntervalSeconds { get; init; } = 1;
    public int TelemetryIntervalSeconds { get; init; } = 5;
    public double DeliverySpeedMetersPerSecond { get; init; } = 8;
    public double DestinationArrivalThresholdMeters { get; init; } = 5;
}