using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using thorn.UserAccounts;

namespace thorn.Services
{
    public class CommandHandler : DiscordClientService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _command;
        private readonly IConfiguration _config;
        private readonly QuicklinkService _quicklink;
        
        private readonly ConstantsService _constants;
        private readonly Random _random;

        public CommandHandler(IServiceProvider provider, DiscordSocketClient client, CommandService commandService, 
            IConfiguration config, ConstantsService constants, QuicklinkService quicklink, ILogger<CommandHandler> logger) : base(client, logger)
        {
            _provider = provider;
            _client = client;
            _command = commandService;
            _config = config;
            _constants = constants;
            _quicklink = quicklink;
            _random = new Random();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _client.MessageReceived += HandleCommandAsync;

            _command.AddTypeReader(typeof(PointType), new PointTypeTypeReader());
            _command.AddTypeReader(typeof(AccountItem), new AccountItemTypeReader());
            await _command.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }

        private async Task HandleCommandAsync(SocketMessage m)
        {
            if (!(m is SocketUserMessage msg) || m.Source == MessageSource.Bot) return;
            
            var argPos = 0;
            
            if (!msg.HasStringPrefix(_config["prefix"], ref argPos))
            {
                if (await HahaFunni(m)) return;
                if (await _quicklink.CheckForScpReference(msg)) return;
            }
            else
            {
                var context = new SocketCommandContext(_client, msg);
                await _command.ExecuteAsync(context, argPos, _provider);
            }
        }

        private async Task<bool> HahaFunni(SocketMessage m)
        {
            if (m.Content.Equals("good bot", StringComparison.OrdinalIgnoreCase))
                await m.AddReactionAsync(Emote.Parse((string) _constants.Strings.emote.agrlove));
            
            else if (m.Content.Equals("bad bot", StringComparison.OrdinalIgnoreCase))
                await m.AddReactionAsync(Emote.Parse((string) _constants.Strings.emote.sad));
            
            else if (m.Content.Equals("Díky Thorne", StringComparison.OrdinalIgnoreCase)) // TODO: change this to a regex
                await m.AddReactionAsync(Emote.Parse((string) _constants.Strings.emote.agrlove));
            
            else if (m.Content.Equals(_constants.Strings.bodies.trigger, StringComparison.OrdinalIgnoreCase)) // TODO: change this to a regex as well
                await m.Channel.SendMessageAsync(_random.Next(2) == 0 ?
                    _constants.Strings.bodies.error :
                    _constants.Strings.bodies.success);
            
            else if (m.Content.Equals(_constants.Strings.bodies.ohgodohfuckTrigger, StringComparison.OrdinalIgnoreCase)) // TODO: and this as well
                await m.Channel.SendMessageAsync(_constants.Strings.bodies.ohgodohfuck);
            
            else return false;
            return true;
        }
    }
}