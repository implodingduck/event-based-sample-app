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
using Azure.Identity;
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
                        string operationName = (string)record?["operationName"];
                        log.LogInformation($"OperationName: {operationName}");
                        if ( string.Equals("Add user", operationName, StringComparison.OrdinalIgnoreCase)){
                            log.LogInformation("Found an add user operation");
                            log.LogInformation($"C# Event Hub trigger function processed a message: {record.ToString()}");
                            if (graphClient == null)
                            {                                
                                string[] scopes = {"https://graph.microsoft.com/.default"};
                                string tenantId = Environment.GetEnvironmentVariable("TenantId");
                                string clientId = Environment.GetEnvironmentVariable("AppId");
                                string clientSecret = Environment.GetEnvironmentVariable("AppSecret");
                                ClientSecretCredential clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret); 

                                graphClient = new GraphServiceClient(clientSecretCredential, scopes);
                            }
                            JObject properties = (JObject)record["properties"];
                            log.LogInformation($"properties = {properties.ToString()}");
                            JArray targetResources = (JArray)properties["targetResources"];
                            log.LogInformation($"targetResources = {targetResources.ToString()}");
                            JObject targetResource = (JObject)targetResources[0];
                            log.LogInformation($"targetResource = {targetResource.ToString()}");
                            string userId = (string)targetResource["id"];
                            log.LogInformation($"userId = {targetResources.ToString()}");
                            try
                            {
                                // Get user by object ID
                                var result = await graphClient.Users[userId]
                                    .Request()
                                    .Select(e => new
                                    {
                                        e.DisplayName,
                                        e.Id,
                                        e.Identities,
                                        e.GivenName,
                                        e.Surname
                                    })
                                    .GetAsync();

                                if (result != null)
                                {
                                    log.LogInformation(JsonConvert.SerializeObject(result));
                                    string email = "";
                                    foreach ( ObjectIdentity oi in result.Identities)
                                    {
                                        if (string.Equals("emailAddress", oi?.SignInType)){
                                            email = oi?.IssuerAssignedId;
                                        }
                                    }
                                    var retval = new {
                                        displayName = result?.DisplayName,
                                        email = email,
                                        firstName = result?.GivenName,
                                        lastName =  result?.Surname,
                                    };
                                    
                                    log.LogInformation(JsonConvert.SerializeObject(retval));
                                }
                            }
                            catch (Exception ex)
                            {
                                log.LogError(ex.Message);
                            }
                        }
                    }
                    
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
