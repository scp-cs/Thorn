using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using thorn.Services;
using thorn.UserAccounts;

namespace thorn.Modules
{
    [Group("profile")]
    [Alias("p", "profil")]
    [RequireContext(ContextType.Guild)]
    public class ProfileModule : ModuleBase<SocketCommandContext>
    {
        private readonly UserAccountsService _userAccounts;
        private readonly ILogger<ProfileModule> _logger;
        private readonly PairsService _pairs;

        public ProfileModule(UserAccountsService userAccounts, ILogger<ProfileModule> logger, PairsService pairs)
        {
            _userAccounts = userAccounts;
            _logger = logger;
            _pairs = pairs;
        }

        [Command]
        public async Task ProfileCommand([Remainder] SocketGuildUser user = null)
        {
            SocketGuildUser target;
            if (user != null)
                target = user;
            else
                target = Context.User as SocketGuildUser;

            var profile = await _userAccounts.GetAccountAsync(target);

            var embed = new EmbedBuilder
            {
                Title = string.IsNullOrEmpty(profile[AccountItem.WikidotUsername])
                    ? target?.Nickname ?? target?.Username
                    : $"{target?.Nickname ?? target?.Username} - {profile[AccountItem.WikidotUsername]}",
                ThumbnailUrl = target?.GetAvatarUrl(),
                Description = profile.ToString()
            };

            if (profile[AccountItem.ProfileColor] != null)
                embed.Color = new Color(Convert.ToUInt32(profile[AccountItem.ProfileColor].Remove(0, 1), 16));

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }
        
        [Command("set")]
        [Alias("s")]
        public async Task SetCommand(AccountItem accountItem, [Remainder] string value)
        {
            var account = await _userAccounts.GetAccountAsync(Context.User);
            if (!IsSafe(account, accountItem, value)) return;
            
            account[accountItem] = value;

            await _userAccounts.SaveAccountsAsync();
            await Context.Message.AddReactionAsync(Emote.Parse(_pairs.GetString("YES_EMOTE")));
            _logger.LogInformation($"{Context.User} changed their profile setting {accountItem} to: {value}");
        }

        [Command("remove")]
        [Alias("r")]
        public async Task RemoveCommand(AccountItem accountItem)
        {
            var profile = await _userAccounts.GetAccountAsync(Context.User);

            profile[accountItem] = null;

            await _userAccounts.SaveAccountsAsync();
            await Context.Message.AddReactionAsync(Emote.Parse(_pairs.GetString("YES_EMOTE")));
            _logger.LogInformation($"{Context.User} removed their profile {accountItem}");
        }

        private static bool IsSafe(UserAccount userAccount, AccountItem accountItem, string value)
        {
            switch (accountItem)
            {
                case AccountItem.Description:
                    return !(value.Length > 100);
                
                case AccountItem.AuthorPage: case AccountItem.TranslatorPage:
                    if (!IsWikiLink(value)) return false;
                    UpdatePrivatePage(userAccount, value);
                    return true;

                case AccountItem.PrivatePage: case AccountItem.Sandbox:
                    return IsWikiLink(value);

                case AccountItem.ProfileColor:
                    var match = Regex.Match(value, @"^#[0-9A-F]{6}$", RegexOptions.IgnoreCase);
                    return match.Success;
                
                case AccountItem.WikidotUsername:
                    // No check here
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(accountItem), accountItem, null);
            }

            return true;
        }
        
        private static bool IsWikiLink(string link) => 
            Regex.Match(link, @"^http:\/\/(scp-cs(-sandbox|)\.wikidot\.com|scp-wiki\.cz)\/[^\s]*$").Success;

        // If translator and author pages are the same, set PrivatePage so only that shows on the profile
        private static void UpdatePrivatePage(UserAccount userAccount, string value)
        {
            if (userAccount[AccountItem.TranslatorPage] == userAccount[AccountItem.AuthorPage])
                userAccount[AccountItem.PrivatePage] = value;
        }
    }
}