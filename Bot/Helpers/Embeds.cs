using Discord;
using Victoria;

namespace RednakoSharp.Helpers
{
    public sealed class Embeds
    {
        public async static Task<Embed> TrackEmbed(LavaTrack track)
        {
            EmbedBuilder eb = new()
            {
                Description = "```css\n" + track.Title + "\n```",
                Title = "Now Playing: ",
                ThumbnailUrl = await track.FetchArtworkAsync()
            };

            eb.AddField("Song: ", "[" + track.Title + "](" + track.Url + ")");
            TimeSpan duration = track.Duration;

            string durationFormatted = "";
            string positionFormatted = "";

            if(duration.TotalHours >= 1)
            {
                durationFormatted = track.Duration.ToString("hh\\:mm\\:ss");
                positionFormatted = track.Position.ToString("hh\\:mm\\:ss");
            }
            else if (duration.TotalMinutes >= 1 || duration.TotalSeconds >= 1)
            {
                durationFormatted = track.Duration.ToString("mm\\:ss");
                positionFormatted = track.Position.ToString("mm\\:ss");
            }
            eb.AddField("Duration: ", positionFormatted + "/" + durationFormatted);
            eb.AddField("By: ", track.Author);
            return eb.Build();
        }
    }
}
