using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
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

        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddJsonFile("Config/config.json");
        builder.Configuration.AddJsonFile("Config/daily.json");

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();
        
        builder.Services.AddDiscordHost((config, _) =>
        {
            config.SocketConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 50,
                GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMessages |
                                 GatewayIntents.GuildMessageReactions
            };

            config.Token = builder.Configuration["token"]!;
            config.LogFormat = (message, _) => $"{message.Source}: {message.Message}";
        });
        
        builder.Services.AddInteractionService((config, _) =>
        {
            config.LogLevel = LogSeverity.Info;
            config.UseCompiledLambda = true;
        });
        
        builder.Services.AddHostedService<InteractionHandler>();
        builder.Services.AddHostedService<ReactionHandler>();
        
        builder.Services.AddSingleton<RssJob>();
        builder.Services.AddSingleton<ReminderJob>();

        builder.Services.AddQuartz(configure =>
        {
            var twoMinSchedule = CronScheduleBuilder.CronSchedule("0 */2 * ? * *");
            var rssJob = new JobKey(nameof(RssJob));
            configure.AddJob<RssJob>(rssJob)
                .AddTrigger(t => t.ForJob(rssJob).WithSchedule(twoMinSchedule));

            var dailySchedule = CronScheduleBuilder.CronSchedule("0 0 0 * * ?");
            var reminderJob = new JobKey(nameof(ReminderJob));
            configure.AddJob<ReminderJob>(reminderJob)
                .AddTrigger(t => t.ForJob(reminderJob).WithSchedule(dailySchedule));
        });
        builder.Services.AddQuartzHostedService();

        var host = builder.Build();

        await host.RunAsync();
    }
}