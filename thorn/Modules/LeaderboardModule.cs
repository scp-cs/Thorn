using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using thorn.Services;
using thorn.UserAccounts;

namespace thorn.Modules
{
    [Group("leaderboard")]
    [Alias("l")]
    [RequireContext(ContextType.Guild)]
    public class LeaderboardModule : ModuleBase<SocketCommandContext>
    {
        private readonly UserAccountsService _userAccounts;
        private readonly PairsService _pairs;
        private readonly int _leaderboardSize;

        public LeaderboardModule(UserAccountsService userAccounts, PairsService pairs)
        {
            _userAccounts = userAccounts;
            _pairs = pairs;
            _leaderboardSize = 15;
        }
        
        [Command]
        public async Task LeaderboardCommand(int page = 1) =>
            await PostLeaderboard(_userAccounts.GetLeaderboard(), page);

        [Command]
        public async Task TypeLeaderboardCommand(PointType type, int page = 1) =>
            await PostLeaderboard(_userAccounts.GetTypeLeaderboard(type), page, type);

        private async Task PostLeaderboard(IEnumerable<UserAccount> accounts, int page, PointType? type = null)
        {
            var range = SplitList(accounts, _leaderboardSize);
            
            if (page < 1 || page > range.Count) page = 1;
            var leaderboard = new StringBuilder();

            foreach (var account in range[page - 1].Select((value, index) => new {value, index}))
            {
                var user = Context.Guild.GetUser(account.value.Id);
                
                // Skip to the next iteration if the user is no longer on the server
                if (user == null) {
                    continue;
                }
                
                // No PointType defined, use the whole leaderboard
                if (type == null)
                {
                    if (account.value.Id != 227114285074087938)
                        leaderboard.Append(
                            $"**`{(page - 1) * _leaderboardSize + account.index + 1}.`** {user.Nickname ?? user.Username} - " +
                            $"**{account.value.Points.Sum(x => x.Value)}**\n");
                    else // Uty
                        leaderboard.Append(
                            $"**`{(page - 1) * _leaderboardSize + account.index + 1}.`** {user.Nickname ?? user.Username} - " +
                            "**ano**\n");
                }
                // PointType defined, use only that leaderboard
                else
                {
                    if (account.value.Id != 227114285074087938 || type != PointType.Translation)
                        leaderboard.Append(
                            $"**`{(page - 1) * _leaderboardSize + account.index + 1}.`** {user.Nickname ?? user.Username} - " +
                            $"**{account.value.Points[type.GetValueOrDefault()]}**\n");
                    else // Uty
                        leaderboard.Append(
                            $"**`{(page - 1) * _leaderboardSize + account.index + 1}.`** {user.Nickname ?? user.Username} - " +
                            "**ano**\n");
                }
            }
            
            var title = type == null
                ? _pairs.GetString("LEADERBOARD_TITLE")
                : _pairs.GetString($"LEADERBOARD_{type.ToString()?.ToUpper()}_TITLE");
            
            await ReplyAsync(embed: new EmbedBuilder
            {
                Title = title,
                Description = leaderboard.ToString(),
                Footer = new EmbedFooterBuilder().WithText($"Strana {page} z {range.Count}"),
                Color = Color.Blue
            }.Build());
        }

        private static List<List<T>> SplitList<T>(IEnumerable<T> list, int chunk)
        {
            return list.Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunk)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}
