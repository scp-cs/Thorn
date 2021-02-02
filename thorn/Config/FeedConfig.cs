namespace thorn.Config
{
    // TODO: Custom embed color for each feed
    public struct FeedConfig
    {
        public string Link { get; set; }
        public ulong ChannelId { get; set; }
        public string CustomDescription { get; set; }
        public uint EmbedColor { get; set; }
        public bool RequireAuth { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}