using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram_AI_Bot.Api.Api.Services;
using Telegram_AI_Bot.Api.Services;
using Telegram_AI_Bot.Core;
using Telegram_AI_Bot.Infrastructure;

namespace Telegram_AI_Bot.Api;

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
				services.Configure<BotConfiguration>(
					context.Configuration.GetSection(BotConfiguration.Configuration));

				// Register named HttpClient to benefits from IHttpClientFactory
				// and consume it with ITelegramBotClient typed client.
				// More read:
				//  https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#typed-clients
				//  https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
				services.AddHttpClient("telegram_bot_client")
					.AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
					{
						BotConfiguration? botConfig = sp.GetConfiguration<BotConfiguration>();
						TelegramBotClientOptions options = new(botConfig.BotToken);
						var x = new TelegramBotClient(options, httpClient);
						x.DeleteWebhookAsync();
						return x;
					});

				services.AddScoped<UpdateHandler>();
				services.AddScoped<ReceiverService>();
				services.AddHostedService<PollingService>();

				services
					.AddDataAccess(context.Configuration)
					.AddCoreModule();
			});
	}
	
	public class BotConfiguration
#pragma warning restore RCS1110 // Declare type inside namespace.
#pragma warning restore CA1050 // Declare types in namespaces
	{
		public static readonly string Configuration = "BotConfiguration";

		public string BotToken { get; set; } = "";
	}
}


