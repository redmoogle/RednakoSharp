using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RednakoSharp.Helpers;
using System.Diagnostics;
using System.Reflection;
using Victoria;
using Victoria.EventArgs;

#pragma warning disable CS1998 // No sync operation

namespace RednakoSharp
{
    public class Program : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly IConfiguration _localservices;
        private readonly IConfiguration _lavacfg;
        private readonly IServiceProvider _services;

        private readonly Process? lavaprocess;

        private readonly string path;

        private readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            AlwaysDownloadUsers = true,
        };

        public void Dispose()
        {
            if (lavaprocess != null)
            {
                lavaprocess.Close();
            }
            Dispose();
            GC.SuppressFinalize(this);
        }

        public Program()
        {
            path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            /// Local Service Configuration
            _localservices = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "services")
                .AddJsonFile("appsettings.json")
                .Build();

            /// Modules Configuration
            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "discord")
                .AddJsonFile("appsettings.json")
                .Build();

            _lavacfg = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "discord")
                .AddJsonFile("appsettings.json")
                .Build();

            ServiceCollection srv = new();
            srv.AddSingleton(_configuration);
            srv.AddSingleton(_socketConfig);
            srv.AddSingleton<DiscordSocketClient>();
            srv.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
            srv.AddSingleton<InteractionHandler>();
            srv.AddSingleton<LavaNode>();
            srv.AddSingleton<LavaConfig>();

            if(_localservices.GetValue<bool>("services:useLocalLavalink"))
            {
                lavaprocess = Process.Start("java", "-jar " + path + "/lavalink.jar");
                Console.WriteLine("Starting local version of lavalink");
                Thread.Sleep(4000); // Wait for startup
            }

            srv.AddLavaNode(x =>
            {
                x.SelfDeaf = true;
                x.Hostname = _lavacfg.GetValue<string>("lavalink:address");
                x.Port = _lavacfg.GetValue<ushort>("lavalink:port");
                x.Authorization = _lavacfg.GetValue<string>("lavalink:password");
                x.LogSeverity = LogSeverity.Error;
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
