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
        await Client.WaitForReadyAsync(cancellationToken);
        
        await handler.RegisterCommandsGloballyAsync();
    }


    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(Client, interaction);
            var result = await handler.ExecuteCommandAsync(context, provider);

            // Due to async nature of InteractionFramework, the result here may always be success.
            // That's why we also need to handle the InteractionExecuted event.
            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        await interaction.RespondAsync(
                            "Tento příkaz se nepodařilo spustit, pravděpodobně nemáš práva :(", ephemeral: true);
                        break;
                }
        }
        catch
        {
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }
}