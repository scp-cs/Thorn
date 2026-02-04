using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using thorn.Services;

namespace thorn.Jobs;

public class DailyJob : IJob
{
    private readonly ILogger<DailyJob> _logger;
    private readonly SocketTextChannel _channel;
    private readonly DailyService _dailyService;
    private readonly Random _random;

    public DailyJob(ILogger<DailyJob> logger, DiscordSocketClient client, IConfiguration config, DailyService dailyService)
    {
        _logger = logger;
        _dailyService = dailyService;
        var generalId = ulong.Parse(config["channels:general"] ?? throw new Exception("No general channel configured"), NumberStyles.Any);

        _channel = client.GetChannel(generalId) as SocketTextChannel;
        _random = new Random();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var day = DateTime.Now;
        var dayInfo = _dailyService.GetDay(day.ToString("dd MM"));
        if (dayInfo == null)
        {
            _logger.LogWarning("No daily info for {Day}", day.ToString("dd MM"));
            return;
        }
        var description = new StringBuilder();
        
        if (!string.IsNullOrWhiteSpace(dayInfo.Header))
        {
            description.AppendLine(dayInfo.Header);
            description.AppendLine();
        }
        
        if (dayInfo.Events.Count > 0)
        {
            description.AppendLine("**Události:**");
            description.AppendLine();

            foreach (var (year, events) in dayInfo.Events)
                foreach (var evt in events)
                    description.AppendLine($"**{year}** – {evt}");
        }

        var dayString = description.ToString();
        const int maxLength = 4096 - 100;

        if (dayString.Length > maxLength)
        {
            var startSearch = dayString.Length - maxLength;
            var cutoffIndex = dayString.IndexOf('\n', startSearch);

            if (cutoffIndex >= 0 && cutoffIndex + 1 < dayString.Length)
                cutoffIndex++;
            else
                cutoffIndex = startSearch;

            description.Clear();
            description.Append("...bylo to delší, zkrátil jsem to, sorry :(\n");
            description.Append(dayString[cutoffIndex..]);
        }

        var greetings = _dailyService.Greetings;
        var closeoff = greetings.Count > 0
            ? greetings[_random.Next(greetings.Count)]
            : "";

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

        // todo: we actually want to do this a while *before* we send the daily embed
        await _dailyService.SyncFromRemoteAsync();
    }
}