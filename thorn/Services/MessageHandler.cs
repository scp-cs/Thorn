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

	    else if (Regex.IsMatch(m.Content, @":3"))
	        await m.Channel.SendMessageAsync(":33");
        
        else if (Regex.IsMatch(m.Content, @"among\s?us"))
            await m.Channel.SendMessageAsync("ඞ");

        else return false;
        return true;
    }
}
