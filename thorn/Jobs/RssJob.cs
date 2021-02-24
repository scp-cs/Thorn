using System;
using System.Collections.Generic;
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
using Quartz.Impl;
using thorn.Config;

namespace thorn.Jobs
{
    public class RssJob : IJob
    {
        private readonly ILogger<ReminderJob> _logger;
        private readonly List<FeedConfig> _configs;
        private readonly Dictionary<ulong, SocketTextChannel> _channels;
        private Dictionary<string, DateTime?> _lastUpdates;

        private const int MaxDescriptionLength = 300;

        public RssJob(ILogger<ReminderJob> logger, DiscordSocketClient client)
        {
            _logger = logger;
            _channels = new Dictionary<ulong, SocketTextChannel>();
            _lastUpdates = new Dictionary<string, DateTime?>();

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
                var client = new WebClient {Credentials = new NetworkCredential(feedConfig.Username, feedConfig.Password)};
                var response = client.DownloadString(feedConfig.Link);
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
            
            // BUG: There are some weird spacing issues with some items, look into it
            var description = new Converter().Convert(feedItem.Description)
                .Replace("<span class=\"printuser\">", "").Replace("</span>", "");

            return new EmbedBuilder
            {
                Title = feedItem.Title,
                Description = string.IsNullOrEmpty(feedConfig.CustomDescription) ? description : feedConfig.CustomDescription,
                Color = feedConfig.EmbedColor == 0 ? Color.Blue : new Color(feedConfig.EmbedColor),
                Footer = new EmbedFooterBuilder().WithText(feedItem.PublishingDateString)
            }.Build();
        }

        private string GetUsername(string source) => Regex.Match(source, "user:info\\/([^\"]*)").Groups[1].Value;
    }
}