using DeliveryBot.RobotSimulator.Core.Simulation;
using DeliveryBot.RobotSimulator.Core.Stock;

namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed class BotFleet
{
    private readonly Dictionary<string, SimulatedBot> _bots = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public IReadOnlyCollection<BotSnapshot> GetAll()
    {
        lock (_lock)
        {
            return _bots.Values
                .Select(bot => bot.ToSnapshot())
                .ToList();
        }
    }

    public BotSnapshot? Get(string botId)
    {
        lock (_lock)
        {
            return _bots.TryGetValue(botId, out var bot)
                ? bot.ToSnapshot()
                : null;
        }
    }

    public bool TryGetBot(string botId, out SimulatedBot bot)
    {
        lock (_lock)
        {
            return _bots.TryGetValue(botId, out bot!);
        }
    }

    public SimulatedBot AddDefaultBot(string botId)
    {
        return AddBot(
            botId,
            model: "DeliveryBot-V1",
            location: new GeoLocation(33.4255, -111.9400));
    }

    public SimulatedBot AddBot(
        string botId,
        string model,
        GeoLocation location)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(botId))
            {
                throw new ArgumentException("Bot ID is required.", nameof(botId));
            }

            if (_bots.ContainsKey(botId))
            {
                throw new InvalidOperationException($"Bot {botId} already exists.");
            }

            var bot = new SimulatedBot(
                botId,
                model,
                location,
                stock:
                [
                    new BotStockItem("water", "Water", 20),
                    new BotStockItem("soda", "Soda", 20),
                    new BotStockItem("chips", "Chips", 15),
                    new BotStockItem("sandwich", "Sandwich", 10)
                ]);

            _bots.Add(bot.BotId, bot);

            return bot;
        }
    }

    public BotSnapshot? UpdateBot(
        string botId,
        UpdateBotRequest request)
    {
        lock (_lock)
        {
            if (!_bots.TryGetValue(botId, out var bot))
            {
                return null;
            }

            return bot.Update(request);
        }
    }

    public bool RemoveBot(
        string botId,
        out BotSnapshot? removedBot,
        out string? reason)
    {
        lock (_lock)
        {
            removedBot = null;
            reason = null;

            if (!_bots.TryGetValue(botId, out var bot))
            {
                reason = "BotNotFound";
                return false;
            }

            if (bot.HasActiveOrQueuedOrders)
            {
                reason = "BotHasActiveOrQueuedOrders";
                return false;
            }

            removedBot = bot.ToSnapshot();
            _bots.Remove(botId);

            return true;
        }
    }

    public void InitializeDefaultFleet(SimulatorOptions options)
    {
        lock (_lock)
        {
            if (_bots.Count > 0)
            {
                return;
            }

            for (var i = 1; i <= options.InitialBotCount; i++)
            {
                var botId = $"{options.BotIdPrefix}-{i:000}";

                var bot = new SimulatedBot(
                    botId,
                    options.DefaultBotModel,
                    new GeoLocation(
                        options.DefaultLatitude,
                        options.DefaultLongitude),
                    stock:
                    [
                        new BotStockItem("water", "Water", 20),
                        new BotStockItem("soda", "Soda", 20),
                        new BotStockItem("chips", "Chips", 15),
                        new BotStockItem("sandwich", "Sandwich", 10)
                    ]);

                _bots.Add(bot.BotId, bot);
            }
        }
    }

    public IReadOnlyCollection<(string BotId, BotSimulationTickResult Result)> TickAll(
        DateTimeOffset now,
        TimeSpan elapsed,
        SimulationOptions options)
    {
        lock (_lock)
        {
            return _bots.Values
                .Select(bot => (bot.BotId, bot.Tick(now, elapsed, options)))
                .ToList();
        }
    }
}