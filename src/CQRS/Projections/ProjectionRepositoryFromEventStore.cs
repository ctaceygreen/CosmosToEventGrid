using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Projections
{
    public interface IProjectionRepository<T> where T : Projection, new()
    {
        Task<T> Get(string key);
        Task Save(T proj);
    }

    public class ProjectionRepositoryFromEventStore<T> : IProjectionRepository<T> where T : Projection, new()
    {
        private readonly IReadEventStore _storage;
        private readonly IProjectionStore<T> _projectionStorage;
        private readonly ILogger<ProjectionRepositoryFromEventStore<T>> _log;

        public ProjectionRepositoryFromEventStore(ILogger<ProjectionRepositoryFromEventStore<T>> log, IReadEventStore storage, IProjectionStore<T> projectionStorage)
        {
            _storage = storage;
            _projectionStorage = projectionStorage;
            _log = log;
        }
        
        public async Task<T> Get(string key)
        {
            // If we can't find a projection in our store, then let's build one from the events
            var currentProjection = await _projectionStorage.Get(key);
            bool isNewProjection = false;
            if (currentProjection == null)
            {
                currentProjection = new T();
                currentProjection.Key = key;
                isNewProjection = true;
            }
            try
            {
                string aggregateId = currentProjection.KeyIsAggregateId() ? key : null;
                var e = await _storage.GetEventsSince(currentProjection.Version, aggregateId);
                if (isNewProjection && !e.Any())
                {
                    _log.LogWarning("Projection is new and there are no events to load, so returning null");
                    return null;
                }
                currentProjection.LoadsFromHistory(e);
                
            }
            catch(Exception e)
            {
                _log.LogError(e, "Loading events for {Projection} threw an exception", typeof(T).Name);

                if(isNewProjection)
                {
                    _log.LogWarning("Projection is new and event loading failed, so returning null");
                    return null;
                }
                // Here we catch any errors from the event store, so that we can protect against failures on this side, and still return our latest snapshot of the projection
            }
            return currentProjection;
        }

        public async Task Save(T proj)
        {
            await _projectionStorage.Save(proj, proj.Key);
        }
    }
}
