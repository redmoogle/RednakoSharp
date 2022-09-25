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
            EmbedBuilder embed = new()
            {
                Title = "Bot Information",
                ThumbnailUrl = client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl()
            };

            embed.AddField("Owner", client.GetUser(_handler.GetValueAsUlong("owner")).ToString());
            embed.AddField("Servers", client.Guilds.Count);

            long Count = 0;
            foreach (SocketGuild guild in client.Guilds)
            {
                Count += guild.MemberCount;
            }

            embed.AddField("Members", Count);
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("serverinfo", "Queries info about the server")]
        public async Task ServerInfoAsync()
        {
            SocketGuild guild = Context.Guild;
            EmbedBuilder embed = new()
            {
                Title = "Server Information",
                ThumbnailUrl = guild.BannerUrl
            };
            embed.AddField("Owner", guild.Owner, true);
            embed.AddField("ID", guild.Id, true);
            embed.AddField("Categories", guild.CategoryChannels.Count, true);
            embed.AddField("Channels", guild.Channels.Count, true);
            embed.AddField("Roles", guild.Roles.Count, true);
            embed.AddField("Members", guild.MemberCount, true);
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("avatar", "Get a avatar")]
        public async Task AvatarTask([Summary(description: "Get avatar of a specific user")] SocketUser? user = null)
        {
            if (user == null)
            {
                string avatar = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl();
                await RespondAsync(embed: new EmbedBuilder() { Title = Context.User.Username, ImageUrl = avatar }.Build());
            }
            else
            {
                string avatar = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
                await RespondAsync(embed: new EmbedBuilder() { Title = user.Username, ImageUrl = avatar }.Build());
            }
        }

        [SlashCommand("animalfacts", "Get some facts about animals")]
        public async Task AnimalFactTask()
        {
            await RespondAsync(components: SelectorComponents.AnimalMenu("fact"));
        }

        [ComponentInteraction("fact")]
        public async Task AnimalFactInteractTask(string[] selectedAnimal)
        {
            string animal = selectedAnimal[0];

            if (animal == "random" || animal == null)
            {
                string[] options = new string[] { "cat", "dog", "koala", "fox", "bird", "red_panda", "panda", "racoon", "kangaroo" };
                Random random = new();
                animal = options[random.Next(0, options.Length)];
            }

            await DeferAsync();

            JObject json = await Http.HttpAPIRequest("https://some-random-api.ml/facts/" + animal);

            EmbedBuilder embed = new()
            {
                Title = char.ToUpper(animal[0]) + animal[1..] + " Fact",
                Description = (string?)json["fact"]
            };

            await ModifyOriginalResponseAsync(props => { props.Components = new ComponentBuilder().Build(); props.Embeds = new Embed[] { embed.Build() }; });
        }

        [SlashCommand("animalimage", "Get some facts about animals")]
        public async Task AnimalImageTask()
        {
            await RespondAsync(components: SelectorComponents.AnimalMenu("image"));
        }

        [ComponentInteraction("image")]
        public async Task AnimalImageInteractTask(string[] selectedAnimal)
        {
            string animal = selectedAnimal[0];

            if (animal == "random" || animal == null)
            {
                string[] options = new string[] { "cat", "dog", "koala", "fox", "bird", "red_panda", "panda", "racoon", "kangaroo" };
                Random random = new();
                animal = options[random.Next(0, options.Length)];
            }

            await DeferAsync();
            JObject json = await Http.HttpAPIRequest("https://some-random-api.ml/img/" + animal);
            EmbedBuilder embed = new()
            {
                Title = char.ToUpper(animal[0]) + animal[1..] + " Image",
                ImageUrl = (string?)json["link"]
            };

            await ModifyOriginalResponseAsync(props => { props.Components = new ComponentBuilder().Build(); props.Embeds = new Embed[] { embed.Build() }; });
        }
    }
}
