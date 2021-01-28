using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace thorn.UserAccounts
{
    public class AccountItemTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (Enum.TryParse(input, out AccountItem result))
                return Task.FromResult(TypeReaderResult.FromSuccess(result));
            else
                switch (input)
                {
                    case "wikidot": case "wikidot-username": case "w":
                        return Task.FromResult(TypeReaderResult.FromSuccess(AccountItem.WikidotUsername));
                    case "d":
                        return Task.FromResult(TypeReaderResult.FromSuccess(AccountItem.Description));
                    case "author": case "author-page": case "ap":
                        return Task.FromResult(TypeReaderResult.FromSuccess(AccountItem.AuthorPage));
                    case "translator": case "translator-page": case "tp":
                        return Task.FromResult(TypeReaderResult.FromSuccess(AccountItem.TranslatorPage));
                    case "private": case "private-page": case "pp":
                        return Task.FromResult(TypeReaderResult.FromSuccess(AccountItem.PrivatePage));
                    case "s":
                        return Task.FromResult(TypeReaderResult.FromSuccess(AccountItem.Sandbox));
                    case "color": case "profile-color": case "c":
                        return Task.FromResult(TypeReaderResult.FromSuccess(AccountItem.ProfileColor));
                    default:
                        return Task.FromResult(TypeReaderResult.FromError(
                            CommandError.ParseFailed, "Input could not be parsed as a AccountItem."));
                }
        }
    }
}