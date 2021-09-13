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
using System.Security.Claims;
namespace frontend_api
{
    
    public static class CreateAccount
    {
        [FunctionName("CreateAccount")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "accounts")] HttpRequest req,
            [ServiceBus("accountqueue", Connection = "CustomerServiceBus")] IAsyncCollector<string> output,
            ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Account account = ((JObject)JsonConvert.DeserializeObject(requestBody)).ToObject<Account>();
            account.uid = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value;
            await output.AddAsync(JsonConvert.SerializeObject(account));

            return new OkObjectResult(JsonConvert.SerializeObject(account));
        }
    }
}
