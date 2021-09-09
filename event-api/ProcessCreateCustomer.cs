using System;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace event_api
{
    public static class ProcessCreateCustomer
    {
        [FunctionName("ProcessCreateCustomer")]
        public static void Run(
            [ServiceBusTrigger("customerqueue", Connection = "CustomerServiceBus")]string myQueueItem, 
            [CosmosDB(
                databaseName: "%CosmosDBDatabase%",
                collectionName: "%ComsosDBCollection%",
                ConnectionStringSetting = "CosmosDBConnection")] out dynamic document,
            ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            dynamic data = JsonConvert.DeserializeObject(myQueueItem);
            document = new { 
                id = data.guid, 
                firstName = data.firstName, 
                lastname = data.lastname, 
                email = data.email
            };

            log.LogInformation($"Hopefully {data.firstName} {data.lastName} is now in cosmos");
        }
    }
}
