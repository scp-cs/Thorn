using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Serilog;
using Serilog.Events;
using thorn.Jobs;
using thorn.Services;

namespace thorn;

internal static class Program
{
    private static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .WriteTo.Console()
            .WriteTo.File("log/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(x =>
            {
                x.AddJsonFile("Config/config.json");
                x.AddJsonFile("Config/daily.json");
                x.Build();
            })
            .UseSerilog()
            .ConfigureDiscordHost((context, config) =>
            {
                config.SocketConfig = new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    DefaultRetryMode = RetryMode.RetryRatelimit,
                    MessageCacheSize = 50,
                    LogLevel = LogSeverity.Info,
                    GatewayIntents = GatewayIntents.All,
                };

                config.Token = context.Configuration["token"];
                config.LogFormat = (message, _) => $"{message.Source}: {message.Message}";
            })
            .UseCommandService((_, config) => { config.LogLevel = LogSeverity.Info; })
            .ConfigureServices((_, collection) =>
            {
                collection.AddHostedService<CommandHandler>();
                collection.AddHostedService<ReactionHandler>();
                collection.AddHostedService<QuartzHostedService>();

                collection.AddSingleton<ConstantsService>();

                collection.AddSingleton<IJobFactory, SingletonJobFactory>();
                collection.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

                collection.AddSingleton<ReminderJob>();
                collection.AddSingleton(new JobSchedule(
                    typeof(ReminderJob),
                    "0 0 0 * * ?")); // Every day at midnight

                collection.AddSingleton<RssJob>();
                collection.AddSingleton(new JobSchedule(
                    typeof(RssJob),
                    "0 0/2 * * * ?")); // Every two minutes
            });

        await hostBuilder.RunConsoleAsync();
    }
}