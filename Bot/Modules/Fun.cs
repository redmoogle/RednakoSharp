using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json.Linq;
using RednakoSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace RednakoSharp.Modules
{
    public sealed class Fun : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService? Commands { get; set; }
        private readonly Random _random = new();

        [SlashCommand("rng", "Generates a random number.")]
        public async Task NumberGen(long minimum = 0, long maximum = 100)
        {
            if(maximum < minimum)
            {
                await RespondAsync("Maximum is larger than the minmum.", ephemeral: true);
                return;
            }

            // Scroll through 20 numbers
            await RespondAsync(_random.NextInt64(minimum, maximum).ToString());
            for(var i = 0; i < 5; i++)
            {
                await ModifyOriginalResponseAsync(props => { props.Content = _random.NextInt64(minimum, maximum).ToString(); });
                Thread.Sleep(100);
            }
        }

        [SlashCommand("meme", "Pulls up a bad meme.")]
        public async Task BadMeme()
        {
            JObject obj = await Http.HttpAPIRequest("https://some-random-api.ml/meme");

            Embed embed = new EmbedBuilder()
                .WithDescription((string?)obj["caption"])
                .WithImageUrl((string?)obj["image"])
                .WithFooter(new EmbedFooterBuilder().WithText("Category: " + (string?)obj["category"]))
                .Build();

            await RespondAsync(embed: embed);

        }

        [SlashCommand("joke", "Parrots a bad joke.")]
        public async Task BadJoke()
        {
            JObject obj = await Http.HttpAPIRequest("https://some-random-api.ml/joke");

            Embed embed = new EmbedBuilder()
                .WithDescription((string?)obj["joke"])
                .Build();

            await RespondAsync(embed: embed);
        }
    }
}
