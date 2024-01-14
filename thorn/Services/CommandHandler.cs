using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using thorn.UserAccounts;

namespace thorn.Services;

public class CommandHandler : DiscordClientService
{
    private readonly IServiceProvider _provider;
    private readonly DiscordSocketClient _client;
    private readonly CommandService _command;
    private readonly IConfiguration _config;

    private readonly ConstantsService _constants;
    private readonly Random _random;

    public CommandHandler(IServiceProvider provider, DiscordSocketClient client, CommandService commandService,
        IConfiguration config, ConstantsService constants, ILogger<CommandHandler> logger) :
        base(client, logger)
    {
        _provider = provider;
        _client = client;
        _command = commandService;
        _config = config;
        _constants = constants;
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
        if (m is not SocketUserMessage msg || m.Source == MessageSource.Bot) return;

        var argPos = 0;

        if (!msg.HasStringPrefix(_config["prefix"], ref argPos))
        {
            if (await HahaFunni(m)) return;
        }
        else
        {
            var context = new SocketCommandContext(_client, msg);
            await _command.ExecuteAsync(context, argPos, _provider);
        }
    }

    private async Task<bool> HahaFunni(SocketMessage m)
    {
        if (m.Content.Equals("good bot", StringComparison.InvariantCultureIgnoreCase))
            await m.AddReactionAsync(new Emoji("💕"));

        else if (m.Content.Equals("bad bot", StringComparison.InvariantCultureIgnoreCase))
            await m.AddReactionAsync(new Emoji("😔"));

        else if (Regex.IsMatch(m.Content, @"d[íi]ky(, |,| )thorne", RegexOptions.IgnoreCase))
            await m.AddReactionAsync(new Emoji("❤️"));

        else if (Regex.IsMatch(m.Content, @"nepozn[áa]v[áa]m t[ay] t[ěe]la ve vod[ěe]", RegexOptions.IgnoreCase))
            await m.Channel.SendMessageAsync(_random.Next(2) == 0
                ? (string) _constants.Strings.bodies.error
                : (string) _constants.Strings.bodies.success);

        else if (Regex.IsMatch(m.Content, @"pozn[áa]v[áa]m t[ay] t[ěe]la ve vod[ěe]", RegexOptions.IgnoreCase))
            await m.Channel.SendMessageAsync((string) _constants.Strings.bodies.ohgodohfuck);

        else if (Regex.IsMatch(m.Content, @"pls penis"))
            await m.Channel.SendMessageAsync(GeneratePenis(m.Author));

	    else if (Regex.IsMatch(m.Content, @":3"))
	        await m.Channel.SendMessageAsync(":33");

        else if (Regex.IsMatch(m.Content, @"how waifu[\?]?", RegexOptions.IgnoreCase))
            await m.Channel.SendMessageAsync(HowWaifu(m.Author));

        else return false;
        return true;
    }

    private string GeneratePenis(SocketUser usr)
    {
        var len = _random.Next(15);
        return $"{usr.Mention} tvůj pele: `8{new string('=', len)}D`";
    }

    private string HowWaifu(SocketUser usr)
    {
        var p = _random.Next(101);
        
        if (p == 0)
            return $"Jsi druhý příchod kristova vědomí (0% waifu) \\o/";
        if (p < 15)
            return $"Nic moc kamaráde, dnes jsi jen {p}% waifu :(";
        else if (p < 50)
            return $"Ujde to příteli! Jsi z {p}% waifu.";
        else if (p < 80)
            return $"Sluší ti to :) jsi {p} procentní waifu!";
        else if (p < 100)
            return $"Neuvěřitelné! Jsi waifu ze skvělých {p}% :D";
        else
            return $"Páni! Jsi úplný waifu materiál!! ";
    }
}
