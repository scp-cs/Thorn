using System;
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
        private readonly ConstantsService _constants;

        public HelpModule(ConstantsService constants)
        {
            _constants = constants;
        }

        [Command]
        public async Task HelpCommand(int page = 1)
        {
            var maxPages = _constants.Help.Count;
            if (page > maxPages || page <= 0) page = 1;
            var titleIndex = _constants.Help[page - 1].IndexOf(Environment.NewLine);

            var embed = new EmbedBuilder
            {
                Title = _constants.Help[page - 1][..titleIndex],
                Description = _constants.Help[page - 1][++titleIndex..],
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
                    help = _constants.Strings.specificHelp.translation; break;
                case "writing": case "psaní": case "w":
                    help = _constants.Strings.specificHelp.writing; break;
                case "correction": case "korekce": case "c": case "correcting":
                    help = _constants.Strings.specificHelp.correction; break;
                case "join": case "připoj-se": case "připojit": case "členství": case "j": case "joining":
                    help = _constants.Strings.specificHelp.join; break;
                default:
                    return;
            }
            
            await ReplyAsync(embed: new EmbedBuilder
            {
                Description = help,
                Color = Color.Blue
            }.Build());
        }
    }
}