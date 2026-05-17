using DeliveryBot.RobotSimulator.Events;

namespace DeliveryBot.RobotSimulator.Infrastructure.Events;

public sealed class RecentRobotEventStore
{
    private readonly object _lock = new();
    private readonly Queue<RobotEventEnvelope> _events = new();
    private readonly int _maxEvents;

    public RecentRobotEventStore(int maxEvents = 100)
    {
        _maxEvents = maxEvents;
    }

    public void Add(RobotEventEnvelope envelope)
    {
        lock (_lock)
        {
            _events.Enqueue(envelope);

            while (_events.Count > _maxEvents)
            {
                _events.Dequeue();
            }
        }
    }

    public IReadOnlyCollection<RobotEventEnvelope> GetRecent(int count = 50)
    {
        lock (_lock)
        {
            return _events
                .Reverse()
                .Take(count)
                .ToList();
        }
    }
}