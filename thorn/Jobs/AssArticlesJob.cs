using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;

namespace thorn.Jobs;

[DisallowConcurrentExecution]
public class AssArticlesJob : IJob
{
    public const int IntervalSeconds = 86400;

    private const string Endpoint = "https://apiv1.crom.avn.sh/graphql";
    private const string Query = """
        query assArticles {
          pages(filter: {
            url: { startsWith: "http://scp-cs.wikidot.com" }
            wikidotInfo: { rating: { lte: -3 } }
          }) {
            edges { node { url wikidotInfo { title rating } } }
          }
        }
        """;
    
    private static readonly TimeSpan HighlightAfter = TimeSpan.FromHours(23.5);

    private readonly ILogger<AssArticlesJob> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly SocketTextChannel _channel;
    
    private readonly Dictionary<string, DateTimeOffset> _firstSeen = new();

    public AssArticlesJob(ILogger<AssArticlesJob> logger, IHttpClientFactory httpFactory, DiscordSocketClient client,
        IConfiguration config)
    {
        _logger = logger;
        _httpFactory = httpFactory;
        var channelId = ulong.Parse(config["channels:console-wiki"] ?? throw new Exception("console-wiki channel is not configured"),
            NumberStyles.Any);
        _channel = client.GetChannel(channelId) as SocketTextChannel;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        List<(string Url, string Title, int Rating)> pages;
        try
        {
            pages = await FetchPages();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to fetch low rated pages");
            return;
        }
        
        if (pages.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        var current = pages.Select(p => p.Url).ToHashSet();
        foreach (var url in _firstSeen.Keys.Where(u => !current.Contains(u)).ToList())
            _firstSeen.Remove(url);
        foreach (var url in current)
            _firstSeen.TryAdd(url, now);

        var lines = pages.OrderBy(p => p.Rating).Select(p =>
        {
            var old = now - _firstSeen[p.Url] >= HighlightAfter;
            var line = $"[{p.Title}]({p.Url}) ({p.Rating})";
            return old ? $"<:sumisplouchalik:939084788218859531> **{line}**" : line;
        });

        var description = new StringBuilder();
        foreach (var line in lines)
            description.AppendLine(line);

        var embed = new EmbedBuilder
        {
            Title = "ass články",
            Description = description.ToString(),
            Color = Color.Red,
        }.WithCurrentTimestamp().Build();

        await _channel.SendMessageAsync(embed: embed);
        _logger.LogInformation("Sent low rated list ({Count} pages)", pages.Count);
    }

    private async Task<List<(string, string, int)>> FetchPages()
    {
        var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(30);
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "thorn@scp-cs");

        var body = JsonConvert.SerializeObject(new { query = Query });
        var response = await http.PostAsync(Endpoint, new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        var edges = json["data"]?["pages"]?["edges"] as JArray ?? throw new Exception("Unexpected response shape");

        return edges
            .Select(e => e["node"])
            .Where(n => n?["wikidotInfo"]?.Type == JTokenType.Object)
            .Select(n => ((string)n!["url"]!, (string)n["wikidotInfo"]!["title"] ?? (string)n["url"]!,
                (int)n["wikidotInfo"]!["rating"]!))
            .Where(n => n.Item1 != "http://scp-cs.wikidot.com/rip-jozin-z-bazin") // lol
            .ToList();
    }
}
