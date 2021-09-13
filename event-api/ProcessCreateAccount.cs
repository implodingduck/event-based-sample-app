using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace event_api
{
    public static class ProcessCreateAccount
    {
        [FunctionName("ProcessCreateAccount")]
        public static void Run([ServiceBusTrigger("accountqueue", Connection = "CustomerServiceBus")]string myQueueItem, 
            [CosmosDB(
                databaseName: "%CosmosDBDatabase%",
                collectionName: "accounts",
                ConnectionStringSetting = "CosmosDBConnection")] out dynamic document,
            ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            dynamic data = JsonConvert.DeserializeObject(myQueueItem);
            document = new { 
                id = Guid.NewGuid(), 
                type = data.type, 
                balance = data.balance, 
                uid = data.uid,
                createdTimestamp = DateTime.UtcNow,
                lastUpdatedTimestamp = DateTime.UtcNow
            };

            log.LogInformation($"Hopefully {data.firstName} {data.lastName} is now in cosmos");
        }
    }
}