using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Quartz;
using thorn.Services;

namespace thorn.Jobs;

public class KickInactiveJob : IJob
{
    private readonly ILogger<KickInactiveJob> _logger;
    private readonly SocketTextChannel _logChannel;
    private readonly SocketTextChannel _welcomeChannel;
    private readonly ConstantsService _constants;

    private readonly List<ulong> _cachedUsers;

    public KickInactiveJob(ILogger<KickInactiveJob> logger, ConstantsService constants, DiscordSocketClient client)
    {
        _logger = logger;
        _constants = constants;

        _logChannel = client.GetChannel(_constants.Channels["console"]) as SocketTextChannel;
        _welcomeChannel = client.GetChannel(_constants.Channels["welcome"]) as SocketTextChannel;
        _cachedUsers = new List<ulong>();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _welcomeChannel.Guild.DownloadUsersAsync();
        var kickedUsers = new StringBuilder("Kicknul jsem následující uživatele za neaktivitu:\n\n");
        
        foreach (var user in _welcomeChannel.Users)
        {
            if (user.Roles.Any(x => x.Id == _constants.Roles["o5"])) continue;
            
            if (!_cachedUsers.Contains(user.Id))
                _cachedUsers.Add(user.Id);
            else
            {
                await user.KickAsync("Neaktivita");
                kickedUsers.Append($"`{user}`");

                _cachedUsers.Remove(user.Id);
            }
        }

        await _logChannel.SendMessageAsync(kickedUsers.ToString());
    }
}