using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using thorn.Services;

namespace thorn.Modules
{
    [Group("help")]
    [Alias("halp", "pomoc", "cože", "hlep", "aaa", "h", "fuckme", "hwelp")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly PairsService _pairs;

        public HelpModule(PairsService pairs)
        {
            _pairs = pairs;
        }

        [Command]
        public async Task HelpCommand(int page = 1)
        {
            var maxPages = int.Parse(_pairs.GetString("HELP_PAGES"));
            if (page > maxPages || page <= 0) page = 1;

            var embed = new EmbedBuilder
            {
                Title = _pairs.GetString($"HELP_TITLE_{page}"),
                Description = _pairs.GetString($"HELP_DESCRIPTION_{page}"),
                Color = Color.LightGrey,
                Footer = new EmbedFooterBuilder().WithText($"Strana {page} z {maxPages}")
            }.Build();

            await ReplyAsync(embed: embed);
        }

        [Command]
        public async Task HelpCommand(string page)
        {
            string help;

            switch (page)
            {
                case "translation": case "překlad": case "překlady": case "translations": case "t": case "translating":
                    help = "TRANSLATION"; break;
                case "writing": case "psaní": case "w":
                    help = "WRITING"; break;
                case "correction": case "korekce": case "c": case "correcting":
                    help = "CORRECTION"; break;
                case "join": case "připoj-se": case "připojit": case "členství": case "j": case "joining":
                    help = "JOIN"; break;
                default:
                    return;
            }
            
            await ReplyAsync(embed: new EmbedBuilder
            {
                Title = _pairs.GetString($"HELP_TITLE_{help}"),
                Description = _pairs.GetString($"HELP_DESCRIPTION_{help}"),
                Color = Color.Blue
            }.Build());
        }
    }
}