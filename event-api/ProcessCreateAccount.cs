using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
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
            [EventGrid(TopicEndpointUri = "EventGridTopicUri", TopicKeySetting = "EventGridTopicKey")]out EventGridEvent outputEvent,
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
            outputEvent = new EventGridEvent(Guid.NewGuid().ToString(), "/Events/Accounts/New", JObject.FromObject(document), "Custom.Account", DateTime.UtcNow, "1.0");

        }
    }
}
