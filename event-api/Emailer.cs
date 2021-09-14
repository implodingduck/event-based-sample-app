// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace event_api
{
    public static class Emailer
    {
        private static HttpClient httpClient = new HttpClient();
        [FunctionName("Emailer")]
        public static async Task Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation(eventGridEvent.Data.ToString());
            var json = JsonConvert.SerializeObject(eventGridEvent);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            string url = Environment.GetEnvironmentVariable("LogicAppUrl");
            var response = await httpClient.PostAsync(url, data);

        }
    }
}
