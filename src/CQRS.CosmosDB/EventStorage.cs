using CQRS.Aggregates;
using CQRS.Events;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQRS.CosmosDB
{
    public class CosmosEvent
    {
        public CosmosEvent()
        {

        }

        public CosmosEvent(Event evt, string aggregateType)
        {
            Id = evt.Id;
            Event = evt;
            AggregateType = aggregateType;
            AggregateId = evt.AggregateId.ToString();
            EventType = evt.Type;
            DateCreated = evt.DateCreated;
            Version = evt.Version;
        }
        [JsonProperty("id")]
        public string Id { get; set; }
        public string AggregateType { get; set; }
        public string AggregateId { get; set; }
        public object Event { get; set; }
        public string EventType { get; set; }
        public DateTime DateCreated { get; set; }
        public int Version { get; set; }
    }
    public class CosmosDBEventStorage : IEventStorage
    {
        private readonly CosmosClient _cosmosClient;
        private readonly CosmosDBRepositorySettings _settings;
        private readonly Dictionary<string, Type> _eventTypes;

        public CosmosDBEventStorage(CosmosClient cosmosClient, CosmosDBRepositorySettings settings)
        {
            _cosmosClient = cosmosClient;
            _settings = settings;
            _eventTypes = settings.EventAssembly.GetTypes().Where(t => typeof(Event).IsAssignableFrom(t)).ToDictionary(t => t.Name, t => t);
        }
        public async Task<List<Event>> GetEventsForAggregate<T>(Guid aggregateId) where T:AggregateRoot
        {
            var container = _cosmosClient.GetContainer(_settings.DatabaseName, _settings.ContainerName);
            var iterator = container.GetItemLinqQueryable<CosmosEvent>(requestOptions: new QueryRequestOptions {PartitionKey = new PartitionKey(aggregateId.ToString()) }).Where(e => e.AggregateId == aggregateId.ToString()).ToFeedIterator();
            return await CosmosEventsToEvents(iterator);
        }

        public async Task<List<Event>> GetEventsSince(int version, string aggregateId = null)
        {
            QueryRequestOptions requestOptions = new QueryRequestOptions();
            if (!string.IsNullOrEmpty(aggregateId))
            {
                requestOptions.PartitionKey = new PartitionKey(aggregateId);
            }
            var container = _cosmosClient.GetContainer(_settings.DatabaseName, _settings.ContainerName);
            var iterator = container
                .GetItemLinqQueryable<CosmosEvent>(requestOptions: requestOptions)
                .Where(e=>e.Version > version)
                .ToFeedIterator();
            return await CosmosEventsToEvents(iterator);
        }

        public async Task SaveNewEventsForAggregate<T>(Guid aggregateId, List<Event> newEvents) where T: AggregateRoot
        {
            var aggregateType = typeof(T).Name;
            var container = _cosmosClient.GetContainer(_settings.DatabaseName, _settings.ContainerName);
            var transactionalBatch = container.CreateTransactionalBatch(new PartitionKey(aggregateId.ToString()));
            foreach(var evt in newEvents)
            {
                transactionalBatch.CreateItem(new CosmosEvent(evt, aggregateType));
            }
            var response = await transactionalBatch.ExecuteAsync();
            if(!response.IsSuccessStatusCode)
            {
                throw new CosmosException($"TransactionalBatch saving failed with StatusCode {response.StatusCode} and Message {response.ErrorMessage}", response.StatusCode, (int)response.StatusCode, response.ActivityId, response.RequestCharge);
            }
        }

        private async Task<List<Event>> CosmosEventsToEvents(FeedIterator<CosmosEvent> cosmosEvents)
        {
            var events = new List<Event>();
            while (cosmosEvents.HasMoreResults)
            {
                foreach (var evt in await cosmosEvents.ReadNextAsync())
                {
                    var json = JsonConvert.SerializeObject(evt.Event);
                    var properEvent = (Event) JsonConvert.DeserializeObject(json, _eventTypes[evt.EventType]);
                    events.Add(properEvent);
                }
            }
            return events;
        }
    }
}
