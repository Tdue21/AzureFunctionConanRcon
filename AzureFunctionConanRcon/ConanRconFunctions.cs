using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AzureFunctionConanRcon;

public static class ConanRconFunctions
{
#if DEBUG
    private const string TimerExpression = "*/30 * * * * *";
#else
        private const string TimerExpression = "0 55 5 * * *";
#endif

    [FunctionName("conan-timer")]
    public static async Task TimerFunction([TimerTrigger(TimerExpression, RunOnStartup = false, UseMonitor = true)] TimerInfo timerInfo, ILogger log)
    {
        log.LogInformation($"Executing timer function. Next={timerInfo?.Schedule?.GetNextOccurrence(DateTime.Now)}.");

        var host = GetEnvironmentVariable("host", "203.17.245.244");
        var port = GetEnvironmentVariable("port", 27056);
        var password = GetEnvironmentVariable("pwd", "o435c");
        var command = GetEnvironmentVariable("rconcmd", "listplayers");

        try
        {
            var result = await ExecuteCommand(host, port, password, command);
            log.LogInformation($"[{DateTime.Now}] {result}");
        }
        catch (Exception e)
        {
            log.LogError(e, "An error occurred.");
        }
    }

    // ?host=203.17.245.244&port=27056&pwd=o435c&command=help
    [FunctionName("conan-rcon")]
    public static async Task<IActionResult> RconCommand([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation("Executing command function");

        var host = req.Query["host"].ToString();
        if (string.IsNullOrWhiteSpace(host))
        {
            host = GetEnvironmentVariable("host", "203.17.245.244");
        }

        var port = req.Query["port"].ToString();
        if (string.IsNullOrWhiteSpace(port))
        {
            port = GetEnvironmentVariable("port", "27056");
        }

        var password = req.Query["pwd"].ToString();
        if (string.IsNullOrWhiteSpace(password))
        {
            password = GetEnvironmentVariable("pwd", "o435c");
        }
        var command = req.Query["command"].ToString();
        if (string.IsNullOrWhiteSpace(command))
        {
            command = GetEnvironmentVariable("rconcmd", "listplayers");
        }

        try
        {
            var portNo = int.TryParse(port, out var tmpPort) ? tmpPort : 27056;
            var result = await ExecuteCommand(host, portNo, password, command);
            log.LogInformation($"[{DateTime.Now}] {result}");
            return new OkObjectResult($"[{DateTime.Now}]\n\n{result}");
        }
        catch (Exception e)
        {
            log.LogError(e, "An error occurred.");
        }
        return new BadRequestResult();
    }

    private static async Task<string> ExecuteCommand(string host, int port, string password, string command)
    {
        using var client = new Rcon.RconClient();

        if (await client.ConnectAsync(host, port) && await client.AuthenticateAsync(password))
        {
            return await client.SendCommandAsync(command);
        }
        return null;
    }

    private static T GetEnvironmentVariable<T>(string name, T defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return defaultValue;
        }

        var result = Environment.GetEnvironmentVariable(name);
        if (result == null)
        {
            return defaultValue;
        }

        return (T)Convert.ChangeType(result, typeof(T));
    }
}

