using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RednakoSharp.Helpers;
using Victoria;
using Victoria.EventArgs;

#pragma warning disable CS1998 // No sync operation

namespace RednakoSharp
{
    public class Program
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;

        private readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            AlwaysDownloadUsers = true,
        };

        public Program()
        {
            ConfigurationBuilder cfg = new();
            cfg.AddEnvironmentVariables(prefix: "discord");
            cfg.AddJsonFile("appsettings.json");
            _configuration = cfg.Build();

            ServiceCollection srv = new();
            srv.AddSingleton(_configuration);
            srv.AddSingleton(_socketConfig);
            srv.AddSingleton<DiscordSocketClient>();
            srv.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
            srv.AddSingleton<InteractionHandler>();
            srv.AddSingleton<LavaNode>();
            srv.AddSingleton<LavaConfig>();

            srv.AddLavaNode(x =>
            {
                x.SelfDeaf = true;
            });

            _services = srv.BuildServiceProvider();
        }

        static void Main()
            => new Program().RunAsync()
                .GetAwaiter()
                .GetResult();

        public async Task RunAsync()
        {
            var client = _services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;
            client.Ready += OnReadyAsync;

            // Here we can initialize the service that will register and execute our commands
            await _services.GetRequiredService<InteractionHandler>()
                .InitializeAsync();

            // Bot token can be provided from the Configuration object we set up earlier
            await client.LoginAsync(TokenType.Bot, _configuration.GetValue<string>("discord:token"));
            await client.StartAsync();

            // Never quit the program until manually forced to.
            await Task.Delay(Timeout.Infinite);
        }

        public async Task OnReadyAsync()
        {
            Console.WriteLine("Connection Ready");
            LavaNode node = _services.GetRequiredService<LavaNode>();
            node.OnLog += LogAsync;
            if (!node.IsConnected)
            {
                node.ConnectAsync();
                node.OnTrackStarted += TrackStart;
            }
        }
        public static async Task TrackStart(TrackStartEventArgs trackargs) // I cant just put this directly into the music module due to the constructor being ran twice(wtf??)
        {
            LavaTrack track = trackargs.Track;
            Embed embed = await Embeds.TrackEmbed(track);
            trackargs.Player.TextChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogAsync(LogMessage message)
            => Console.WriteLine(message.ToString());
    }
}
