using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace thorn.UserAccounts;

public class PointTypeTypeReader : TypeReader
{
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
    {
        if (Enum.TryParse(input, out PointType result))
            return Task.FromResult(TypeReaderResult.FromSuccess(result));
        else
            switch (input)
            {
                case "t":
                case "p":
                case "překlad":
                case "preklad":
                    return Task.FromResult(TypeReaderResult.FromSuccess(PointType.Translation));
                case "w":
                case "s":
                case "psaní":
                case "psani":
                case "spisovatel":
                case "spisovatelské":
                case "spisovatelske":
                    return Task.FromResult(TypeReaderResult.FromSuccess(PointType.Writing));
                case "c":
                case "k":
                case "korekce":
                    return Task.FromResult(TypeReaderResult.FromSuccess(PointType.Correction));
                default:
                    return Task.FromResult(TypeReaderResult.FromError(
                        CommandError.ParseFailed, "Input could not be parsed as a PointType."));
            }
    }
}