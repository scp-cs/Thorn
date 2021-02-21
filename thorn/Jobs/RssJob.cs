using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Discord;
using Discord.WebSocket;
using Html2Markdown;
using Microsoft.Extensions.Logging;
using Quartz;
using thorn.Config;

namespace thorn.Jobs
{
    public class RssJob : IJob
    {
        private readonly ILogger<ReminderJob> _logger;
        private readonly List<FeedConfig> _configs;
        private readonly Dictionary<ulong, SocketTextChannel> _channels;
        private Dictionary<string, DateTime?> _lastUpdates;

        public RssJob(ILogger<ReminderJob> logger, DiscordSocketClient client)
        {
            _logger = logger;
            _channels = new Dictionary<ulong, SocketTextChannel>();
            _lastUpdates = new Dictionary<string, DateTime?>();

            // TODO: Place this in a config file of some sort
            // This will be later required anyway because of the password protected feeds
            _configs = new List<FeedConfig>()
            {
                // Newest articles
                new FeedConfig
                {
                    Link = "http://scp-cs.wikidot.com/feed/pages/pagename/most-recently-created/category/_default/tags/-admin/rating/%3E%3D-15/order/created_at+desc/limit/30/t/Most+Recently+Created",
                    ChannelIds = new ulong[] {640917453169229856},
                    EmbedColor = 16711680
                },
                // Latest changes
                new FeedConfig
                {
                    Link = "http://scp-cs.wikidot.com/feed/site-changes.xml",
                    ChannelIds = new ulong[] {640917453169229856},
                    EmbedColor = 16711680
                }
            };

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
            _logger.LogInformation("fired"); // TODO: Remove me in production
            
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
            // TODO: Implement password-protected feeds
            var feed = await FeedReader.ReadAsync(feedConfig.Link);
            var lastUpdate = _lastUpdates[feedConfig.Link];

            if (lastUpdate is null)
            {
                _lastUpdates[feedConfig.Link] = feed.LastUpdatedDate;
                return null;
            }

            // If latest item is older than latest stored item, continue
            if (lastUpdate > feed.LastUpdatedDate) return null;
            
            _lastUpdates[feedConfig.Link] = feed.LastUpdatedDate;
            return feed.Items.Where(x => x.PublishingDate > lastUpdate).ToList();
        }
        
        private Embed GetEmbed(FeedItem feedItem, FeedConfig feedConfig)
        {
            // TODO: Get UserAccount and link to appropriate pages, these bugs relate to that: 
            // Exits when null exception (no user with that username) :
            // Just catch it and pass a placeholder "user unknown" or something
            
            // People with space in their name. Haven't tested this yet but I am pretty sure it won't work

            // BUG: the <span> tag encapsulating the username remains, get rid of it
            // BUG: There are some weird spacing issues with some items, look into it
            // TODO: Maybe somehow limit length of the message?
            var description = new Converter().Convert(feedItem.Description);

            return new EmbedBuilder
            {
                Title = feedItem.Title,
                Description = description,
                Color = feedConfig.EmbedColor == 0 ? Color.Blue : new Color(feedConfig.EmbedColor),
                Footer = new EmbedFooterBuilder().WithText(feedItem.PublishingDateString)
            }.Build();
        }

        private string GetUsername(string source) => Regex.Match(source, "user:info\\/([^\"]*)").Groups[1].Value;
    }
}