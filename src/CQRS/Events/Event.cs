using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQRS.Events
{
    public class Event : Message
    {
        public int Version { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public Guid AggregateId { get; set; }
        public DateTime DateCreated { get; set; }
        public string CorrelationId { get; set; }
        public string CausationId { get; set; }
        public string ServiceName { get; set; }
        public Event()
        {

        }
        public Event(Guid aggregateId)
        {
            AggregateId = aggregateId;
            Type = GetType().Name;
            DateCreated = DateTime.UtcNow;
            Id = Guid.NewGuid().ToString();
        }
    }

    public abstract class EventWithIntegrationEvent : Event
    {
        public EventWithIntegrationEvent(Guid aggregateId) : base(aggregateId)
        {
        }

        public abstract Event ToIntegrationEvent();
    }
}
