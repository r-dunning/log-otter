﻿using Microsoft.Extensions.Options;

namespace LogOtter.CosmosDb.EventStore;

public class EventRepository<TBaseEvent, TSnapshot>
    where TBaseEvent : class, IEvent<TSnapshot>
    where TSnapshot : class, ISnapshot, new()
{
    private readonly EventStore _eventStore;
    private readonly EventStoreOptions _options;

    public EventRepository(EventStoreDependency<TBaseEvent> eventStoreDependency, IOptions<EventStoreOptions> options)
    {
        _eventStore = eventStoreDependency.EventStore;
        _options = options.Value;
    }

    public async Task<TSnapshot?> Get(
        string id,
        int? revision = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default
    )
    {
        var streamId = _options.EscapeIdIfRequired(id);

        var eventStoreEvents = await _eventStore.ReadStreamForwards(streamId, cancellationToken);

        var eventsSelect = eventStoreEvents.Select(e => (TBaseEvent)e.EventBody);

        var events = revision != null
            ? eventsSelect.Take(revision.Value).ToList()
            : eventsSelect.ToList();

        if (!events.Any())
        {
            return null;
        }

        var model = new TSnapshot
        {
            Revision = events.Count,
            Id = streamId
        };

        foreach (var @event in events)
        {
            @event.Apply(model);
        }

        if (model.DeletedAt.HasValue && !includeDeleted)
        {
            return null;
        }

        return model;
    }

    public async Task<IReadOnlyCollection<TBaseEvent>> GetEventStream(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var streamId = _options.EscapeIdIfRequired(id);
        var eventStoreEvents = await _eventStore.ReadStreamForwards(streamId, cancellationToken);

        var events = eventStoreEvents
            .Select(e => (TBaseEvent)e.EventBody)
            .ToList();

        return events;
    }

    public async Task<TSnapshot> ApplyEvents(
        string id,
        int? expectedRevision,
        params TBaseEvent[] events
    )
    {
        return await ApplyEvents(id, expectedRevision, CancellationToken.None, events);
    }

    public async Task<TSnapshot> ApplyEvents(
        string id,
        int? expectedRevision,
        CancellationToken cancellationToken,
        params TBaseEvent[] events
    )
    {
        if (events.Any(e => e.EventStreamId != id))
        {
            throw new ArgumentException("All events must be for the same entity", nameof(events));
        }

        var entity = await Get(id, null, true, cancellationToken) ?? new TSnapshot();

        foreach (var eventToApply in events)
        {
            eventToApply.Apply(entity);
        }

        var streamId = _options.EscapeIdIfRequired(id);

        var eventData = events
            .Select(e => new EventData(Guid.NewGuid(), e, e.Ttl ?? -1))
            .ToArray();

        await _eventStore.AppendToStream(streamId, expectedRevision ?? 0, cancellationToken, eventData);

        entity.Revision += events.Length;

        return entity;
    }
}