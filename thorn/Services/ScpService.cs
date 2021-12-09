using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

namespace thorn.Services;

public class ScpService
{
    private readonly GraphQLHttpClient _graphQlClient;
    private readonly ConstantsService _constants;

    public ScpService(ConstantsService constants)
    {
        _constants = constants;
        _graphQlClient = new GraphQLHttpClient("https://api.crom.avn.sh/", new NewtonsoftJsonSerializer());
    }

    private async Task<List<ArticleData>> GetArticleData(string query)
    {
        var request = new GraphQLHttpRequest
        {
            Query = $"{{searchPages(query: \"{query}\") {{url wikidotInfo{{title}}}}}}"
        };

        var response = await _graphQlClient.SendQueryAsync<ResponseType>(request);
        return response.Data.SearchPages;
    }

    public async Task<Embed> GetEmbedForReference(List<string> queries)
    {
        var description = new StringBuilder();

        foreach (var query in queries)
        {
            var articles = await GetArticleData(query);
            if (articles is null) continue;

            var czech = articles.FirstOrDefault(x => Regex.Match(x.Url, @"scp-cs\.wikidot\.com").Success);
            var english = articles.FirstOrDefault(x => Regex.Match(x.Url, @"scp-wiki\.wikidot\.com").Success);

            if (czech == null && english == null) continue;

            if (czech != null && english != null)
                description.Append(
                    $"**{czech.WikidotInfo.Title}** - [překlad]({czech.Url}) | [originál]({english.Url})\n");
            else if (czech != null)
                description.Append($"**{czech.WikidotInfo.Title}** - [odkaz]({czech.Url})\n");
            else
                description.Append($"**{english.WikidotInfo.Title}** - [originál]({english.Url})\n");
        }

        return new EmbedBuilder().WithDescription(description.ToString()).Build();
    }
}