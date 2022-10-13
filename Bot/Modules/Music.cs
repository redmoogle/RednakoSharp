using Discord;
using Discord.Interactions;
using RednakoSharp.Helpers;
using Victoria;
using Victoria.Enums;
using Victoria.Filters;
using Victoria.Responses.Search;

namespace RednakoSharp.Modules
{
    public sealed class Music : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService? Commands { get; set; }
        private readonly LavaNode _lavaNode;
        // Constructor injection is also a valid way to access the dependencies
        public Music(LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
        }

        [EnabledInDm(false)]
        [SlashCommand("play", "Play some music")]
        public async Task PlayTask([Summary(description: "URL or search term")] string search)
        {
            IVoiceState? voiceState = Context.User as IVoiceState;
            IVoiceChannel? uservc = voiceState?.VoiceChannel;

            if (voiceState == null || uservc == null)
            {
                await RespondAsync("You are not connected to a voice channel", ephemeral: true);
                return;
            }

            if (!_lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer tryplayer))
            {
                await _lavaNode.JoinAsync(uservc, Context.Channel as ITextChannel);
            }

            LavaPlayer player = tryplayer ?? _lavaNode.GetPlayer(Context.Guild);
            IVoiceChannel playervc = player.VoiceChannel;

            if (playervc != null && playervc != uservc)
            {
                await RespondAsync("I'm already connected to another voice channel.", ephemeral: true);
                return;
            }

            var searchResponse = Uri.IsWellFormedUriString(search, UriKind.Absolute) ?
                    await _lavaNode.SearchAsync(SearchType.Direct, search)
                    : await _lavaNode.SearchAsync(SearchType.YouTube, search);
            if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
            {
                await RespondAsync($"I wasn't able to find anything for `{search}`.", ephemeral: true);
                return;
            }

            if (searchResponse.Status == SearchStatus.PlaylistLoaded)
            {
                player.Queue.Enqueue(searchResponse.Tracks);
                await RespondAsync($"Enqueued {searchResponse.Tracks.Count} songs.");
            }
            else
            {
                LavaTrack track = searchResponse.Tracks.First();
                player.Queue.Enqueue(track);
                await RespondAsync($"Enqueued {track?.Title}");
            }

            if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                return;
            }

            player.Queue.TryDequeue(out var lavaTrack);
            await player.PlayAsync(x =>
            {
                x.Track = lavaTrack;
                x.ShouldPause = false;
                x.StartTime = TimeSpan.FromMilliseconds(10);
            });
        }

        [EnabledInDm(false)]
        [SlashCommand("stop", "Stop playing music")]
        public async Task StopTask()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
                return;
            }
            IVoiceChannel voiceChannel = player.VoiceChannel;

            await player.StopAsync();
            await _lavaNode.LeaveAsync(voiceChannel);
            await RespondAsync("Stopped the player.");
        }

        [EnabledInDm(false)]
        [SlashCommand("playing", "Get currently playing song")]
        public async Task PlayingTask()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
                return;
            }

            if(player.Track != null)
            {
                RespondAsync(embed: await Embeds.TrackEmbed(player.Track));
            }
            else
            {
                await RespondAsync("Nothing is playing.", ephemeral: true);
                return;
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("skip", "Skip the current song")]
        public async Task SkipTask()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
                return;
            }

            if (player.Track != null)
            {
                await RespondAsync("Skipped " + player.Track.Title);
                await player.SkipAsync();
            }
            else
            {
                await RespondAsync("Nothing is playing.", ephemeral: true);
                return;
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("bass", "Adjust the bass in %")]
        public async Task BassTask([MinValue(0)][MaxValue(500)] double percentage = 0)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
                return;
            }
            double percentageAdj = (percentage - 100) / 400; // Converts to a -0.25 to 1.0 range
            if(percentageAdj == 0.0)
            {
                Console.WriteLine("AAAAAAAAAAAAAA");
                EqualizerBand[] arr =
                {
                    new EqualizerBand(0, 0.01),
                    new EqualizerBand(1, 0.01),
                    new EqualizerBand(2, 0.01),
                    new EqualizerBand(3, 0.01),
                    new EqualizerBand(4, 0.01)
                };
                player.EqualizerAsync(arr);
            }
            else
            {
                EqualizerBand[] arr =
                {
                    new EqualizerBand(0, percentageAdj * 0.25),
                    new EqualizerBand(1, percentageAdj * 0.5),
                    new EqualizerBand(2, percentageAdj),
                    new EqualizerBand(3, percentageAdj * 0.5),
                    new EqualizerBand(4, percentageAdj * 0.25)
                };
                player.EqualizerAsync(arr);
            }
            await RespondAsync("Set Bass Gain to " + percentage.ToString() + "%");
        }

        [EnabledInDm(false)]
        [SlashCommand("mids", "Adjust the mid in %")]
        public async Task MidTask([MinValue(0)][MaxValue(500)] double percentage = 0)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
                return;
            }
            double percentageAdj = (percentage - 100) / 400; // Converts to a -0.25 to 1.0 range
            if (percentageAdj == 0.0)
            {
                EqualizerBand[] arr =
                {
                    new EqualizerBand(5, 0.01),
                    new EqualizerBand(6, 0.01),
                    new EqualizerBand(7, 0.01),
                    new EqualizerBand(8, 0.01),
                    new EqualizerBand(9, 0.01)
                };
                player.EqualizerAsync(arr);
            }
            else
            {
                EqualizerBand[] arr =
                {
                    new EqualizerBand(5, percentageAdj * 0.25),
                    new EqualizerBand(6, percentageAdj * 0.5),
                    new EqualizerBand(7, percentageAdj),
                    new EqualizerBand(8, percentageAdj * 0.5),
                    new EqualizerBand(9, percentageAdj * 0.25)
                };
                player.EqualizerAsync(arr);
            }
            await RespondAsync("Set Mid Gain to " + percentage.ToString() + "%");
        }

        [EnabledInDm(false)]
        [SlashCommand("treble", "Adjust the treble in %")]
        public async Task TrebleTask([MinValue(0)][MaxValue(500)] double percentage = 0)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
                return;
            }
            double percentageAdj = (percentage - 100) / 400; // Converts to a -0.25 to 1.0 range
            if (percentageAdj == 0.0)
            {
                EqualizerBand[] arr =
                {
                    new EqualizerBand(10, 0.01),
                    new EqualizerBand(11, 0.01),
                    new EqualizerBand(12, 0.01),
                    new EqualizerBand(13, 0.01),
                    new EqualizerBand(14, 0.01)
                };
                player.EqualizerAsync(arr);
            }
            else
            {
                EqualizerBand[] arr =
                {
                    new EqualizerBand(10, percentageAdj * 0.25),
                    new EqualizerBand(11, percentageAdj * 0.5),
                    new EqualizerBand(12, percentageAdj),
                    new EqualizerBand(13, percentageAdj * 0.5),
                    new EqualizerBand(14, percentageAdj * 0.25)
                };
                player.EqualizerAsync(arr);
            }
            await RespondAsync("Set Treble Gain to " + percentage.ToString() + "%");
        }
    }
}
