using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
            }
            try
            {
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
                    
                    responseMessage = JsonConvert.SerializeObject(result);
                    log.LogInformation(responseMessage);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
            return new OkObjectResult(responseMessage);
        }
    }
}
