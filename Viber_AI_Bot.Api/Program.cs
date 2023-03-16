using System.Globalization;
using System.Text;
using Askmethat.Aspnet.JsonLocalizer.Extensions;
using Askmethat.Aspnet.JsonLocalizer.JsonOptions;
using CorrelationId;
using CorrelationId.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;
using Viber.Bot.NetCore.Middleware;
using Telegram_AI_Bot.Core;
using Telegram_AI_Bot.Core.Common;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Core.Viber.Options;
using Telegram_AI_Bot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Personal.json", optional: true);
// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ViberBotConfiguration>(
    builder.Configuration.GetSection(ViberBotConfiguration.Configuration));
builder.Services.Configure<OpenAiConfiguration>(
    builder.Configuration.GetSection(OpenAiConfiguration.Configuration));
builder.Services.Configure<CommonConfiguration>(
    builder.Configuration.GetSection(CommonConfiguration.Configuration));

builder.Services.AddViberBotApi(builder.Configuration);

// builder.Services.AddQuartz(q =>
// {
//     q.UseMicrosoftDependencyInjectionScopedJobFactory();
//     // Just use the name of your job that you created in the Jobs folder.
//     var jobKey = new JobKey("PostEndpointJob");
//     q.AddJob<PostEndpointJob>(opts => opts.WithIdentity(jobKey));
//     
//     q.AddTrigger(opts => opts
//         .ForJob(jobKey)
//         .WithIdentity("PostEndpointJob-trigger")
//     );
// });
// builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

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

builder.Services
    .AddViberDataAccess(builder.Configuration)
    .AddViberEvents(builder.Configuration, builder.Environment)
    .AddInfrastructureModule(builder.Configuration)
    .AddViberCoreModule();

var app = builder.Build();

app.UseCorrelationId();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


// curl -# -i -g -H "X-Viber-Auth-Token:TOKEN " -d @viber.json -X POST https://chatapi.viber.com/pa/set_webhook -v