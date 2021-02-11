using CQRS.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Events
{
    public interface IEventStorage
    {
        Task<List<Event>> GetEventsForAggregate<T>(Guid aggregateId) where T: AggregateRoot;
        Task<List<Event>> GetEventsSince(int version, string aggregateId = null);
        Task SaveNewEventsForAggregate<T>(Guid aggregateId, List<Event> newEvents) where T:AggregateRoot;
    }

    public class BullshitEventStorage : IEventStorage
    {
        private readonly List<Event> _events = new List<Event>();
        private int _eventCounter = 0;
        public async Task<List<Event>> GetEventsForAggregate<T>(Guid aggregateId) where T:AggregateRoot
        {
            return _events.Where(e => e.AggregateId == aggregateId).ToList();
        }

        public async Task<List<Event>> GetEventsSince(int version, string aggregateId = null)
        {
            // This is just mimicking a comparison that would happen on your store of ids
            return _events.Where(e => e.Version > version).ToList();
        }

        public async Task SaveNewEventsForAggregate<T>(Guid aggregateId, List<Event> newEvents) where T:AggregateRoot
        {
            foreach(var ev in newEvents)
            {
                // Just mimicking an id of the store being incremented
                ev.Id = _eventCounter.ToString();
                _eventCounter++;
                _events.Add(ev);
            }
        }
    }
}
