using System.Globalization;
using Discord.Interactions;
using Discord;
using Newtonsoft.Json.Linq;
using RednakoSharp.Helpers;

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
                await RespondAsync("Maximum is larger than the minimum.", ephemeral: true);
                return;
            }
            await RespondAsync(_random.NextInt64(minimum, maximum).ToString(CultureInfo.InvariantCulture));
        }

        [SlashCommand("meme", "Pulls up a bad meme.")]
        public async Task BadMeme()
        {
            JObject obj = await HttpHelper.HttpApiRequest(new Uri("https://some-random-api.ml/meme"));

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
            JObject obj = await HttpHelper.HttpApiRequest(new Uri("https://some-random-api.ml/joke"));

            Embed embed = new EmbedBuilder()
                .WithDescription((string?)obj["joke"])
                .Build();

            await RespondAsync(embed: embed);
        }
    }
}
