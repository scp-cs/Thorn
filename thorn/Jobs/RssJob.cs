using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Discord;
using Discord.WebSocket;
using Html2Markdown;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;
using thorn.Config;

namespace thorn.Jobs
{
    public class RssJob : IJob
    {
        private readonly ILogger<ReminderJob> _logger;
        private readonly List<FeedConfig> _configs;
        private readonly Dictionary<ulong, SocketTextChannel> _channels;
        private readonly WebClient _webClient;
        private Dictionary<string, DateTime?> _lastUpdates;

        public RssJob(ILogger<ReminderJob> logger, DiscordSocketClient client)
        {
            _logger = logger;
            _channels = new Dictionary<ulong, SocketTextChannel>();
            _lastUpdates = new Dictionary<string, DateTime?>();
            _webClient = new WebClient();

            _configs = JsonConvert.DeserializeObject<List<FeedConfig>>(File.ReadAllText("Config/feeds.json"));

            foreach (var config in _configs)
            foreach (var channelId in config.ChannelIds)
            {
                if (_channels.ContainsKey(channelId)) continue;
                _channels.Add(channelId, client.GetChannel(channelId) as SocketTextChannel);
            }

            foreach (var feedConfig in _configs)
                _lastUpdates.Add(feedConfig.Link, null);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            foreach (var config in _configs)
            {
                var newItems = await GetNewItems(config);
                if (newItems is null) continue;

                foreach (var feedItem in newItems)
                foreach (var channelId in config.ChannelIds)
                {
                    var channel = _channels[channelId];
                    if (channel is null) continue;
                    
                    await channel.SendMessageAsync(embed: GetEmbed(feedItem, config));
                    _logger.LogInformation("Sent RSS feed '{Title}' to #{Channel}", feedItem.Title, channel);
                }
            }
        }
        
        private async Task<List<FeedItem>> GetNewItems(FeedConfig feedConfig)
        {
            Feed feed;
            
            if (!feedConfig.RequireAuth)
                feed = await FeedReader.ReadAsync(feedConfig.Link);
            else
            {
                // Basic HTTP auth
                _webClient.Credentials = new NetworkCredential(feedConfig.Username, feedConfig.Password);
                var response = _webClient.DownloadString(feedConfig.Link);
                feed = FeedReader.ReadFromString(response);
            }
            
            var lastUpdate = _lastUpdates[feedConfig.Link];

            if (lastUpdate is null)
            {
                _lastUpdates[feedConfig.Link] = feed.LastUpdatedDate;
                return null;
            }

            // If latest item is older than latest stored item, continue
            if (lastUpdate > feed.LastUpdatedDate) return null;
            
            _lastUpdates[feedConfig.Link] = feed.LastUpdatedDate;
            
            return feedConfig.Filter is null 
                ? feed.Items.Where(x => x.PublishingDate > lastUpdate).ToList() 
                : feed.Items.Where(x => x.PublishingDate > lastUpdate && feedConfig.Filter.Any(x.Title.Contains)).ToList();
        }
        
        private Embed GetEmbed(FeedItem feedItem, FeedConfig feedConfig)
        {
            // TODO: Get UserAccount and link to appropriate pages, these bugs relate to that: 
            // - Exits when null exception (no user with that username):
            //      - Just catch it and pass a placeholder "user unknown" or something
            // - People with space in their name. Haven't tested this yet but I am pretty sure it won't work
            
            var description = new Converter().Convert(feedItem.Description)
                .Replace("<span class=\"printuser\">", "").Replace("</span>", "");
            
            // There is a bug in the Html2Markdown library that inserts about 10 newlines instead of like 2 so this has to be in place
            // (not needed cuz I got rid of the whole section lol)
            // description = Regex.Replace(description, @"\n{4,}", "\n\n\n");

            // Remove two redundant lines
            var split = description.Split("\n").ToList();
            split.RemoveRange(2, 2);
            
            // And remove the preview
            split.RemoveRange(4, split.Count - 4);
            if (split.Last() == "\n") split.RemoveAt(split.Count - 1);
            description = string.Join("\n", split);

            return new EmbedBuilder
            {
                Title = feedItem.Title,
                Description = string.IsNullOrEmpty(feedConfig.CustomDescription) ? description : feedConfig.CustomDescription,
                Color = feedConfig.EmbedColor == 0 ? Color.Blue : new Color(feedConfig.EmbedColor),
                // Adding one hour here cuz timezones
                Footer = new EmbedFooterBuilder().WithText(feedItem.PublishingDate?.AddHours(1).ToString(CultureInfo.InvariantCulture))
            }.Build();
        }

        private string GetUsername(string source) => Regex.Match(source, "user:info\\/([^\"]*)").Groups[1].Value;
    }
}