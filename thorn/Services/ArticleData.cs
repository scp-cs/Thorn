using System.Collections.Generic;
// Instantiated by the Json library
// ReSharper disable ClassNeverInstantiated.Global

namespace thorn.Services
{
    public class ResponseType
    {
        public List<ArticleData> SearchPages { get; set; }
    }
    
    public class ArticleData
    {
        public string Url { get; set; }
        public WikidotInfo WikidotInfo { get; set; }
        public List<AlternateTitle> AlternateTitles { get; set; }
        public List<Attribution> Attributions { get; set; }
    }

    public class WikidotInfo
    {
        public string Title { get; set; }
        public int Rating { get; set; }
        // public string ThumbnailUrl { get; set; }
    }

    public class AlternateTitle
    {
        // public string Type { get; set; }
        public string Title { get; set; }
    }

    public class Attribution
    {
        // public string Type { get; set; }
        public User User { get; set; }
    }

    public class User
    {
        public string Name { get; set; }
    }
}