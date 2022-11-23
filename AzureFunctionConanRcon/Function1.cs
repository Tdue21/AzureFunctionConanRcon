using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionConanRcon
{
    public static class Function1
    {
        [FunctionName("conan-rcon")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("Executing function");
            string host = req.Query["host"];
            var port = int.TryParse(req.Query["port"].ToString(), out var portNo) ? portNo : 25056;
            string password = req.Query["pwd"];
            string command = req.Query["command"];

            using (var client = new Rcon.RconClient())
            {
                if (await client.ConnectAsync(host, port) && await client.AuthenticateAsync(password))
                {
                    var result = await client.SendCommandAsync(command);
                    return new OkObjectResult(result);
                }
            }

            return new BadRequestResult();
        }

        //public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        //{
        //    log.LogInformation("C# HTTP trigger function processed a request.");

        //    string name = req.Query["name"];

        //    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //    dynamic data = JsonConvert.DeserializeObject(requestBody);
        //    name = name ?? data?.name;

        //    var responseMessage = string.IsNullOrEmpty(name)
        //        ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
        //        : $"Hello, {name}. This HTTP triggered function executed successfully.";

        //    return new OkObjectResult(responseMessage);
        //}
    }
}
