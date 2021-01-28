using System.Threading.Tasks;
using Discord.Commands;

namespace thorn.Modules
{
    [Summary("Miscellaneous functions")]
    public class MiscModule : ModuleBase<SocketCommandContext>
    {
        // You cannot have a proper bot without these :)

        [Command("ping")]
        public async Task PingCommand() => await ReplyAsync("Pong!");
        
        [Command("pong")]
        public async Task PongCommand() => await ReplyAsync("Ping!");
    }
}