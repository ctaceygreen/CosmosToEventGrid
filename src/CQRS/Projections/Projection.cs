using CQRS.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQRS.Projections
{
    public abstract class Projection
    {
        public Projection()
        {
            ProjectionType = GetType().Name;
        }
        public int Version { get; set; }
        public string Key { get; set; }
        public string Id  => Key;
        public string ProjectionType { get; set; }

        public void LoadsFromHistory(IEnumerable<Event> history)
        {
            foreach (var e in history) ApplyChange(e);
        }

        public void ApplyChange(Event @event)
        {
            this.AsDynamic().Handle(@event);
            Version = @event.Version;
        }

        public abstract bool KeyIsAggregateId();

    }
}
