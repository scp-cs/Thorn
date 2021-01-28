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
            _logger.LogInformation($"{Context.User} used STOP in #{Context.Channel}");
        }

        [Command("status")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task StatusCommand([Remainder] string status)
        {
            await _client.SetGameAsync(status);
            _logger.LogInformation($"{Context.User} changed status to: {status}");
        }
        
        [Command("say")]
        [Priority(0)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SayCommand([Remainder] string text)
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(text);
            _logger.LogInformation($"{Context.User} said \"{text}\" in #{Context.Channel}");
        }

        [Command("say")]
        [Priority(1)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SayElsewhereCommand(SocketTextChannel channel, [Remainder] string text)
        {
            await channel.SendMessageAsync(text);
            _logger.LogInformation($"{Context.User} said \"{text}\" in #{channel}");
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
            _logger.LogInformation($"{Context.User} made a vote in #{Context.Channel}: {text}");
        }

        [Command("vote")]
        [Priority(1)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task VoteElsewhereCommand(SocketTextChannel channel, [Remainder] string text)
        {
            var msg = await channel.SendMessageAsync(text);
            await AddVoteEmotes(msg);
            _logger.LogInformation($"{Context.User} made a vote in #{channel}: {text}");
        }
        
        
        // Boring stuff

        [Command("mping")]
        [Alias("master-ping")]
        [RequireUserPermission(GuildPermission.Administrator)]
        // This exists solely for debugging purposes
        public async Task MasterPingCommand()
        {
            await ReplyAsync("Pong!");
            _logger.LogInformation($"{Context.User} pinged!");
        }

        [Command("loads")]
        [Alias("load-strings")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ReloadStringsCommand()
        {
            await _pairs.ReloadStrings();
            _logger.LogInformation($"{Context.User} reloaded strings");
            await ReplyAsync("Reloaded!");
        }

        [Command("loadp")]
        [Alias("load-profiles")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ReloadProfilesCommand()
        {
            await _accounts.LoadAccountsAsync();
            _logger.LogInformation($"{Context.User} reloaded profiles");
            await ReplyAsync("Reloaded!");
        }

        private async Task AddVoteEmotes(IMessage msg)
        {
            await msg.AddReactionAsync(Emote.Parse(_pairs.GetString("YES_EMOTE")));
            await msg.AddReactionAsync(Emote.Parse(_pairs.GetString("NO_EMOTE")));
            await msg.AddReactionAsync(Emote.Parse(_pairs.GetString("ABSTAIN_EMOTE")));
        }
    }
}