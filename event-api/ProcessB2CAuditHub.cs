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

using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
namespace event_api
{
    public static class ProcessB2CAuditHub
    {
        private static GraphServiceClient graphClient;
        [FunctionName("ProcessB2CAuditHub")]
        public static async Task Run(
            [EventHubTrigger("%EventHubName%", Connection = "EventHubConnection")] EventData[] events, 
            
            ILogger log)
        {
            var exceptions = new List<Exception>();
            log.LogInformation("A new event hub thing has been triggered");
            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    dynamic data = JsonConvert.DeserializeObject(messageBody);
                    JArray records = data.records;
                    foreach (JObject record in records)
                    {
                        if ( "Add User".Equals(record.GetValue("operationName"))){
                            log.LogInformation("Found an add user operation");
                            log.LogInformation($"C# Event Hub trigger function processed a message: {record.ToString()}");
                            if (graphClient == null)
                            {                                
                                // Initialize the client credential auth provider
                                IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                                    .Create(Environment.GetEnvironmentVariable("AppId"))
                                    .WithTenantId(Environment.GetEnvironmentVariable("TenantId"))
                                    .WithClientSecret(Environment.GetEnvironmentVariable("AppSecret"))
                                    .Build();
                                ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);

                                // Set up the Microsoft Graph service client with client credentials
                                graphClient = new GraphServiceClient(authProvider);
                                string userId = record?.GetValue("properties")?.GetValue("targetResources")[0].GetValue("id");
                                try
                                {
                                    // Get user by object ID
                                    var result = await graphClient.Users[userId]
                                        .Request()
                                        .Select(e => new
                                        {
                                            e.DisplayName,
                                            e.Id,
                                            e.Identities
                                        })
                                        .GetAsync();

                                    if (result != null)
                                    {
                                        log.LogInformation(JsonConvert.SerializeObject(result));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    log.LogError(ex.Message);
                                }
                            }
                        }
                    }
                    // Replace these two lines with your processing logic.
                    
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

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
