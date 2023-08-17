using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace thorn.Services;

public class ReactionHandler : DiscordClientService
{
    private readonly DiscordSocketClient _client;
    private readonly ConstantsService _constants;

    // TODO: replace with SocketTextChannel
    private readonly ulong _welcomeChannelId;
    private readonly ulong _loggingChannelId;
    private readonly ulong _classCRoleId;
    private readonly ulong _intRoleId;

    public ReactionHandler(DiscordSocketClient client, ConstantsService constants, ILogger<ReactionHandler> logger) : base(client, logger)
    {
        _client = client;
        _constants = constants;

        _welcomeChannelId = _constants.Channels["welcome"];
        _loggingChannelId = _constants.Channels["o5"];
        _classCRoleId = _constants.Roles["classC"];
        _intRoleId = _constants.Roles["INT"];
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _client.ReactionAdded += ClientOnReactionAdded;
        return Task.CompletedTask;
    }

    private async Task ClientOnReactionAdded(Cacheable<IUserMessage, ulong> messageCache,
        Cacheable<IMessageChannel, ulong> channelCache, SocketReaction reaction)
    {
        if (reaction.UserId == _client.CurrentUser.Id) return;

        // Reaction handling in the #welcome channel
        if (reaction.Channel.Id == _welcomeChannelId) await HandleWelcomeReactions(messageCache, reaction);
    }

    private async Task HandleWelcomeReactions(Cacheable<IUserMessage, ulong> cacheable, SocketReaction reaction)
    {
        if (!((IGuildUser)reaction.User.Value).GuildPermissions.Administrator) return;

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