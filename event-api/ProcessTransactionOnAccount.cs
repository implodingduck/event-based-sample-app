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
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
namespace event_api
{
    public static class ProcessTransactionOnAccount
    {
        [FunctionName("ProcessTransactionOnAccount")]
        public static async Task Run(
            [EventHubTrigger("transactions", ConsumerGroup="event-based-sample-app-accounts", Connection = "EventHubConnection")] EventData[] events, 
            [CosmosDB(
                databaseName: "%CosmosDBDatabase%",
                collectionName: "accounts",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
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
                    string databaseName = Environment.GetEnvironmentVariable("CosmosDBDatabase");
                    string collectionName = "accounts";

                    Uri documentUri = UriFactory.CreateDocumentUri(databaseName, collectionName, t.accountId);
                    Document account = await client.ReadDocumentAsync(documentUri);
                    log.LogInformation($"Account(pre): {JsonConvert.SerializeObject(account)}");
                    decimal newBalance = account.GetPropertyValue<decimal>("balance") + t.amount;
                    account.SetPropertyValue("balance", newBalance);
                    
                    await client.UpsertDocumentAsync(documentUri, account);
                    log.LogInformation($"Account(post): {JsonConvert.SerializeObject(account)}");
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
