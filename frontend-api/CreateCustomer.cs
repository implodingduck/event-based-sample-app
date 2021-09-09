using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace frontend_api
{
    public static class CreateCustomer
    {
        [FunctionName("CreateCustomer")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer")] HttpRequest req,
            [ServiceBus("customerqueue", Connection = "CustomerServiceBus")] IAsyncCollector<string> output,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string firstName = data?.firstName;
            string lastName = data?.lastName;
            string email = data?.email;

            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(firstName) ) 
            {
                errors.Add("First name is required");
            }
            if (string.IsNullOrEmpty(lastName) ) 
            {
                errors.Add("Last name is required");
            }
            if (string.IsNullOrEmpty(email) ) 
            {
                errors.Add("Email is required");
            }
            if (errors.Count > 0){
                return new BadRequestObjectResult(errors);
            }
            JObject json = JObject.FromObject(new
            {
                firstName = firstName,
                lastName = lastName,
                email = email
            });
            await output.AddAsync(json.ToString());
            
            var result = new ObjectResult(json.ToString());
            result.StatusCode = 200;
            return result;
        }
    }
}
