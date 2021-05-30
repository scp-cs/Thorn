using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using thorn.Services;

namespace thorn.Modules
{
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly PairsService _pairs;
        private readonly UserAccountsService _accounts;
        private readonly ILogger<AdminModule> _logger;

        public AdminModule(ILogger<AdminModule> logger, DiscordSocketClient client, PairsService pairs,
            UserAccountsService accounts)
        {
            _logger = logger;
            _client = client;
            _pairs = pairs;
            _accounts = accounts;
        }

        [Command("info")]
        public async Task InfoCommand()
        {
            await ReplyAsync(embed: new EmbedBuilder
            {
                Title = "Thorn.aic",
                Description = $"{_pairs.GetString("INFO")}\n\n**Ping:** {_client.Latency}ms",
                ThumbnailUrl = _client.CurrentUser.GetAvatarUrl(),
                Color = new Color(153, 204, 0)
            }.Build());
        }

        [Command("stop")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task StopCommand()
        {
            await Context.Channel.SendFileAsync("Media/stop.png");
            _logger.LogInformation("{ContextUser} used STOP in #{ContextChannel}", Context.User, Context.Channel);
        }

        [Command("status")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task StatusCommand(string mode, [Remainder] string status)
        {
            IActivity game = mode switch
            {
                "streaming" => new Game(status, ActivityType.Streaming),
                "watching" => new Game(status, ActivityType.Watching),
                "listening" => new Game(status, ActivityType.Listening),
                _ => new Game(status),
            };

            await _client.SetActivityAsync(game);
            
            _logger.LogInformation("{ContextUser} changed {Game} status to: {Status}", Context.User, game.Type, status);
        }
        
        [Command("say")]
        [Priority(0)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SayCommand([Remainder] string text)
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(text);
            _logger.LogInformation("{ContextUser} said \"{Text}\" in #{ContextChannel}", Context.User, text, Context.Channel);
        }

        [Command("say")]
        [Priority(1)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SayElsewhereCommand(SocketTextChannel channel, [Remainder] string text)
        {
            await channel.SendMessageAsync(text);
            // ReSharper disable once ComplexObjectDestructuringProblem - It freezes the gateway task, and that's not good lol
            _logger.LogInformation("{ContextUser} said \"{Text}\" in #{Channel}", Context.User, text, channel);
        }

        [Command("edit")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task EditCommand(IUserMessage message, [Remainder] string text)
        {
            // BUG: If the message is not in cache, it will not get edited
            // Not sure how to resolve this, I have not found a way to download a single message by ID
            await message.ModifyAsync(x => x.Content = text);
            _logger.LogInformation("{User} edited message '{Message}' to '{Text}'", Context.User, message.Content, text);
        }


        [Command("vote")]
        [Priority(0)]
        // This command is available only to OPs, because this *may* be recognised by users as an "official vote"
        // I'll change this maybe someday?
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task VoteCommand([Remainder] string text)
        {
            var msg = await ReplyAsync(text);
            await AddVoteEmotes(msg);
            _logger.LogInformation("{ContextUser} made a vote in #{ContextChannel}: {Text}", Context.User, Context.Channel, text);
        }

        [Command("vote")]
        [Priority(1)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task VoteElsewhereCommand(SocketTextChannel channel, [Remainder] string text)
        {
            var msg = await channel.SendMessageAsync(text);
            await AddVoteEmotes(msg);
            // ReSharper disable once ComplexObjectDestructuringProblem - It freezes the gateway task, and that's not good lol
            _logger.LogInformation("{ContextUser} made a vote in #{Channel}: {Text}", Context.User, channel, text);
        }
        
        
        // Boring stuff

        [Command("mping")]
        [Alias("master-ping")]
        [RequireUserPermission(GuildPermission.Administrator)]
        // This exists solely for debugging purposes
        public async Task MasterPingCommand()
        {
            await ReplyAsync("Pong!");
            _logger.LogInformation("{ContextUser} pinged!", Context.User);
        }

        [Command("loads")]
        [Alias("load-strings")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ReloadStringsCommand()
        {
            await _pairs.ReloadStrings();
            _logger.LogInformation("{ContextUser} reloaded strings", Context.User);
            await ReplyAsync("Reloaded!");
        }

        [Command("loadp")]
        [Alias("load-profiles")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ReloadProfilesCommand()
        {
            await _accounts.LoadAccountsAsync();
            _logger.LogInformation("{ContextUser} reloaded profiles", Context.User);
            await ReplyAsync("Reloaded!");
        }

        private async Task AddVoteEmotes(IMessage msg)
        {
            await msg.AddReactionAsync(Emote.Parse(_pairs.GetString("YES_EMOTE")));
            await msg.AddReactionAsync(Emote.Parse(_pairs.GetString("NO_EMOTE")));
            await msg.AddReactionAsync(Emote.Parse(_pairs.GetString("ABSTAIN_EMOTE")));
            await msg.AddReactionAsync(new Emoji("👁️"));
        }
    }
}