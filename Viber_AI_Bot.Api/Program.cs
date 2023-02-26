using CorrelationId;
using CorrelationId.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;
using Viber.Bot.NetCore.Middleware;
using Newtonsoft.Json;
using Quartz;
using Telegram_AI_Bot.Core;
using Telegram_AI_Bot.Core.Services.OpenAi;
using Telegram_AI_Bot.Infrastructure;
using Viber_AI_Bot.Api.QuartzJobs;

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

builder.Services.AddDefaultCorrelationId(options => options.UpdateTraceIdentifier = true);

builder.Services
    .AddViberDataAccess(builder.Configuration)
    .AddEvents(builder.Configuration, builder.Environment)
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


public class ViberBotConfiguration
{
    public static readonly string Configuration = "ViberBot";

    public string Webhook { get; set; } = "";
}


// curl -# -i -g -H "X-Viber-Auth-Token:TOKEN " -d @viber.json -X POST https://chatapi.viber.com/pa/set_webhook -v