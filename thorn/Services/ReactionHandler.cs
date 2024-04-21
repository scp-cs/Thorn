using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace thorn.Services;

public class ReactionHandler(DiscordSocketClient client, IConfiguration config, ILogger<ReactionHandler> logger)
    : DiscordClientService(client, logger)
{
    private readonly DiscordSocketClient _client = client;
    
    private readonly ulong _welcomeChannelId = ulong.Parse(config["channels:welcome"] ?? throw new Exception("Welcome channel is not configured"), NumberStyles.Any);
    private readonly ulong _loggingChannelId = ulong.Parse(config["channels:o5"] ?? throw new Exception("O5 channel is not configured"), NumberStyles.Any);
    private readonly ulong _classCRoleId = ulong.Parse(config["roles:classC"] ?? throw new Exception("Role Class C is not configured"), NumberStyles.Any);
    private readonly ulong _intRoleId = ulong.Parse(config["roles:INT"] ?? throw new Exception("Role INT is not configured"), NumberStyles.Any);
    private readonly ulong _o5RoleId = ulong.Parse(config["roles:O5"] ?? throw new Exception("Role O5 is not configured"), NumberStyles.Any);
    private readonly ulong _o4RoleId = ulong.Parse(config["roles:O4"] ?? throw new Exception("Role O4 is not configured"), NumberStyles.Any);

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _client.ReactionAdded += ClientOnReactionAdded;
        return Task.CompletedTask;
    }

    private async Task ClientOnReactionAdded(Cacheable<IUserMessage, ulong> messageCache,
        Cacheable<IMessageChannel, ulong> channelCache, SocketReaction reaction)
    {
        if (reaction.UserId == _client.CurrentUser.Id) return;
        
        if (reaction.Channel.Id == _welcomeChannelId) await HandleWelcomeReactions(messageCache, reaction);
    }

    private async Task HandleWelcomeReactions(Cacheable<IUserMessage, ulong> cacheable, SocketReaction reaction)
    {
        var guildUser = (IGuildUser)reaction.User.Value;
        if (!guildUser.RoleIds.ToList().Contains(_o5RoleId) && !guildUser.RoleIds.ToList().Contains(_o4RoleId))
            return;

        var emote = reaction.Emote;
        var user = (IGuildUser)cacheable.Value.Author;

        // TODO: Maybe create another service for this?
        if (Equals(emote, new Emoji("👍")))
            await AssignRole(cacheable, reaction, user, UserActions.ClassC);
        else if (Equals(emote, new Emoji("😄")))
            await AssignRole(cacheable, reaction, user, UserActions.Int);
        else if (Equals(emote, new Emoji("😔")))
            await AssignRole(cacheable, reaction, user, UserActions.Underage);
    }

    private async Task AssignRole(Cacheable<IUserMessage, ulong> cacheable, SocketReaction reaction,
        IGuildUser user, UserActions action)
    {
        string message;
        switch (action)
        {
            case UserActions.ClassC:
                await user.AddRoleAsync(user.Guild.GetRole(_classCRoleId));
                message = $"**Přijat nový člen {user.Mention}!** [{user.Id}]\n" +
                          $"```{cacheable.Value.Content}```";
                break;
            case UserActions.Int:
                await user.AddRoleAsync(user.Guild.GetRole(_intRoleId));
                message = $"**Přijat nový *INT* člen {user.Mention}!** [{user.Id}]\n" +
                          $"```{cacheable.Value.Content}```";
                break;
            case UserActions.Underage:
                message = $"**Uživatel {user.Mention} pod věkovou hranicí!** [{user.Id}]\n" +
                          $"```{cacheable.Value.Content}```";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }

        await reaction.Message.Value.DeleteAsync();
        await ((ISocketMessageChannel)_client.GetChannel(_loggingChannelId)).SendMessageAsync(message);
        Logger.LogInformation("{ReactionUser} made action in #welcome: {Message}", reaction.User, message);
    }
}

internal enum UserActions
{
    ClassC,
    Int,
    Underage
}