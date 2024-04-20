using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;

namespace thorn.Modules;

public enum Help
{
    [ChoiceDisplay("Připojení na stránku")] Join,
    [ChoiceDisplay("Překlad")] Translate,
    [ChoiceDisplay("Psaní")] Write,
    [ChoiceDisplay("Korekce")] Correction,
}

public class UserInteractionModule(IConfiguration config) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "pong!")]
    public async Task Ping() => await RespondAsync("pong!");

    [SlashCommand("pomoc", "Pomoc s překládáním, psaním, korekcí a připojením na stránku.")]
    [RequireRole("xd")]
    public async Task Help(Help choice)
    {
        switch (choice)
        {
            case Modules.Help.Join:
                await RespondAsync(config["helpText:join"]);
                break;
            case Modules.Help.Translate:
                await RespondAsync(config["helpText:translate"]);
                break;
            case Modules.Help.Write:
                await RespondAsync(config["helpText:write"]);
                break;
            case Modules.Help.Correction:
                await RespondAsync(config["helpText:correction"]);
                break;
            default:
                await RespondAsync("wtf");
                break;
        }
    }

    [SlashCommand("info", "Informace o Thornovi")]
    public async Task Info()
    {
        var embed = new EmbedBuilder
        {
            Title = "Thorn.aic",
            Description = $"{config["miscText:info"]}\n\n**Ping:** {Context.Client.Latency}ms",
            ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(),
            Color = new Color(153, 204, 0)
        }.Build();

        await RespondAsync(embed: embed);
    }
}