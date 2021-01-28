using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using thorn.UserAccounts;

namespace thorn.Services
{
    public class CommandHandler : InitializedService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _command;
        private readonly IConfiguration _config;
        
        private readonly PairsService _pairs;
        private readonly Random _random;

        public CommandHandler(IServiceProvider provider, DiscordSocketClient client, CommandService commandService, 
            IConfiguration config, PairsService pairs)
        {
            _provider = provider;
            _client = client;
            _command = commandService;
            _config = config;
            _pairs = pairs;
            _random = new Random();
        }

        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            _client.MessageReceived += HandleCommandAsync;
            
            _command.AddTypeReader(typeof(PointType), new PointTypeTypeReader());
            _command.AddTypeReader(typeof(AccountItem), new AccountItemTypeReader());
            await _command.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }

        private async Task HandleCommandAsync(SocketMessage m)
        {
            if (!(m is SocketUserMessage msg) || m.Source == MessageSource.Bot) return;
            if (await HahaFunni(m)) return;

            var argPos = 0;
            var context = new SocketCommandContext(_client, msg);

            if (msg.HasStringPrefix(_config["prefix"], ref argPos))
                await _command.ExecuteAsync(context, argPos, _provider);
        }

        private async Task<bool> HahaFunni(SocketMessage m)
        {
            if (m.Content.Equals("good bot", StringComparison.OrdinalIgnoreCase))
                await m.AddReactionAsync(Emote.Parse(_pairs.GetString("AGRLOVE_EMOTE")));
            
            else if (m.Content.Equals("bad bot", StringComparison.OrdinalIgnoreCase))
                await m.AddReactionAsync(Emote.Parse(_pairs.GetString("SAD_EMOTE")));
            
            else if (m.Content.Equals(_pairs.GetString("THANKS_THORN_TRIGGER"), StringComparison.OrdinalIgnoreCase))
                await m.AddReactionAsync(Emote.Parse(_pairs.GetString("AGRLOVE_EMOTE")));
            
            else if (m.Content.Equals(_pairs.GetString("BODIES_IN_WATER_TRIGGER"), StringComparison.OrdinalIgnoreCase))
                await m.Channel.SendMessageAsync(_random.Next(2) == 0 ?
                    _pairs.GetString("BODIES_IN_WATER_ERROR") :
                    _pairs.GetString("BODIES_IN_WATER_SUCCESS"));
            
            else if (m.Content.Equals(_pairs.GetString("BODIES_IN_WATER_OHGODOHFUCK_TRIGGER"), StringComparison.OrdinalIgnoreCase))
                await m.Channel.SendMessageAsync(_pairs.GetString("BODIES_IN_WATER_OHGODOHFUCK"));
            
            else return false;
            return true;
        }
    }
}