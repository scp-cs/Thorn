using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace thorn.Services;

public class DayInfo
{
    [JsonPropertyName("header")] public string Header { get; set; } = "";
    [JsonPropertyName("events")] public Dictionary<string, List<string>> Events { get; set; } = new();
}

public class DailyService
{
    private readonly ILogger<DailyService> _logger;
    private readonly GitHubService _github;
    private readonly string _localPath;
    
    private Dictionary<string, DayInfo> _daily = new();
    private List<string> _greetings = new();

    public DailyService(ILogger<DailyService> logger, GitHubService github)
    {
        _logger = logger;
        _github = github;
        _localPath = Path.Combine(AppContext.BaseDirectory, "Config", "daily.json");

        LoadLocal();
    }

    public DayInfo GetDay(string key) => _daily.GetValueOrDefault(key);
    public IReadOnlyList<string> Greetings => _greetings;

    private void LoadLocal()
    {
        if (!File.Exists(_localPath))
        {
            _logger.LogWarning("Local daily.json not found at {Path}", _localPath);
            return;
        }

        var content = File.ReadAllText(_localPath);
        ParseAndStore(content);
        _logger.LogInformation("Loaded {Count} days from local daily.json", _daily.Count);
    }

    public async Task SyncFromRemoteAsync()
    {
        var content = await _github.GetRemoteDailyJsonAsync();
        if (content == null)
        {
            _logger.LogWarning("Could not fetch remote daily.json");
            return;
        }

        await File.WriteAllTextAsync(_localPath, content);
        ParseAndStore(content);
        _logger.LogInformation("Synced {Count} days from remote", _daily.Count);
        _github.ResetRateLimits();
    }

    private void ParseAndStore(string content)
    {
        try
        {
            var root = JsonSerializer.Deserialize<DailyRoot>(content);
            if (root?.Daily != null)
                _daily = root.Daily;
            if (root?.Greetings != null)
                _greetings = root.Greetings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse daily.json");
        }
    }

    private class DailyRoot
    {
        [JsonPropertyName("daily")] public Dictionary<string, DayInfo> Daily { get; set; }
        [JsonPropertyName("greetings")] public List<string> Greetings { get; set; }
    }
}
