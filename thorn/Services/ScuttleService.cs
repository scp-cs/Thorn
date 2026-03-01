#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace thorn.Services;

public record ScuttleUser(
    int Id,
    string Nickname,
    string Wikidot,
    string Discord,
    decimal Points,
    int TrCount,
    int CrCount,
    int OrigCount
);

public class ScuttleService(HttpClient httpClient, ILogger<ScuttleService> logger)
{
    private readonly JsonSerializerOptions _seriOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };
    private const string BaseUrl = "https://scuttle.scp-wiki.cz/api";

    public async Task<ScuttleUser?> SearchUser(string query)
    {
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/search/user?q={Uri.EscapeDataString(query)}");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Scuttle API returned status {StatusCode} for query {Query}", response.StatusCode, query);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ScuttleApiResponse>(content, _seriOptions);

            if (apiResponse?.Result == null || apiResponse.Result.Count == 0)
                return null;

            var user = apiResponse.Result[0];
            return new ScuttleUser(
                user.Id,
                user.Nickname,
                user.Wikidot,
                user.Discord,
                user.Points,
                user.TrCount,
                user.CrCount,
                user.OrigCount
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching for user {Query} in Scuttle", query);
            return null;
        }
    }

    private class ScuttleApiResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("result")]
        public List<ScuttleUserResponse>? Result { get; set; }
    }

    private class ScuttleUserResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; } = "";

        [JsonPropertyName("wikidot")]
        public string Wikidot { get; set; } = "";

        [JsonPropertyName("discord")]
        public string Discord { get; set; } = "";

        [JsonPropertyName("points")]
        public decimal Points { get; set; }

        [JsonPropertyName("tr_count")]
        public int TrCount { get; set; }

        [JsonPropertyName("cr_count")]
        public int CrCount { get; set; }

        [JsonPropertyName("orig_count")]
        public int OrigCount { get; set; }
    }
}
