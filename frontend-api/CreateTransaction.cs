using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace frontend_api
{
    public static class CreateTransaction
    {
        [FunctionName("CreateTransaction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "transactions")] HttpRequest req,
            [EventHub("%EventHubName%", ConsumerGroup="event-based-sample-app", Connection = "EventHubConnection")]IAsyncCollector<string> outputEvents,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Transaction transaction = ((JObject)JsonConvert.DeserializeObject(requestBody)).ToObject<Transaction>();
            transaction.createdTimestamp = DateTime.UtcNow;

            string transactionString = JsonConvert.SerializeObject(transaction);
            outputEvents.AddAsync(transactionString);
            return new OkObjectResult(transactionString);
        }
    }
}
