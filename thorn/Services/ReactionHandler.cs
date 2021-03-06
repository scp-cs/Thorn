using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace thorn.Services
{
    public class ReactionHandler : DiscordClientService
    {
        private readonly DiscordSocketClient _client;
        private readonly PairsService _pairs;
        private readonly QuicklinkService _quicklink;
        
        // TODO: replace with SocketTextChannel
        private readonly ulong _welcomeChannelId;
        private readonly ulong _loggingChannelId;
        private readonly ulong _classCRoleId;
        private readonly ulong _intRoleId;

        public ReactionHandler(DiscordSocketClient client, PairsService pairs, ILogger<ReactionHandler> logger, QuicklinkService quicklink) : base(client, logger)
        {
            _client = client;
            _pairs = pairs;
            _quicklink = quicklink;

            _welcomeChannelId = ulong.Parse(_pairs.GetString("WELCOME_CHANNEL_ID"));
            _loggingChannelId = ulong.Parse(_pairs.GetString("LOGGING_CHANNEL_ID"));
            _classCRoleId = ulong.Parse(_pairs.GetString("CLASS_C_ROLE_ID"));
            _intRoleId = ulong.Parse(_pairs.GetString("INT_ROLE_ID"));
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _client.ReactionAdded += ClientOnReactionAdded;
            return Task.CompletedTask;
        }

        private async Task ClientOnReactionAdded(Cacheable<IUserMessage, ulong> cacheable,
            ISocketMessageChannel messageChannel, SocketReaction reaction)
        {
            if (reaction.UserId == _client.CurrentUser.Id) return;
            
            // Reaction handling in the #welcome channel
            if (reaction.Channel.Id == _welcomeChannelId) await HandleWelcomeReactions(cacheable, reaction);
            
            // User requested quick link delete
            if (Equals(reaction.Emote, new Emoji("🗞️")) && _quicklink.IsRecentQuicklink(reaction.MessageId))
                await reaction.Message.Value.DeleteAsync();
        }

        private async Task HandleWelcomeReactions(Cacheable<IUserMessage, ulong> cacheable, SocketReaction reaction)
        {
            if (!((IGuildUser) reaction.User.Value).GuildPermissions.Administrator) return;

            var emote = reaction.Emote;
            var user = (IGuildUser) cacheable.Value.Author;

            // TODO: Maybe create another service for this?
            if (Equals(emote, Emote.Parse(_pairs.GetString("999_EMOTE"))))
                await AssignRole(cacheable, reaction, user, UserActions.ClassC);
            else if (Equals(emote, Emote.Parse(_pairs.GetString("POGEY_EMOTE"))))
                await AssignRole(cacheable, reaction, user, UserActions.Int);
            else if (Equals(emote, Emote.Parse(_pairs.GetString("SAD_EMOTE"))))
                await AssignRole(cacheable, reaction, user, UserActions.Underage);
            else if (Equals(emote, Emote.Parse(_pairs.GetString("RAGEY_EMOTE"))))
            {
                await user.BanAsync();
                Logger.LogWarning("Emergency ban enacted by {ReactionUser} on {User} ({UserId})", reaction.User, user, user.Id);
            }
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
            await ((ISocketMessageChannel) _client.GetChannel(_loggingChannelId)).SendMessageAsync(message);
            Logger.LogInformation("{ReactionUser} made action in #welcome: {Message}", reaction.User, message);
        }
    }

    internal enum UserActions
    {
        ClassC,
        Int,
        Underage
    }
}