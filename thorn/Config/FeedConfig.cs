namespace thorn.Config;

public class FeedConfig
{
    public string Link { get; set; }
    public ulong[] ChannelIds { get; set; }
    public string[] Filter { get; set; }
    public string CustomDescription { get; set; }
    public bool NewPageAnnouncement { get; set; }
    public uint EmbedColor { get; set; }
}