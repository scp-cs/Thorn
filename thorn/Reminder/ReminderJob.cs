using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;
using thorn.Services;

namespace thorn.Reminder
{
    public class ReminderJob : IJob
    {
        private readonly ILogger<ReminderJob> _logger;
        private readonly SocketTextChannel _channel;
        private readonly PairsService _pairs;

        private readonly Dictionary<string, string> _daily;
        
        public ReminderJob(ILogger<ReminderJob> logger, DiscordSocketClient client, PairsService pairs, DataStorageService data)
        {
            _logger = logger;
            _pairs = pairs;

            _channel = client.GetChannel(ulong.Parse(pairs.GetString("GENERAL_CHANNEL_ID"))) as SocketTextChannel;
            _daily = DataStorageService.GetDictionary<string>("Config/daily.json");
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            var day = DateTime.Now;
            var description = _daily[day.ToString("dd MM")] +
                              $"\n\nPřeji krásný den {_pairs.GetString("AGRLOVE_EMOTE")}";

            var embed = new EmbedBuilder
            {
                Title = "Krásné dobré ráno!",
                Description = description,
                ThumbnailUrl = "https://cdn.discordapp.com/attachments/537064369725636611/733080455217283131/calendar-flat.png",
                Color = Color.Green
            }.Build();

            await _channel.SendMessageAsync(embed: embed);
            _logger.LogInformation("Sent daily reminder for {Day}", day.Date);
        }
    }
}