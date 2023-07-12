namespace RednakoSharp.Helpers
{
    public class RednakoConfig
    {
        public string discordToken = null!;
        public string repository = null!;
        public string inviteLink = null!;
        public long ownerId;
        public long testGuildId;

        public bool localLavalink;

        public int lavaPort;
        public string lavaIp = null!;
        public string lavaPass = null!;
    }
}
