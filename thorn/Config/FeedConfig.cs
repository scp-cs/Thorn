namespace thorn.Config
{
    public class FeedConfig
    {
        public string Link { get; set; }
        public ulong[] ChannelIds { get; set; }
        public string CustomDescription { get; set; }
        public uint EmbedColor { get; set; }
        public bool RequireAuth { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}