using CQRS.Projections;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.CosmosDB
{
    public class CosmosProjection<T> where T : Projection
    {
        public CosmosProjection(T projection)
        {
            Projection = projection;
            Id = projection.Id;
            ProjectionType = projection.ProjectionType;
        }
        [JsonProperty("id")]
        public string Id { get; set; }
        public T Projection { get; set; }
        public string ProjectionType { get; set; }
    }

    public class CosmosDBProjectionStore<T> : IProjectionStore<T> where T : Projection
    {
        private readonly CosmosClient _cosmosClient;
        private readonly CosmosDBProjectionRepositorySettings _settings;
        public CosmosDBProjectionStore(CosmosClient cosmosClient, CosmosDBProjectionRepositorySettings settings)
        {
            _cosmosClient = cosmosClient;
            _settings = settings;
        }
        public async Task<T> Get(string key)
        {
            var container = _cosmosClient.GetContainer(_settings.DatabaseName, _settings.ContainerName);
            try
            {
                var item = await container.ReadItemAsync<CosmosProjection<T>>(key, new PartitionKey(typeof(T).Name));
                return item.Resource.Projection;
            }
            catch(CosmosException ex)
            {
                if(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw;
            }
        }

        public async Task Save(T projection, string key)
        {
            var container = _cosmosClient.GetContainer(_settings.DatabaseName, _settings.ContainerName);
            await container.UpsertItemAsync<CosmosProjection<T>>(new CosmosProjection<T>(projection));
        }
    }
}
