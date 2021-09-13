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

namespace frontend_api
{
    public class Account 
    {
        public string Type { get; set; }
        public decimal Balance { get; set; }
        public string Uid { get; set; }

    }
    public static class CreateAccount
    {
        [FunctionName("CreateAccount")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "accounts")] HttpRequest req,
            [ServiceBus("accountqueue", Connection = "CustomerServiceBus")] IAsyncCollector<string> output,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Account account = ((JObject)JsonConvert.DeserializeObject(requestBody)).ToObject<Account>();
            await output.AddAsync(JsonConvert.SerializeObject(account));

            return new OkObjectResult(JsonConvert.SerializeObject(account));
        }
    }
}
