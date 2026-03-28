using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord.WebSocket;
using GitHubJwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;

namespace thorn.Services;

public class GitHubService
{
    private readonly ILogger<GitHubService> _logger;
    private readonly GitHubJwtFactory _jwtFactory;
    private readonly long _installationId;

    private const int RateLimit = 1;
    private readonly Dictionary<ulong, int> _rateLimits = new();

    private const string Owner = "scp-cs";
    private const string Repo = "Thorn";
    private const string Branch = "master";
    private const string DailyPath = "thorn/Config/daily.json";

    public bool IsRateLimited(ulong userId) => _rateLimits.GetValueOrDefault(userId, 0) >= RateLimit;

    public void ResetRateLimits() => _rateLimits.Clear();

    public GitHubService(ILogger<GitHubService> logger, IConfiguration config)
    {
        _logger = logger;

        var appId = int.Parse(config["githubAppId"] ?? "2797321");
        var keyPath = Path.Combine(AppContext.BaseDirectory, "Config", "github-app.pem");
        _installationId = long.Parse(config["githubInstallationId"] ?? throw new Exception("githubInstallationId not configured"));

        _jwtFactory = new GitHubJwtFactory(
            new FilePrivateKeySource(keyPath),
            new GitHubJwtFactoryOptions { AppIntegrationId = appId, ExpirationSeconds = 600 }
        );
    }

    private async Task<GitHubClient> GetAuthenticatedClientAsync()
    {
        var jwt = _jwtFactory.CreateEncodedJwtToken();
        var appClient = new GitHubClient(new ProductHeaderValue("Thorn"))
        {
            Credentials = new Credentials(jwt, AuthenticationType.Bearer)
        };

        var token = await appClient.GitHubApps.CreateInstallationToken(_installationId);
        return new GitHubClient(new ProductHeaderValue("Thorn"))
        {
            Credentials = new Credentials(token.Token)
        };
    }

    public async Task<string> GetRemoteDailyJsonAsync()
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var contents = await client.Repository.Content.GetAllContentsByRef(Owner, Repo, DailyPath, Branch);
            return contents[0].Content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get remote daily.json");
            return null;
        }
    }

    public async Task<string> CreateProposalPrAsync(string date, string year, string eventText, SocketUser author)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();

            var contents = await client.Repository.Content.GetAllContentsByRef(Owner, Repo, DailyPath, Branch);
            var currentContent = contents[0].Content;
            var currentSha = contents[0].Sha;

            var daily = JsonSerializer.Deserialize<DailyRoot>(currentContent);
            if (daily?.Daily == null) return null;

            if (!daily.Daily.TryGetValue(date, out var dayInfo))
            {
                dayInfo = new DailyEntry { Header = "", Events = new() };
                daily.Daily[date] = dayInfo;
            }

            if (!dayInfo.Events.TryGetValue(year, out var events))
            {
                events = [];
                dayInfo.Events[year] = events;
            }

            events.Add(eventText);

            var newContent = JsonSerializer.Serialize(daily, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            var branchName = $"daily-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var baseRef = await client.Git.Reference.Get(Owner, Repo, $"heads/{Branch}");
            await client.Git.Reference.Create(Owner, Repo, new NewReference($"refs/heads/{branchName}", baseRef.Object.Sha));

            var formattedDate = date.Replace(" ", ". ") + ".";

            await client.Repository.Content.UpdateFile(Owner, Repo, DailyPath,
                new UpdateFileRequest($"add new daily entry {formattedDate} {year} by @{author}", newContent, currentSha, branchName));

            var pr = await client.PullRequest.Create(Owner, Repo, new NewPullRequest(
                $"[Daily] {formattedDate} {year} od @{author}",
                branchName,
                Branch)
            {
                Body = $"**Datum:** {formattedDate} {year}\n**Událost:** {eventText}\nNavrženo uživatelem `@{author}`",
            });

            await client.Issue.Labels.AddToIssue(Owner, Repo, pr.Number, ["daily"]);

            _logger.LogInformation("Created PR #{Number} for daily entry {Date}/{Year}", pr.Number, date, year);
            _rateLimits[author.Id] = _rateLimits.GetValueOrDefault(author.Id, 0) + 1;

            return pr.HtmlUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create proposal PR");
            return null;
        }
    }

    private class DailyRoot
    {
        [JsonPropertyName("daily")] public Dictionary<string, DailyEntry> Daily { get; set; }
    }

    private class DailyEntry
    {
        [JsonPropertyName("header")] public string Header { get; set; } = "";
        [JsonPropertyName("events")] public Dictionary<string, List<string>> Events { get; set; } = new();
    }
}
