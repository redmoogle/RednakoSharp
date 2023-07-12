using Discord;
using System.Globalization;
using Victoria;
using Victoria.Player;

namespace RednakoSharp.Helpers
{
    public static class Embeds
    {
        public static async Task<Embed> TrackEmbed(LavaTrack track)
        {
            if (track == null)
            {
                throw new ArgumentNullException(paramName: nameof(track), message: "Track data is missing");
            }
            EmbedBuilder eb = new()
            {
                Description = "```css\n" + track.Title + "\n```",
                Title = "Now Playing: ",
                ThumbnailUrl = await track.FetchArtworkAsync()
            };

            eb.AddField("Song: ", "[" + track.Title + "](" + track.Url + ")");
            TimeSpan duration = track.Duration;

            string durationFormatted;
            string positionFormatted;

            if(duration.TotalHours >= 1)
            {
                durationFormatted = track.Duration.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture);
                positionFormatted = track.Position.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture);
            }
            else
            {
                durationFormatted = track.Duration.ToString("mm\\:ss", CultureInfo.InvariantCulture);
                positionFormatted = track.Position.ToString("mm\\:ss", CultureInfo.InvariantCulture);
            }
            eb.AddField("Duration: ", positionFormatted + "/" + durationFormatted);
            eb.AddField("By: ", track.Author);
            //eb.AddField("Requester: ", track.Requester);
            return eb.Build();
        }
    }
}
