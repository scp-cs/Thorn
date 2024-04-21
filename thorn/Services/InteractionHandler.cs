using System;
using System.Reflection;
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
    ILogger<DiscordClientService> logger,
    InteractionService handler,
    IServiceProvider provider)
    : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await handler.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
        
        Client.InteractionCreated += HandleInteraction;
        handler.InteractionExecuted += HandleExecuted;
        
        await Client.WaitForReadyAsync(cancellationToken);
        
        await handler.RegisterCommandsGloballyAsync();
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
}