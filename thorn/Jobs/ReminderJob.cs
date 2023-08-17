using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using thorn.Services;

namespace thorn.Jobs;

public class ReminderJob : IJob
{
    private readonly ILogger<ReminderJob> _logger;
    private readonly SocketTextChannel _channel;
    private readonly ConstantsService _constants;

    private readonly Dictionary<string, string> _daily;

    public ReminderJob(ILogger<ReminderJob> logger, DiscordSocketClient client, ConstantsService constants, IConfiguration config)
    {
        _logger = logger;
        _constants = constants;

        _channel = client.GetChannel(_constants.Channels["general"]) as SocketTextChannel;
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
            ThumbnailUrl =
                "https://cdn.discordapp.com/attachments/537064369725636611/733080455217283131/calendar-flat.png",
            Color = Color.Green
        }.Build();

        await _channel.SendMessageAsync(embed: embed);
        _logger.LogInformation("Sent daily reminder for {Day}", day.Date);
    }
}