using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace thorn.Services;

public class AdminLogService(DiscordSocketClient client, IConfiguration config, ILogger<AdminLogService> logger)
    : DiscordClientService(client, logger)
{
    private readonly DiscordSocketClient _client = client;

    private readonly ulong _consoleChannelId = ulong.Parse(
        config["channels:console"] ?? throw new Exception("Console channel is not configured"), NumberStyles.Any);
    
    private static readonly TimeSpan AuditLookback = TimeSpan.FromSeconds(15);

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _client.UserLeft += OnUserLeft;
        _client.UserBanned += OnUserBanned;
        _client.UserUnbanned += OnUserUnbanned;
        _client.GuildMemberUpdated += OnGuildMemberUpdated;
        return Task.CompletedTask;
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        // kicks and bans both trigger a UserLeft event; the audit log tells us which one happened
        if (await GetRecentAuditEntry(guild, ActionType.Ban, user.Id) is not null)
            return; // a ban is reported separately by OnUserBanned

        var kick = await GetRecentAuditEntry(guild, ActionType.Kick, user.Id);
        if (kick is not null)
        {
            await LogKick(user, kick);
            return;
        }

        await LogLeft(user);
    }

    private async Task LogLeft(SocketUser user)
    {
        var embed = BaseEmbed(user)
            .WithTitle($"Týpek to lývnul (｡>﹏<)")
            .AddField("Uživatel", $"{user.Mention} ({user})")
            .AddField("ID", user.Id.ToString())
            .Build();

        await SendLog(embed);
    }

    private async Task LogKick(SocketUser user, RestAuditLogEntry entry)
    {
        var embed = BaseEmbed(user)
            .WithTitle("Někdo dostal kopanec, čau! (´•︵•`)")
            .AddField("Uživatel", $"{user.Mention} ({user})")
            .AddField("ID", user.Id.ToString())
            .AddField("Kopanec provedl", entry.User?.Mention ?? "???", inline: true)
            .AddField("Důvod", string.IsNullOrWhiteSpace(entry.Reason) ? "neuveden" : entry.Reason, inline: true)
            .Build();

        await SendLog(embed);
    }

    private async Task OnUserBanned(SocketUser user, SocketGuild guild)
    {
        var entry = await GetRecentAuditEntry(guild, ActionType.Ban, user.Id);

        var embed = BaseEmbed(user)
            .WithTitle("Někdo dostal banán, čau! (ㅠ﹏ㅠ)")
            .AddField("Uživatel", $"{user.Mention} ({user})")
            .AddField("ID", user.Id.ToString())
            .AddField("Banán dal", entry?.User?.Mention ?? "???", inline: true)
            .AddField("Důvod", string.IsNullOrWhiteSpace(entry?.Reason) ? "neuveden" : entry.Reason, inline: true)
            .Build();

        await SendLog(embed);
    }

    private async Task OnUserUnbanned(SocketUser user, SocketGuild guild)
    {
        var entry = await GetRecentAuditEntry(guild, ActionType.Unban, user.Id);

        var embed = BaseEmbed(user)
            .WithColor(Color.Green)
            .WithTitle("Někomu odpustili banán, vítej zpět! (ﾉ◕ヮ◕)ﾉ*:・ﾟ✧")
            .AddField("Uživatel", $"{user.Mention} ({user})")
            .AddField("ID", user.Id.ToString())
            .AddField("Banán odebral", entry?.User?.Mention ?? "???", inline: true)
            .Build();

        await SendLog(embed);
    }

    private async Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        if (!before.HasValue) return;
        var old = before.Value;

        var added = after.Roles.Except(old.Roles).Where(r => !r.IsEveryone).ToList();
        var removed = old.Roles.Except(after.Roles).Where(r => !r.IsEveryone).ToList();

        if (added.Count == 0 && removed.Count == 0) return;

        var diff = string.Join("\n",
            added.OrderByDescending(r => r.Position).Select(r => $"`+` {r.Mention}")
                .Concat(removed.OrderByDescending(r => r.Position).Select(r => $"`-` {r.Mention}")));

        var entry = await GetRecentAuditEntry(after.Guild, ActionType.MemberRoleUpdated, after.Id);

        var embed = BaseEmbed(after)
            .WithTitle("Zmšna rolí ^_^")
            .AddField("Aktuální role", FormatRoles(after.Roles))
            .AddField("Staré role", FormatRoles(old.Roles))
            .AddField("Změna", diff)
            .AddField("Změnu provedl", entry?.User?.Mention ?? "kdosi", inline: true)
            .Build();

        await SendLog(embed);
    }

    private static string FormatRoles(System.Collections.Generic.IEnumerable<SocketRole> roles)
    {
        var mentions = roles.Where(r => !r.IsEveryone)
            .OrderByDescending(r => r.Position)
            .Select(r => r.Mention)
            .ToList();
        return mentions.Count > 0 ? string.Join(" ", mentions) : "žádné";
    }

    private static EmbedBuilder BaseEmbed(SocketUser user) =>
        new EmbedBuilder()
            .WithAuthor(user.ToString(), user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithColor(Color.Red)
            .WithCurrentTimestamp();

    private async Task<RestAuditLogEntry?> GetRecentAuditEntry(SocketGuild guild, ActionType type, ulong targetId)
    {
        await Task.Delay(500); // give Discord a moment to write the audit log
        try
        {
            var logs = await guild.GetAuditLogsAsync(5, actionType: type).FlattenAsync();
            return logs.FirstOrDefault(e =>
                GetAuditTargetId(e) == targetId &&
                DateTimeOffset.UtcNow - e.CreatedAt < AuditLookback);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to read audit logs for {Action}", type);
            return null;
        }
    }

    private static ulong? GetAuditTargetId(RestAuditLogEntry entry) => entry.Data switch
    {
        BanAuditLogData ban => ban.Target.Id,
        UnbanAuditLogData unban => unban.Target.Id,
        KickAuditLogData kick => kick.Target.Id,
        MemberRoleAuditLogData roleUpdate => roleUpdate.Target.Id,
        _ => null
    };

    private async Task SendLog(Embed embed)
    {
        if (_client.GetChannel(_consoleChannelId) is not IMessageChannel channel)
        {
            Logger.LogWarning("Console channel {ChannelId} not found, skipping log", _consoleChannelId);
            return;
        }

        await channel.SendMessageAsync(embed: embed);
    }
}
