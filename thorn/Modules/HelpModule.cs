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

        // TODO: make all of these a single command
        
        [Command("translation")]
        [Alias("překlad", "překlady", "translations", "t")]
        public async Task TranslationHelpCommand()
        {
            await ReplyAsync(embed: new EmbedBuilder
            {
                Title = _pairs.GetString("HELP_TITLE_TRANSLATION"),
                Description = _pairs.GetString("HELP_DESCRIPTION_TRANSLATION"),
                Color = Color.Blue
            }.Build());
        }

        [Command("writing")]
        [Alias("psaní", "w")]
        public async Task WritingHelpCommand()
        {
            await ReplyAsync(embed: new EmbedBuilder
            {
                Title = _pairs.GetString("HELP_TITLE_WRITING"),
                Description = _pairs.GetString("HELP_DESCRIPTION_WRITING"),
                Color = Color.Red
            }.Build());
        }

        [Command("correction")]
        [Alias("korekce", "c")]
        public async Task CorrectionHelpCommand()
        {
            await ReplyAsync(embed: new EmbedBuilder
            {
                Title = _pairs.GetString("HELP_TITLE_CORRECTION"),
                Description = _pairs.GetString("HELP_DESCRIPTION_CORRECTION"),
                Color = Color.Green
            }.Build());
        }

        [Command("join")]
        [Alias("připoj-se", "připojit", "členství", "j")]
        public async Task JoinHelpCommand()
        {
            await ReplyAsync(embed: new EmbedBuilder
            {
                Title = _pairs.GetString("HELP_TITLE_JOIN"),
                Description = _pairs.GetString("HELP_DESCRIPTION_JOIN"),
                Color = Color.Orange
            }.Build());
        }
    }
}