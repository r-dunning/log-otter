using Microsoft.Extensions.Logging;

namespace LogOtter.CosmosDb.EventStore;

internal class SnapshotProjectionCatchupSubscription<TBaseEvent, TSnapshot> : ICatchupSubscription<TBaseEvent>
    where TBaseEvent : class, IEvent<TSnapshot> where TSnapshot : class, ISnapshot, new()
{
    private readonly ILogger<SnapshotProjectionCatchupSubscription<TBaseEvent, TSnapshot>> _logger;
    private readonly EventStoreMetadata<TBaseEvent, TSnapshot> _metadata;
    private readonly SnapshotRepository<TBaseEvent, TSnapshot> _snapshotRepository;

    public SnapshotProjectionCatchupSubscription(
        SnapshotRepository<TBaseEvent, TSnapshot> snapshotRepository,
        ILogger<SnapshotProjectionCatchupSubscription<TBaseEvent, TSnapshot>> logger,
        EventStoreMetadata<TBaseEvent, TSnapshot> metadata)
    {
        _snapshotRepository = snapshotRepository;
        _logger = logger;
        _metadata = metadata;
    }

    public async Task ProcessEvents(IReadOnlyCollection<Event<TBaseEvent>> events, CancellationToken cancellationToken)
    {
        var eventsByStream = events.GroupBy(e => e.StreamId);
        foreach (var eventsForStream in eventsByStream)
        {
            try
            {
                await ApplyEventsToSingleSnapshot(eventsForStream, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Snapshot Projection: Error applying events to snapshot for stream ID {StreamId}. Starting revision was {StartRevision}. Attempting to update to {EndRevision}",
                    eventsForStream.Key,
                    eventsForStream.Min(e => e.EventNumber),
                    eventsForStream.Max(e => e.EventNumber));
                throw;
            }
        }
    }

    private async Task ApplyEventsToSingleSnapshot(IGrouping<string, Event<TBaseEvent>> events, CancellationToken cancellationToken)
    {
        var partitionKey = _metadata.SnapshotPartitionKeyResolver(events.First().Body);
        var streamId = events.Key;

        await _snapshotRepository.ApplyEventsToSnapshot(
            streamId,
            partitionKey,
            events.ToList(),
            cancellationToken);
    }
}
