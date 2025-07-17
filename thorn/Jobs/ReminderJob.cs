using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
    private readonly Random _random;

    private readonly Dictionary<string, string> _daily;

    public ReminderJob(ILogger<ReminderJob> logger, DiscordSocketClient client, IConfiguration config)
    {
        _logger = logger;
        var generalId = ulong.Parse(config["channels:general"] ?? throw new Exception("No general channel configured"), NumberStyles.Any);

        _channel = client.GetChannel(generalId) as SocketTextChannel;
        _daily = config.GetSection("daily").Get<Dictionary<string, string>>();
        _random = new Random();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var day = DateTime.Now;
        var dayString = _daily[day.ToString("dd MM")];
        var description = new StringBuilder();

        const int maxLength = 4096 - 100;

        if (dayString.Length > maxLength)
        {
            var startSearch = dayString.Length - maxLength;
            var cutoffIndex = dayString.IndexOf('\n', startSearch);

            if (cutoffIndex >= 0 && cutoffIndex + 1 < dayString.Length)
                cutoffIndex++;
            else
                cutoffIndex = startSearch;
            
            var trimmed = dayString[cutoffIndex..];
            description.Append("...bylo to delší, zkrátil jsem to, sorry :(\n");
            description.Append(trimmed);
        }
        else
        {
            description.Append(dayString);
        }
        
        var closeoff = _random.Next(7) switch
        {
            0 => "Přeji krásný nový den!",
            1 => "Mějte se famfárově!",
            2 => "Přeji hezký den \u2764\ufe0f",
            3 => "dobre amongus rano",
            4 => "zdravím všechny gejmrovce a gejmrovkyně",
            5 => "Máme to ale hezký počas, doufejme, že nám podrží.",
            6 => "Dobré jitro přátelé internetu \u2764\ufe0f",
            _ => "",
        };

        if (_random.Next(365) == 0)
            closeoff = "Špatné ráno \ud83d\ude21";
        
        description.Append("\n\n");
        description.Append(closeoff);

        var embed = new EmbedBuilder
        {
            Title = "Krásné dobré ráno!",
            Description = description.ToString(),
            Color = Color.Green
        }.Build();
        
        await _channel.SendMessageAsync(embed: embed);
        _logger.LogInformation("Sent daily reminder for {Day}", day.Date);
    }
}