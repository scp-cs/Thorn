using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Discord;
using Discord.WebSocket;
using Html2Markdown;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;
using thorn.Config;

namespace thorn.Jobs;

[DisallowConcurrentExecution] // there is shared state in this object
public partial class RssJob : IJob
{
    public const int DefaultDelaySeconds = 10800;
    public const string FeedIndexKey = "feedIndex";

    private readonly ILogger<ReminderJob> _logger;
    private readonly List<FeedConfig> _configs;
    private readonly Dictionary<ulong, SocketTextChannel> _channels;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<FeedConfig, DateTime?> _lastUpdates;

    public RssJob(ILogger<ReminderJob> logger, DiscordSocketClient client, IConfiguration configuration)
    {
        _logger = logger;
        _channels = new Dictionary<ulong, SocketTextChannel>();
        _lastUpdates = new Dictionary<FeedConfig, DateTime?>();
        _httpClient = BuildHttpClient(configuration["rssProxy"]);

        _configs = JsonConvert.DeserializeObject<List<FeedConfig>>(File.ReadAllText("Config/feeds.json"));

        foreach (var config in _configs)
        foreach (var channelId in config.ChannelIds)
        {
            if (_channels.ContainsKey(channelId)) continue;
            _channels.Add(channelId, client.GetChannel(channelId) as SocketTextChannel);
        }

        foreach (var feedConfig in _configs)
            _lastUpdates.Add(feedConfig, null);
    }

    private HttpClient BuildHttpClient(string proxy)
    {
        if (string.IsNullOrWhiteSpace(proxy))
        {
            _logger.LogInformation("RSS proxy not configured");
            return new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        _logger.LogInformation("RSS routed through proxy {Proxy}", proxy);
        var handler = new SocketsHttpHandler
        {
            Proxy = new WebProxy(proxy),
            UseProxy = true
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var index = context.MergedJobDataMap.GetInt(FeedIndexKey);
        if (index < 0 || index >= _configs.Count)
        {
            _logger.LogError("RssJob fired with out-of-range feed index {Index}", index);
            return;
        }

        var config = _configs[index];
        var newItems = await GetNewItems(config);
        if (newItems is null) return;

        foreach (var feedItem in newItems)
        foreach (var channelId in config.ChannelIds)
        {
            var channel = _channels[channelId];
            if (channel is null) continue;

            await channel.SendMessageAsync(embed: GetEmbed(feedItem, config));
            _logger.LogInformation("Sent RSS feed '{Title}' to #{Channel}", feedItem.Title, channel);
        }
    }

    private async Task<List<FeedItem>> GetNewItems(FeedConfig feedConfig)
    {
        Feed feed;
        try
        {
            var content = await _httpClient.GetStringAsync(feedConfig.Link);
            feed = FeedReader.ReadFromString(content);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to fetch RSS feed {Link}", feedConfig.Link);
            return null;
        }

        var lastUpdate = _lastUpdates[feedConfig];

        if (lastUpdate is null)
        {
            _lastUpdates[feedConfig] = feed.LastUpdatedDate;
            return null;
        }

        // If latest item is older than latest stored item, continue
        if (lastUpdate > feed.LastUpdatedDate) return null;

        _lastUpdates[feedConfig] = feed.LastUpdatedDate;

        var newItems = feed.Items.Where(x => x.PublishingDate > lastUpdate);

        if (feedConfig.Filter is not null)
            newItems = newItems.Where(x => feedConfig.Filter.Any(f => x.Title.Contains(f)));

        if (feedConfig.FilterIgnore is not null)
            newItems = newItems.Where(x => !feedConfig.FilterIgnore.Any(f =>
                x.Title.Contains(f) || (x.Link?.Contains(f) ?? false)));

        return newItems.ToList();
    }

    private Embed GetEmbed(FeedItem feedItem, FeedConfig feedConfig)
    {
        var description = new Converter().Convert(feedItem.Description)
            .Replace("<span class=\"printuser\">", "").Replace("</span>", "");

        if (feedConfig.NewPageAnnouncement)
            return GetAnnouncementEmbed(feedItem, feedConfig, description);

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
            Description = string.IsNullOrEmpty(feedConfig.CustomDescription)
                ? description
                : feedConfig.CustomDescription,
            Color = feedConfig.EmbedColor == 0 ? Color.Blue : new Color(feedConfig.EmbedColor),
            // Adding one hour here cuz timezones
            Footer = new EmbedFooterBuilder().WithText(feedItem.PublishingDate?.AddHours(1)
                .ToString(CultureInfo.InvariantCulture))
        }.Build();
    }

    private Embed GetAnnouncementEmbed(FeedItem feedItem, FeedConfig feedConfig, string text)
    {
        var title = GetTitle(feedItem.Title);
        var author = GetUsername(text);
        var description = new StringBuilder("Nový článek na wiki! Yay! \\o/\n");

        if (author is not null)
            description.Append($"[{title}]({feedItem.Link}) od uživatele `{author}`");
        else
            description.Append($"[{title}]({feedItem.Link}) od kdoví koho :/");
        
        return new EmbedBuilder
        {
            Title = title,
            Description = description.ToString(),
            Color = feedConfig.EmbedColor == 0 ? Color.Blue : new Color(feedConfig.EmbedColor),
            // Adding one hour here cuz timezones
            Footer = new EmbedFooterBuilder().WithText(feedItem.PublishingDate?.AddHours(1)
                .ToString(CultureInfo.InvariantCulture))
        }.Build();
    }

    private string GetTitle(string source) => TitleRegex().Match(source).Groups[1].Value;
    private string GetUsername(string source) => UsernameRegex().Match(source).Groups[1].Value;

    [GeneratedRegex("user:info\\/([^)]*)")]
    private static partial Regex UsernameRegex();
    
    [GeneratedRegex("\"(.*)\" - .*")]
    private static partial Regex TitleRegex();
}