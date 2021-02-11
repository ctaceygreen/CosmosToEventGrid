using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Projections
{
    public class EventualProjectionRepository<T> : IProjectionRepository<T> where T : Projection, new()
    {
        private readonly IProjectionStore<T> _projectionStorage;
        private readonly ILogger<EventualProjectionRepository<T>> _log;

        public EventualProjectionRepository(ILogger<EventualProjectionRepository<T>> log, IProjectionStore<T> projectionStorage)
        {
            _projectionStorage = projectionStorage;
            _log = log;
        }

        public async Task<T> Get(string key)
        {
            // If we can't find a projection in our store, then let's build one from the events
            var currentProjection = await _projectionStorage.Get(key);
            if (currentProjection == null)
            {
                currentProjection = new T();
                currentProjection.Key = key;
            }
            return currentProjection;
        }

        public async Task Save(T proj)
        {
            await _projectionStorage.Save(proj, proj.Key);
        }
    }
}
