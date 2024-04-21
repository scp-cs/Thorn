using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using thorn.UserAccounts;

namespace thorn.Services;

public class MessageHandler(
    IServiceProvider provider,
    DiscordSocketClient client,
    CommandService commandService,
    ILogger<MessageHandler> logger)
    : DiscordClientService(client, logger)
{
    private readonly DiscordSocketClient _client = client;
    private readonly Random _random = new();

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _client.MessageReceived += HandleMessageAsync;

        commandService.AddTypeReader(typeof(PointType), new PointTypeTypeReader());
        commandService.AddTypeReader(typeof(AccountItem), new AccountItemTypeReader());
        await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
    }

    private async Task HandleMessageAsync(SocketMessage m)
    {
        if (m is not SocketUserMessage msg || m.Source == MessageSource.Bot) return;

        await HahaFunni(m);
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
                ? """[✘] Zamítnuto. CRV uživatele není v přijatelných hodnotách. CRV uživatele je ovlivněno aktivními kognitohazardy. Prosím, zůstaňte na místě, člen lékařského tým[''///afe44/25\\\\23 s vámi bude za chvíli."""
                : """[✅] Přijato. CRV uživatele je v přijatelných hodnotách.""");

        else if (Regex.IsMatch(m.Content, @"pozn[áa]v[áa]m t[ay] t[ěe]la ve vod[ěe]", RegexOptions.IgnoreCase))
            await m.Channel.SendMessageAsync("<:monkaw:939084023190421504>");

        else if (Regex.IsMatch(m.Content, @"pls penis"))
            await m.Channel.SendMessageAsync(GeneratePenis(m.Author));

	    else if (Regex.IsMatch(m.Content, @":3"))
	        await m.Channel.SendMessageAsync(":33");

        else if (Regex.IsMatch(m.Content, @"how waifu[\?]?", RegexOptions.IgnoreCase)){
            SocketUser usr;
            try { usr = m.MentionedUsers.First(); }
            catch { usr = m.Author; }

            await m.Channel.SendMessageAsync(HowWaifu(usr));
        }

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
        var mention = $"{usr.Mention} ";

        mention += p switch
        {
            0 => "Jsi druhý příchod kristova vědomí (0% waifu) \\o/",
            < 15 => $"Nic moc kamaráde, dnes jsi jen {p}% waifu :(",
            < 50 => $"Ujde to příteli! Jsi z {p}% waifu.",
            < 80 => $"Sluší ti to :) jsi {p} procentní waifu!",
            < 100 => $"Neuvěřitelné! Jsi waifu ze skvělých {p}% :D",
            _ => "Honto？ 100％！ Omae wa waifu no materiaru da yo!! Sugoi!!! 😳"
        };

        return mention;
    }
}
