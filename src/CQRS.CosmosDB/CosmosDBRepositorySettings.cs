using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CQRS.CosmosDB
{
    public class CosmosDBRepositorySettings
    {
        public string DatabaseName { get; set; }
        public string ContainerName { get; set; }
        public Assembly EventAssembly { get; set; }
    }

    public class CosmosDBProjectionRepositorySettings
    {
        public string DatabaseName { get; set; }
        public string ContainerName { get; set; }
    }
}
