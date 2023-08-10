using System.Globalization;
using System.Text;
using Askmethat.Aspnet.JsonLocalizer.Extensions;
using Askmethat.Aspnet.JsonLocalizer.JsonOptions;
using CorrelationId;
using CorrelationId.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram_AI_Bot;
using Telegram_AI_Bot.Api.Services;
using Telegram_AI_Bot.Core;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Services.Telegram.Payments;
using Telegram_AI_Bot.Core.Telegram.Options;
using Telegram_AI_Bot.Infrastructure;
using Viber.Bot.NetCore.Middleware;


var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddControllers();

builder.Configuration
    .AddJsonFile("appsettings.Personal.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.Configure<TelegramBotConfiguration>(
    builder.Configuration.GetSection(TelegramBotConfiguration.Configuration));
builder.Services.Configure<OpenAiConfiguration>(
    builder.Configuration.GetSection(OpenAiConfiguration.Configuration));
builder.Services.Configure<CommonConfiguration>(
    builder.Configuration.GetSection(CommonConfiguration.Configuration));
builder.Services.Configure<PaymentsConfiguration>(
    builder.Configuration.GetSection(PaymentsConfiguration.Configuration));

builder.Services.Configure<HostOptions>(hostOptions =>
{
    hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddViberBotApi(builder.Configuration);


builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = false;
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services
    .AddJsonLocalization(options =>
    {
        options.LocalizationMode = LocalizationMode.I18n;
        options.UseBaseName = false;
        options.IsAbsolutePath = false;
        options.CacheDuration = TimeSpan.FromMinutes(15);
        options.ResourcesPath = "Resources/";
        options.FileEncoding = Encoding.GetEncoding("UTF-8");
        options.SupportedCultureInfos = new HashSet<CultureInfo>()
        {
            new("en-US"),
            new("ru-RU")
        };
    })
    .AddDefaultCorrelationId(options => options.UpdateTraceIdentifier = true);

builder.Services.AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        TelegramBotConfiguration? botConfig = sp.GetConfiguration<TelegramBotConfiguration>();
        TelegramBotClientOptions options = new(botConfig.BotToken);
        var x = new TelegramBotClient(options, httpClient);
        x.DeleteWebhookAsync();
        return x;
    });

builder.Services
    .AddScoped<UpdateHandler>()
    .AddScoped<ReceiverService>()
    .AddHostedService<PollingService>();

builder.Services
    .AddDataAccess(builder.Configuration)
    .AddInfrastructureModule(builder.Configuration)
    .AddTelegramInfrastructureModule(builder.Configuration)
    .AddTelegramCoreModule();

var app = builder.Build();

// app.UseCorrelationId();

// app.MapControllers();

app.Run();
