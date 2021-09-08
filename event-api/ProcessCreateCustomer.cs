using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace event_api
{
    public static class ProcessCreateCustomer
    {
        [FunctionName("ProcessCreateCustomer")]
        public static void Run([ServiceBusTrigger("customerqueue", Connection = "CustomerServiceBus")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }
    }
}
