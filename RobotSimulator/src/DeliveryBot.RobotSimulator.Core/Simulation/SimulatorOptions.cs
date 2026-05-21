namespace DeliveryBot.RobotSimulator.Core.Simulation;

public sealed class SimulatorOptions
{
    public int InitialBotCount { get; init; } = 3;
    public string BotIdPrefix { get; init; } = "bot";
    public string DefaultBotModel { get; init; } = "DeliveryBot-V1";
    public double DefaultLatitude { get; init; } = 33.4255;
    public double DefaultLongitude { get; init; } = -111.9400;
}