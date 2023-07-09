using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using RednakoSharp.Helpers;
using System.Globalization;
using Victoria.Node;
using Victoria.Player;
using Victoria.Player.Filters;
using Victoria.Responses.Search;

namespace RednakoSharp.Modules
{
    public sealed class Music : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService? Commands { get; set; }
        private readonly LavaNode _lavaNode;
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<ulong, PageComponent> Pages = new();
        // Constructor injection is also a valid way to access the dependencies
        public Music(LavaNode lavaNode, DiscordSocketClient client)
        {
            _lavaNode = lavaNode;
            _client = client;

            _client.ButtonExecuted += OnButtonInteraction;
        }

        private async Task OnButtonInteraction(SocketMessageComponent component)
        {
            if(component.GuildId == null)
            {
                return;
            }
            if (!Pages.TryGetValue((ulong)component.GuildId, out var PageComponent))
            {
                return;
            }

            Tuple<Embed, MessageComponent> message;
            var originalResponse = await component.GetOriginalResponseAsync();
            switch (component.Data.CustomId) {
                case "decrease":
                    message = PageComponent.DecrementPage();
                    await component.UpdateAsync(props => { props.Components = message.Item2; props.Embeds = new[] { message.Item1 }; });
                    break;
                case "increase":
                    message = PageComponent.IncrementPage();
                    await component.UpdateAsync(props => { props.Components = message.Item2; props.Embeds = new[] { message.Item1 }; });
                    break;
            }
        }

        [EnabledInDm(false)]
        [SlashCommand("play", "Play some music")]
        public async Task PlayTask([Summary(description: "URL or search term")] string search)
        {
            IVoiceState? voiceState = Context.User as IVoiceState;
            IVoiceChannel? voiceChannel = voiceState?.VoiceChannel;

            await DeferAsync(true);

            if (voiceState == null || voiceChannel == null)
            {
                await ModifyOriginalResponseAsync(props => { props.Content = "You are not connected to a voice channel"; });
                return;
            }

            if (!_lavaNode.TryGetPlayer(Context.Guild, out LavaPlayer<LavaTrack> player))
            {
                await _lavaNode.JoinAsync(voiceChannel, Context.Channel as ITextChannel); 
                _lavaNode.TryGetPlayer(Context.Guild, out player);
            }
            IVoiceChannel? playerVoiceChannel = player.VoiceChannel;

            if (playerVoiceChannel != null && playerVoiceChannel != voiceChannel)
            {
                await ModifyOriginalResponseAsync(props => { props.Content = "I'm already connected to another voice channel."; });
                return;
            }

            var searchResponse = Uri.IsWellFormedUriString(search, UriKind.Absolute) ?
                    await _lavaNode.SearchAsync(SearchType.Direct, search)
                    : await _lavaNode.SearchAsync(SearchType.YouTube, search);
            if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
            {
                await ModifyOriginalResponseAsync(props => { props.Content = $"I wasn't able to find anything for `{search}`."; });
                return;
            }

            if (searchResponse.Status == SearchStatus.PlaylistLoaded)
            {
                foreach(LavaTrack track in searchResponse.Tracks)
                {
                    ExtendedTrack extendedTrack = new(track, Context.User);
                    player.Vueue.Enqueue(extendedTrack);
                    //player.Vueue.Enqueue(track);
                }
                await ModifyOriginalResponseAsync(props => { props.Content = $"Enqueued {searchResponse.Tracks.Count} songs."; });
            }
            else
            {
                LavaTrack track = searchResponse.Tracks.First();
                ExtendedTrack extendedTrack = new(track, Context.User);
                player.Vueue.Enqueue(extendedTrack);
                //player.Vueue.Enqueue(track);
                await ModifyOriginalResponseAsync(props => { props.Content = $"Enqueued {track.Title}"; });
            }

            if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                return;
            }

            player.Vueue.TryDequeue(out var lavaTrack);
            await player.PlayAsync(lavaTrack);
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

            //ExtendedTrack track = (ExtendedTrack)player.Track;

            RespondAsync(embed: await Embeds.TrackEmbed(player.Track));
        }

        [EnabledInDm(false)]
        [SlashCommand("queue", "List the queue")]
        public async Task QueueTask()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
                return;
            }

            PageComponent page = new(player.Vueue, Context);
            if(Pages.ContainsKey(Context.Guild.Id))
            {
                Pages[Context.Guild.Id] = page;
            } else
            {
                Pages.Add(Context.Guild.Id, page);
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
                EqualizerBand[] arr =
                {
                    new EqualizerBand(0, 0),
                    new EqualizerBand(1, 0),
                    new EqualizerBand(2, 0),
                    new EqualizerBand(3, 0),
                    new EqualizerBand(4, 0)
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
            await RespondAsync("Set Bass Gain to " + percentage.ToString(CultureInfo.InvariantCulture) + "%");
        }

        [EnabledInDm(false)]
        [SlashCommand("mid", "Adjust the mid in %")]
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
                    new EqualizerBand(5, 0),
                    new EqualizerBand(6, 0),
                    new EqualizerBand(7, 0),
                    new EqualizerBand(8, 0),
                    new EqualizerBand(9, 0)
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
            await RespondAsync("Set Mid Gain to " + percentage.ToString(CultureInfo.InvariantCulture) + "%");
        }

        [EnabledInDm(false)]
        [SlashCommand("treble", "Adjust the treble in %")]
        public async Task TrebleTask([MinValue(0)][MaxValue(500)]double percentage = 0)
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
                    new EqualizerBand(10, 0),
                    new EqualizerBand(11, 0),
                    new EqualizerBand(12, 0),
                    new EqualizerBand(13, 0),
                    new EqualizerBand(14, 0)
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
            await RespondAsync("Set Treble Gain to " + percentage.ToString(CultureInfo.InvariantCulture) + "%");
        }

        [EnabledInDm(false)]
        [SlashCommand("volume", "Adjust volume")]
        public async Task VolumeTask([MinValue(0)][MaxValue(1000)] int percentage = 0)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
                return;
            }
            await player.SetVolumeAsync(percentage);
            await RespondAsync("Set volume to " + percentage.ToString(CultureInfo.InvariantCulture) + "%");
        }
    }
}
