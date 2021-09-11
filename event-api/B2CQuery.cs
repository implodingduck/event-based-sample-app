using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Microsoft.Graph;
using Azure.Identity;

namespace event_api
{
    public static class B2CQuery
    {
        private static GraphServiceClient graphClient;
        [FunctionName("B2CQuery")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";
            log.LogInformation($"The before response message: {responseMessage}");
            if (graphClient == null)
            {                                
                string[] scopes = {"https://graph.microsoft.com/.default"};
                string tenantId = Environment.GetEnvironmentVariable("TenantId");
                string clientId = Environment.GetEnvironmentVariable("AppId");
                string clientSecret = Environment.GetEnvironmentVariable("AppSecret");
                ClientSecretCredential clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret); 

                graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            }
            try
            {
                log.LogInformation("attempting the call...");
                // Get user by object ID
                var result = await graphClient.Users[name]
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
                    string email = "";
                    foreach ( ObjectIdentity oi in result.Identities)
                    {
                        if (string.Equals("emailAddress", oi?.SignInType)){
                            email = oi?.IssuerAssignedId;
                        }
                    }
                    log.LogInformation(JsonConvert.SerializeObject(responseMessage));
                    var retval = new {
                        displayName = result?.DisplayName,
                        email = email
                    };
                    responseMessage = JsonConvert.SerializeObject(retval);
                    log.LogInformation(responseMessage);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                responseMessage = ex.Message;
            }
            return new OkObjectResult(responseMessage);
        }
    }
}
