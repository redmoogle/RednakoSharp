using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using RednakoSharp.Helpers;

namespace RednakoSharp.Modules
{
    public sealed class User : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService? Commands { get; set; }
        private readonly InteractionHandler _handler;

        private readonly Random _random = new();
        private readonly string[] _animals = { "cat", "dog", "koala", "fox", "bird", "red_panda", "panda", "racoon", "kangaroo" };

        // Constructor injection is also a valid way to access the dependencies
        public User(InteractionHandler handler)
        {
            _handler = handler;
        }

        [SlashCommand("ping", "Pings the bot and returns its latency.")]
        public async Task GreetUserAsync()
        {
            await RespondAsync(text: $":ping_pong: It took me {Context.Client.Latency}ms to respond to you!");
        }

        // TODO: Add Owner/Github Fields
        [SlashCommand("botinfo", "Queries info about the bot")]
        public async Task BotInfoAsync()
        {
            DiscordSocketClient client = Context.Client;

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Bot Information")
                .WithThumbnailUrl(client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl())
                .AddField("Owner", client.GetUser(_handler.GetValueAsUlong("owner")).ToString())
                .AddField("Servers", client.Guilds.Count)
                .AddField("Members", client.Guilds.Sum(guild => guild.MemberCount));

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("serverinfo", "Queries info about the server")]
        public async Task ServerInfoAsync()
        {
            SocketGuild guild = Context.Guild;
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Server Information")
                .WithThumbnailUrl(guild.BannerUrl)
                .AddField("Owner", guild.Owner, true)
                .AddField("ID", guild.Id, true)
                .AddField("Categories", guild.CategoryChannels.Count, true)
                .AddField("Channels", guild.Channels.Count, true)
                .AddField("Roles", guild.Roles.Count, true)
                .AddField("Members", guild.MemberCount, true);

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("avatar", "Get a avatar")]
        public async Task AvatarTask([Summary(description: "Get avatar of a specific user")] SocketUser? user = null)
        {
            user ??= Context.User;

            string avatar = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
            await RespondAsync(embed: new EmbedBuilder() { Title = user.Username, ImageUrl = avatar }.Build());
        }

        [SlashCommand("animalfacts", "Get some facts about animals")]
        public async Task AnimalFactTask()
        {
            await RespondAsync(components: SelectorComponents.AnimalMenu("fact"));
        }

        [ComponentInteraction("fact")]
        internal async Task AnimalFactInteractTask(string[] selectedAnimal)
        {
            string animal = selectedAnimal[0];

            if (animal == "random")
            {
                animal = _animals[_random.Next(0, _animals.Length)];
            }

            await DeferAsync();

            JObject json = await HttpHelper.HttpApiRequest(new Uri("https://some-random-api.ml/facts/" + animal));

            EmbedBuilder embed = new()
            {
                Title = General.Proper(animal) + " Fact",
                Description = (string?)json["fact"]
            };

            await ModifyOriginalResponseAsync(props => { props.Components = new ComponentBuilder().Build(); props.Embeds = new[] { embed.Build() }; });
        }

        [SlashCommand("animalimage", "Get some facts about animals")]
        public async Task AnimalImageTask()
        {
            await RespondAsync(components: SelectorComponents.AnimalMenu("image"));
        }

        [ComponentInteraction("image")]
        internal async Task AnimalImageInteractTask(string[] selectedAnimal)
        {
            string animal = selectedAnimal[0];

            if (animal == "random")
            {
                animal = _animals[_random.Next(0, _animals.Length)];
            }

            await DeferAsync().ConfigureAwait(false);
            JObject json = await HttpHelper.HttpApiRequest(new Uri("https://some-random-api.ml/img/" + animal));
            EmbedBuilder embed = new()
            {
                Title = General.Proper(animal) + " Image",
                ImageUrl = (string?)json["link"]
            };

            await ModifyOriginalResponseAsync(props => { props.Components = new ComponentBuilder().Build(); props.Embeds = new[] { embed.Build() }; });
        }
    }
}
