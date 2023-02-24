using Microsoft.AspNetCore.Http.Json;
using Viber.Bot.NetCore.Middleware;
using Newtonsoft.Json;
using Telegram_AI_Bot.Core;
using Telegram_AI_Bot.Core.Services.OpenAi;
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

builder.Services.AddViberBotApi(builder.Configuration);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = false;
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services
    .AddViberDataAccess(builder.Configuration)
    .AddViberCoreModule();

var app = builder.Build();

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