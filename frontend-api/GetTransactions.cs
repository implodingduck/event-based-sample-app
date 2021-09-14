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

namespace frontend_api
{
    public static class GetTransactions
    {
        [FunctionName("GetTransactions")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "transactions/{accountId}")] HttpRequest req,
            [CosmosDB(
                databaseName: "%CosmosDBDatabase%",
                collectionName: "accounts",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "select * from c where c.accountId = {accountId}")]
                IEnumerable<Transaction> transactions,
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

            return new OkObjectResult(JsonConvert.SerializeObject(transactions));
        }
    }
}
