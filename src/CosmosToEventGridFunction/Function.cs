using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CQRS.CosmosDB;
using CQRS.Events;
using Microsoft.Azure.Documents;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CosmosToEventGridFunction
{
    public class CosmosToEventGridOptions
    {
        public CosmosToEventGridOptions(Assembly assembly, string domainSubject)
        {
            EventAssembly = assembly;
            DomainSubject = domainSubject;
        }
        public Assembly EventAssembly { get; set; }
        public string DomainSubject { get; set; }
    }
    public class BaseCosmosToEventGridFunction
    {
        public BaseCosmosToEventGridFunction(CosmosToEventGridOptions options, ILogger log)
        {
            _integrationEventTypes = options.EventAssembly.GetTypes().Where(t => typeof(EventWithIntegrationEvent).IsAssignableFrom(t)).ToDictionary(t => t.Name, t => t);
            _options = options;
            _log = log;
        }
        private const string IntegrationSubject = "integration";
        private readonly Dictionary<string, Type> _integrationEventTypes;
        private readonly CosmosToEventGridOptions _options;
        private readonly ILogger _log;

        [FunctionName("Function")]
        public async Task Process(IReadOnlyList<Document> input)
        {
            if (input != null && input.Count > 0)
            {
                _log.LogInformation("Documents modified " + input.Count);
                string topicHostname = new Uri(Environment.GetEnvironmentVariable("EventGridTopicConnectionString")).Host;
                TopicCredentials topicCredentials = new TopicCredentials(Environment.GetEnvironmentVariable("EventGridTopicKey"));
                EventGridClient client = new EventGridClient(topicCredentials);
                await client.PublishEventsAsync(topicHostname, GetEventsList(input));
            }
        }

        private IList<EventGridEvent> GetEventsList(IReadOnlyList<Document> input)
        {
            List<EventGridEvent> events = new List<EventGridEvent>();
            foreach(var document in input)
            {
                var evt = JsonConvert.DeserializeObject<CosmosEvent>(document.ToString());
                _log.LogInformation("Creating Event Grid {EventType} for {Subject} with {Id}", evt.EventType, _options.DomainSubject, evt.Id);
                events.Add(new EventGridEvent()
                {
                    Id = evt.Id,
                    Data = evt.Event,
                    EventTime = evt.DateCreated,
                    EventType = evt.EventType,
                    Subject = _options.DomainSubject,
                    DataVersion = "1.0"
                });

                if(_integrationEventTypes.TryGetValue(evt.EventType, out var eventType))
                {
                    _log.LogInformation("{EventType} is also an integration event", evt.EventType);
                    // This is also an integration event, so let's generate that too
                    var eventJson = JsonConvert.SerializeObject(evt.Event);
                    var eventWithIntegration = (EventWithIntegrationEvent) JsonConvert.DeserializeObject(eventJson, eventType);
                    var integrationEvent = eventWithIntegration.ToIntegrationEvent();
                    _log.LogInformation("Creating Integration Event Grid {EventType} for {Subject} with {Id}", evt.EventType, IntegrationSubject, integrationEvent.Id);
                    events.Add(new EventGridEvent()
                    {
                        Id = integrationEvent.Id,
                        Data = integrationEvent,
                        EventTime = integrationEvent.DateCreated,
                        EventType = integrationEvent.Type,
                        Subject = IntegrationSubject,
                        DataVersion = "1.0"
                    });
                }
                
            }
            return events;
        }
    }
}
