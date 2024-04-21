using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace thorn.Jobs;

public class ReminderJob : IJob
{
    private readonly ILogger<ReminderJob> _logger;
    private readonly SocketTextChannel _channel;

    private readonly Dictionary<string, string> _daily;

    public ReminderJob(ILogger<ReminderJob> logger, DiscordSocketClient client, IConfiguration config)
    {
        _logger = logger;
        var generalId = ulong.Parse(config["channels:general"] ?? throw new Exception("No general channel configured"), NumberStyles.Any);

        _channel = client.GetChannel(generalId) as SocketTextChannel;
        _daily = config.GetSection("daily").Get<Dictionary<string, string>>();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var day = DateTime.Now;
        var description = _daily[day.ToString("dd MM")] +
                          $"\n\nPřeji hezký den ❤️";

        var embed = new EmbedBuilder
        {
            Title = "Krásné dobré ráno!",
            Description = description,
            Color = Color.Green
        }.Build();

        await _channel.SendMessageAsync(embed: embed);
        _logger.LogInformation("Sent daily reminder for {Day}", day.Date);
    }
}