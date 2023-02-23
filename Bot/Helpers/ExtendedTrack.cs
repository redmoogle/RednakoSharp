using Discord.WebSocket;
using Victoria.Player;

namespace RednakoSharp.Helpers
{
    public class ExtendedTrack : LavaTrack
    {
        public SocketUser Requester { get; }

        public ExtendedTrack(LavaTrack track, SocketUser requester) : base(track)
        {
            Requester = requester;
        }
    }
}
