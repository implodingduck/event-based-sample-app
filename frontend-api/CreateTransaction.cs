using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace frontend_api
{
    public static class CreateTransaction
    {
        [FunctionName("CreateTransaction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "transactions")] HttpRequest req,
            [EventHub("%EventHubName%", Connection = "EventHubConnection")]IAsyncCollector<EventData> outputEvents,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Transaction transaction = ((JObject)JsonConvert.DeserializeObject(requestBody)).ToObject<Transaction>();
            transaction.id = Guid.NewGuid().ToString();
            transaction.creationTime = DateTime.UtcNow;
            var wrapper = new {
                transaction = transaction
            };
            string transactionString = JsonConvert.SerializeObject(wrapper);
            await outputEvents.AddAsync(new EventData(System.Text.Encoding.UTF8.GetBytes(transactionString)));
            return new OkObjectResult(transactionString);
        }
    }
}
