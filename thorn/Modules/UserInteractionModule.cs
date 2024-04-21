using System;
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
    private readonly Random _random = new();
    
    [SlashCommand("ping", "pong!")]
    public async Task Ping() => await RespondAsync("pong!");

    [SlashCommand("pomoc", "Pomoc s překládáním, psaním, korekcí a připojením na stránku.")]
    [RequireRole("xd")]
    public async Task Help([Summary("téma", "o čem zobrazit pomoc?")] Help choice)
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

    [SlashCommand("penis", "pls penis \ud83d\ude33")]
    public async Task Penis()
    {
        var len = _random.Next(15);
        await RespondAsync($"{Context.User.Mention} tvůj pele: `8{new string('=', len)}D`");
    }
    
    [SlashCommand("waifu", "how waifu?")]
    public async Task Waifu([Summary("uživatel", "jaký uživatel bude waifu?")] IUser user=null)
    {
        if (user == Context.Client.CurrentUser)
            await RespondAsync("já jsem ta nejlepší waifu :3");
        
        user ??= Context.User;
        
        var p = _random.Next(101);
        var mention = $"{user.Mention} ";

        mention += p switch
        {
            0 => "Jsi druhý příchod kristova vědomí (0% waifu) \\o/",
            < 15 => $"Nic moc kamaráde, dnes jsi jen {p}% waifu :(",
            < 50 => $"Ujde to příteli! Jsi z {p}% waifu.",
            < 80 => $"Sluší ti to :) jsi {p} procentní waifu!",
            < 100 => $"Neuvěřitelné! Jsi waifu ze skvělých {p}% :D",
            _ => "Honto？ 100％！ Omae wa waifu no materiaru da yo!! Sugoi!!! 😳"
        };

        await RespondAsync(mention);
    }
    
    
}