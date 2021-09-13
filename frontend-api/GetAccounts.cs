using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Claims;

namespace frontend_api
{
    public static class GetAccounts
    {
        [FunctionName("GetAccounts")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts")] HttpRequest req,
            ClaimsPrincipal claimsPrincipal,
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
            try
            {
                List<string> retval = new List<string>();
                foreach (var identity in claimsPrincipal.Identities)
                {
                    log.LogInformation($"Identity {identity.Name}:");
                    retval.Add($"Identity {identity.Name}:");
                    log.LogInformation($"Auth type is {identity.AuthenticationType}");
                    retval.Add($"Auth type is {identity.AuthenticationType}");
                    foreach (var claim in identity.Claims)
                    {
                        log.LogInformation($"Claim '{claim.Type}' = '{claim.Value}'");
                        retval.Add($"Claim '{claim.Type}' = '{claim.Value}'");
                    }
                    retval.Add(JsonConvert.SerializeObject(identity.BootstrapContext));
                    retval.Add("----------------");
                }
                var newretval = new {
                    id = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier),
                    oldretval = retval
                };
                return new OkObjectResult(
                    JsonConvert.SerializeObject(newretval)
                );
            }
            catch (Exception ex) {
                return new BadRequestObjectResult(ex.Message);
            }
            
        }
    }
}
