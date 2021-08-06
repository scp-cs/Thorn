using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using thorn.Services;
using thorn.UserAccounts;

namespace thorn.Modules
{
    [Group("points")]
    public class AssignPointsModule : ModuleBase<SocketCommandContext>
    {
        private readonly ulong[] _authorizedUsers;
        private readonly WebClient _webClient;

        private readonly UserAccountsService _accounts;
        private readonly ConstantsService _constants;
        private readonly ILogger<AssignPointsModule> _logger;
        private readonly DiscordSocketClient _client;

        public AssignPointsModule(UserAccountsService accounts, ConstantsService constants, ILogger<AssignPointsModule> logger, DiscordSocketClient client)
        {
            _accounts = accounts;
            _constants = constants;
            _logger = logger;
            _client = client;

            _webClient = new WebClient();
            
            // Kubík, Hajgrando, and H3G1
            _authorizedUsers = new ulong[] {408675740633268226, 213659030244163585, 688157416407302161};
        }

        [Command]
        public async Task AssignPointsCommand(PointType type)
        {
            if (!_authorizedUsers.Contains(Context.User.Id)) return;
            
            // ReSharper disable once MethodHasAsyncOverload
            _webClient.DownloadFile(new Uri(Context.Message.Attachments.First().Url), "points");
            var file = await File.ReadAllLinesAsync("points");

            string typeString;
            string pattern;

            switch (type)
            {
                case PointType.Translation:
                    pattern = "([0-9]{18}),Bodů,(\"[0-9]*,[0-9]*|[0-9]*)";
                    typeString = "Překladatelské";
                    File.Move("points", "translation-points", true);
                    break;
                case PointType.Writing:
                    pattern = "([0-9]{18}),([0-9]*)";
                    typeString = "Spisovatelské";
                    File.Move("points", "writing-points", true);
                    break;
                case PointType.Correction:
                    pattern = "([0-9]{18});([0-9]*(,[0-9]*|))";
                    typeString = "Korektorské";
                    File.Move("points", "correction-points", true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            var matches = file.Select(s => Regex.Match(s, pattern)).ToList();
            var changes = new StringBuilder($"**{typeString.ToUpper()} BODY**\n\n");
            var numOfChanges = 0;
            
            foreach (var match in matches.Where(match => match.Success))
            {
                ulong id;
                float points;
                
                try
                {
                    id = Convert.ToUInt64(match.Groups[1].Value);
                    points = SanitizePointValue(match.Groups[2].Value);
                }
                catch (InvalidOperationException)
                {
                    // Row was formatted weirdly or data couldn't be converted 
                    // Either way we continue
                    continue;
                }
                
                var account = await _accounts.GetAccountAsync(id);
                if (type == PointType.Translation && account.Id == 227114285074087938) points = 999999; // Utylike
                
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (account.Points[type] == points) continue;

                changes.Append($"`{account.Id}` {account.Points[type]} -> {points}\n");
                account.Points[type] = points;
                numOfChanges++;
            }
            
            changes.Append($"\n{typeString} body aktualizovány u {numOfChanges} lidí. Díky, " +
                           $"{Context.User.Username} {_constants.Strings.emote.agrlove}");

            var message = changes.ToString();
            
            if (numOfChanges == 0)
                message = "Nejspíš si mi nahrál špatný soubor. Podívej se a zkus to znovu!";
            
            _logger.LogInformation("Updated points: {Message}", message);
            await _accounts.UpdateRanks(type);

            await ReplyAsync(message);

            if (!(_client.GetChannel(_constants.Channels["o5"]) is SocketTextChannel o5) 
                || numOfChanges == 0) return;
            await o5.SendMessageAsync(message);
        }

        private static float SanitizePointValue(string point)
        {
            if (point.Last() == ',') point = point.Remove(point.Length - 1);
            point = point.Replace(",", ".").Replace("\"", "");
            return Convert.ToSingle(point);
        }
    }
}