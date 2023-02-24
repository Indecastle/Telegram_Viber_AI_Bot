using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram_AI_Bot.Core;
using Telegram_AI_Bot.Infrastructure;
using Viber.Bot.NetCore.Middleware;

namespace Viber_AI_Bot.Api;


public class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(x => x.AddJsonFile("appsettings.Personal.json", optional: true))
            .ConfigureServices((context, services) =>
            {
                // Register Bot configuration
                services.Configure<ViberBotConfiguration>(
                    context.Configuration.GetSection(ViberBotConfiguration.Configuration));
                services.AddControllers();
                services.AddViberBotApi(context.Configuration);

                services
                    .AddDataAccess(context.Configuration)
                    .AddCoreModule();
            });
    }
	
    public class ViberBotConfiguration
    {
        public static readonly string Configuration = "ViberBot";

        public string Webhook { get; set; } = "";
    }
}