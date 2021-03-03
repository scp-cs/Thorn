using System.Threading.Tasks;
using Discord.Commands;
using thorn.Services;

namespace thorn.Modules
{
    public class WikiModule : ModuleBase<SocketCommandContext>
    {
        private readonly ScpService _scp;

        public WikiModule(ScpService scp)
        {
            _scp = scp;
        }

        [Command("wiki")]
        [Alias("w")]
        public async Task WikiCommand([Remainder] string query) => await ReplyAsync(embed: await _scp.GetEmbedForArticle(query));
    }
}