using CQRS.Aggregates;
using CQRS.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQRS
{
    public interface IReadEventStore
    {
        Task<List<Event>> GetEventsSince(int version, string aggregateId = null);

    }

    public class ReadEventStore : IReadEventStore
    {
        private IEventStorage _eventStorage;
        public ReadEventStore(IEventStorage eventStorage)
        {
            _eventStorage = eventStorage;
        }

        public async Task<List<Event>> GetEventsSince(int version, string aggregateId = null)
        {
            return (await _eventStorage.GetEventsSince(version, aggregateId)).ToList();
        }
    }

    public interface IEventStore
    {
        Task SaveEvents<T>(Guid aggregateId, IEnumerable<Event> events, int expectedVersion) where T:AggregateRoot;
        Task<List<Event>> GetEventsForAggregate<T>(Guid aggregateId) where T:AggregateRoot;
    }

    public class EventDescriptor
    {

        public readonly Event EventData;
        public readonly Guid Id;
        public readonly int Version;
        public readonly Guid AggregateId;

        public EventDescriptor(Guid id, Guid aggregateId, Event eventData, int version)
        {
            EventData = eventData;
            Version = version;
            Id = id;
            AggregateId = aggregateId;
        }
    }

    //NOTE : This is just to simulate a shared event store. Obviously don't use this.
    public static class EventStorage
    {
        public static List<EventDescriptor> Current = new List<EventDescriptor>();
    }

    public class EventStore : IEventStore
    {
        private readonly IEventPublisher _publisher;
        private readonly IEventStorage _eventStorage;
        private readonly ICorrelationIdAccessor _correlationIdAccessor;
        private readonly ICausationIdAccessor _causationIdAccessor;
        private readonly EventStoreOptions _options;

        public EventStore(IEventPublisher publisher, IEventStorage eventStorage, ICorrelationIdAccessor correlationIdAccessor, ICausationIdAccessor causationIdAccessor, EventStoreOptions options)
        {
            _publisher = publisher;
            _eventStorage = eventStorage;
            _correlationIdAccessor = correlationIdAccessor;
            _causationIdAccessor = causationIdAccessor;
            _options = options;
        }

        public async Task SaveEvents<T>(Guid aggregateId, IEnumerable<Event> events, int expectedVersion) where T:AggregateRoot
        {
            List<Event> Events = await _eventStorage.GetEventsForAggregate<T>(aggregateId);

            // try to get event list for given aggregate id
            // otherwise -> create empty list
            if (Events == null || !Events.Any())
            {
                Events = new List<Event>();
            }
            // check whether latest event version matches current aggregate version
            // otherwise -> throw exception
            else if (Events[Events.Count - 1].Version != expectedVersion && expectedVersion != -1)
            {
                throw new ConcurrencyException();
            }
            var i = expectedVersion;

            // iterate through current aggregate events increasing version with each processed event
            List<Event> newEvents = new List<Event>();
            foreach (var @event in events)
            {
                i++;
                @event.Version = i;
                @event.CorrelationId = _correlationIdAccessor.GetCorrelationId();
                @event.CausationId = _causationIdAccessor.GetCausationId();
                @event.ServiceName = _options.ServiceName;

                // push event to the event descriptors list for current aggregate
                @event.AggregateId = aggregateId;
                newEvents.Add(@event);
            }

            //Save events to storage
            if (newEvents != null && newEvents.Any())
            {
                await _eventStorage.SaveNewEventsForAggregate<T>(aggregateId, newEvents);
            }

            foreach (var @event in events)
            {
                // publish current event to the bus for further processing by subscribers
                //await _publisher.Publish(@event); CTG Removed as we want to publish events properly within a transaction
            }
        }

        // collect all processed events for given aggregate and return them as a list
        // used to build up an aggregate from its history (Domain.LoadsFromHistory)
        public async Task<List<Event>> GetEventsForAggregate<T>(Guid aggregateId) where T:AggregateRoot
        {
            List<Event> Events = await _eventStorage.GetEventsForAggregate<T>(aggregateId);

            if (Events == null || !Events.Any())
            {
                throw new AggregateNotFoundException();
            }

            return Events.ToList();
        }
    }

    public class AggregateNotFoundException : Exception
    {
    }

    public class ConcurrencyException : Exception
    {
    }

    public class EventStoreOptions
    {
        public string ServiceName { get; set; }
    }
}
