using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

namespace thorn.Services
{
    public class ScpService
    {
        private readonly GraphQLHttpClient _graphQlClient;
        private readonly PairsService _pairs;

        public ScpService(PairsService pairs)
        {
            _pairs = pairs;
            _graphQlClient = new GraphQLHttpClient("https://api.crom.avn.sh/", new NewtonsoftJsonSerializer());
        }

        private async Task<List<ArticleData>> GetArticleData(string query)
        {
            var request = new GraphQLHttpRequest
            {
                Query = $"{{searchPages(query: \"{query}\") {{url wikidotInfo {{title rating}} " +
                        $"alternateTitles {{title}} attributions {{user {{name}}}}}}}}",
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
                    description.Append($"**{czech.WikidotInfo.Title}** - [překlad]({czech.Url}) | [originál]({english.Url})\n");
                else if (czech != null)
                    description.Append($"**{czech.WikidotInfo.Title}** - [odkaz]({czech.Url})\n");
                else
                    description.Append($"**{english.WikidotInfo.Title}** - [originál]({english.Url})\n");
            }

            return new EmbedBuilder().WithDescription(description.ToString()).Build();
        }

        public async Task<Embed> GetEmbedForArticle(string query)
        {
            var articles = await GetArticleData(query);

            if (articles is null) return new EmbedBuilder().WithDescription("Tento článek jsem nenašel :(").Build();

            var czech = articles.FirstOrDefault(x => Regex.Match(x.Url, @"scp-cs\.wikidot\.com").Success);
            var english = articles.FirstOrDefault(x => Regex.Match(x.Url, @"scp-wiki\.wikidot\.com").Success);
            
            if (czech == null && english == null) return new EmbedBuilder().WithDescription("Tento článek jsem nenašel :(").Build();

            string title, description;
            List<EmbedFieldBuilder> fieldBuilders;
            EmbedFooterBuilder footerBuilder;

            if (czech != null && english != null)
            {
                description = $"**Autor: `{english.Attributions.First().User.Name}`**\n" +
                              $"**Překladatel: `{czech.Attributions.First().User.Name}`**";
                
                title = czech.AlternateTitles.FirstOrDefault() == null 
                    ? czech.WikidotInfo.Title 
                    : $"{czech.WikidotInfo.Title} - {czech.AlternateTitles.First().Title}";
                
                fieldBuilders = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder().WithName("Překlad").WithValue($"[link]({czech.Url})").WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Originál").WithValue($"[link]({english.Url})").WithIsInline(true)
                };

                footerBuilder = new EmbedFooterBuilder().WithText($"Hodnocení překladu: {czech.WikidotInfo.Rating}");
            }
            else if (czech != null)
            {
                description = $"**Autor: `{czech.Attributions.First().User.Name}`**";
                
                title = czech.AlternateTitles.FirstOrDefault() == null 
                    ? czech.WikidotInfo.Title 
                    : $"{czech.WikidotInfo.Title} - {czech.AlternateTitles.First().Title}";

                fieldBuilders = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder().WithName("Odkaz").WithValue($"[link]({czech.Url})").WithIsInline(true)
                };
                
                footerBuilder = new EmbedFooterBuilder().WithText($"Hodnocení: {czech.WikidotInfo.Rating}");
            }
            else
            {
                description = $"**Autor: `{english.Attributions.First().User.Name}`**\n" +
                              $"Zatím nepřeloženo {_pairs.GetString("SAD_EMOTE")}";
                
                title = english.AlternateTitles.FirstOrDefault() == null 
                    ? english.WikidotInfo.Title 
                    : $"{english.WikidotInfo.Title} - {english.AlternateTitles.First().Title}";
                
                fieldBuilders = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder().WithName("Odkaz").WithValue($"[link]({english.Url})").WithIsInline(true)
                };
                
                footerBuilder = new EmbedFooterBuilder().WithText($"Hodnocení: {english.WikidotInfo.Rating}");
            }

            return new EmbedBuilder
            {
                Title = title,
                Description = description,
                Color = Color.Blue,
                Fields = fieldBuilders,
                Footer = footerBuilder
            }.Build();
        }
    }
}