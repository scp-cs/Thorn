using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace thorn.Services;

public class InteractionHandler(
    DiscordSocketClient client,
    ILogger<InteractionHandler> logger,
    InteractionService handler,
    IServiceProvider provider)
    : DiscordClientService(client, logger)
{
    private readonly Random _random = new();
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await handler.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
        
        Client.InteractionCreated += HandleInteraction;
        handler.InteractionExecuted += HandleExecuted;
        Client.MessageReceived += HandleMessage;
        
        await Client.WaitForReadyAsync(cancellationToken);
        
        await handler.RegisterCommandsGloballyAsync();
    }

    private async Task HandleMessage(SocketMessage m)
    {
        if (m.Source == MessageSource.Bot) return;
        await HahaFunni(m);
    }

    private async Task HandleExecuted(ICommandInfo command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess && result.Error == InteractionCommandError.UnmetPrecondition)
            await context.Interaction.RespondAsync("na tuto akci nemáš oprávnění :(", ephemeral: true);
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(Client, interaction);
        await handler.ExecuteCommandAsync(context, provider);
    }
    
    private async Task HahaFunni(SocketMessage m)
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
        
        else if (Regex.IsMatch(m.Content, @"amo[n]?g(\s?us|)"))
            await m.Channel.SendMessageAsync("ඞ");
    }
}