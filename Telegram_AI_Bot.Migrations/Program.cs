using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Migrations;

namespace Telegram_AI_Bot.Migrations;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Personal.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        new ServiceCollection().AddStarterKitMigrations(x => x
                .UseAssembly(typeof(Program).Assembly)
                .UseDefaultConnectionString(
                    configuration.GetConnectionString("Default"))
                .ConfigureRunner(runner => runner.AddSqlServer()))
            .BuildServiceProvider()
            .GetRequiredService<IStarterKitMigrator>()
            .Run(args);
    }
}