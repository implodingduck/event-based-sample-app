using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace event_api
{
    public static class ProcessCreateTransaction
    {
        [FunctionName("ProcessCreateTransaction")]
        public static async Task Run(
            [EventHubTrigger("transactions", ConsumerGroup="event-based-sample-app", Connection = "EventHubConnection")] EventData[] events, 
            [CosmosDB(
                databaseName: "%CosmosDBDatabase%",
                collectionName: "transactions",
                ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<object> output,
            ILogger log)
        {
            log.LogInformation("ProcessCreateTransaction");
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");
                    TransactionWrapper tw = ((JObject)JsonConvert.DeserializeObject(messageBody)).ToObject<TransactionWrapper>();
                    Transaction t = tw.transaction;
                    t.completionTime = DateTime.UtcNow;
                    await output.AddAsync(t);
                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 0)
            {
                foreach( Exception e in exceptions)
                {
                    log.LogError(e.Message);
                    if(exceptions.Count == 1)
                    {
                        throw e;
                    }
                }
                throw new AggregateException(exceptions);
            }
        }
    }
}
