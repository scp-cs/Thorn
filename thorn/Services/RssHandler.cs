using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Html2Markdown;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using thorn.Config;
using thorn.UserAccounts;

namespace thorn.Services
{
    public class RssHandler : InitializedService
    {
        private readonly DiscordSocketClient _client;
        private readonly PairsService _pairs;
        private readonly ILogger<ReactionHandler> _logger;
        private readonly UserAccountsService _accounts;

        private readonly List<FeedConfig> _feedConfigs;
        private Timer _timer;
        private Dictionary<string, DateTime?> _lastUpdates;

        public RssHandler(DiscordSocketClient client, PairsService pairs, IConfiguration config,
            UserAccountsService accounts, ILogger<ReactionHandler> logger)
        {
            _client = client;
            _pairs = pairs;
            _logger = logger;
            _accounts = accounts;

            // _feedConfigs = config.GetValue<List<FeedConfig>>("feeds");
            
            // TODO: Do not hardcode this
            _feedConfigs = new List<FeedConfig>()
            {
                new FeedConfig
                {
                    Link =
                        "http://scp-cs.wikidot.com/feed/pages/pagename/most-recently-created/category/_default/tags/-admin/rating/%3E%3D-15/order/created_at+desc/limit/30/t/Most+Recently+Created",
                    ChannelId = 640917453169229856,
                    CustomDescription = "Nový článek od uživatele {0}!",
                    RequireAuth = false
                },
                new FeedConfig
                {
                    Link = "http://scp-cs.wikidot.com/feed/site-changes.xml",
                    ChannelId = 640917453169229856,
                    EmbedColor = 11454159,
                    RequireAuth = false
                }
            };
            
            _lastUpdates = new Dictionary<string, DateTime?>();
            foreach (var feedConfig in _feedConfigs)
                _lastUpdates.Add(feedConfig.Link, null);
        }

        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(async _ => await CheckFeeds(), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(2));
        }

        private async Task CheckFeeds()
        {
            foreach (var feedConfig in _feedConfigs)
            {
                var newItems = await GetNewItems(feedConfig);
                if (newItems == null) continue;

                var channel = _client.GetChannel(feedConfig.ChannelId) as SocketTextChannel;
                if (channel is null) continue;

                foreach (var feedItem in newItems)
                    await channel.SendMessageAsync(embed: GetEmbed(feedItem, feedConfig));
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

            // If latest new item is older than latest stored item, continue
            if (lastUpdate > feed.LastUpdatedDate) return null;
            
            // TODO: log
            _lastUpdates[feedConfig.Link] = feed.LastUpdatedDate;
            return feed.Items.Where(x => x.PublishingDate > lastUpdate).ToList();
        }

        private Embed GetEmbed(FeedItem feedItem, FeedConfig feedConfig)
        {
            // BUG: exits when null exception (no user with that username)
            // Just catch it and pass a placeholder "user unknown" or something
            // BUG: people with space in their name. Haven't tested this yet but I am pretty sure it won't work

            var account = _accounts.GetAccountByWikidot(GetUsername(feedItem.Content));

            // BUG: the <span> tag encapsulating the username remains, get rid of it
            // BUG: There are some weird spacing issues with some items, look into it
            var description = string.IsNullOrEmpty(feedConfig.CustomDescription)
                ? new Converter().Convert(feedItem.Description)
                : string.Format(feedConfig.CustomDescription, new object [] {account[AccountItem.WikidotUsername]});
            
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