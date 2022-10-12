using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RednakoSharp.Helpers;
using System;
using System.Diagnostics;
using System.Reflection;
using Victoria;
using Victoria.EventArgs;

#pragma warning disable CS1998 // No sync operation

namespace RednakoSharp
{
    internal sealed class Program
    {
        /// <summary>
        /// Discord Configuration
        /// </summary>
        private readonly IConfiguration _configuration;
        /// <summary>
        /// Enabled Services Configuration
        /// </summary>
        private readonly IConfiguration _localservices;
        /// <summary>
        /// Lavalink Configuration
        /// </summary>
        private readonly IConfiguration _lavacfg;
        /// <summary>
        /// Services modules can pull from
        /// </summary>
        private readonly IServiceProvider _services;

        /// <summary>
        /// Represents running lavalink process
        /// </summary>
        private readonly Process? lavaprocess;

        /// <summary>
        /// Path to executing file
        /// </summary>
        private readonly string path;

        private readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            LogGatewayIntentWarnings = false,
            AlwaysDownloadUsers = true,
        };

        public Program()
        {
            /// As far as my C# understanding goes GetDirectoryName can return null but this code *should* never
            /// return null or something has gone terribly wrong.
            path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            /// Local Services Configuration
            _localservices = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "services")
                .AddJsonFile("appsettings.json")
                .Build();

            /// Discord.Net Configuration
            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "discord")
                .AddJsonFile("appsettings.json")
                .Build();

            /// Lavalink/Victoria Configuration
            _lavacfg = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "discord")
                .AddJsonFile("appsettings.json")
                .Build();

            if (_localservices.GetValue<bool>("services:useLocalLavalink"))
            {
                lavaprocess = new Process();
                lavaprocess.StartInfo.FileName = "java";
                lavaprocess.StartInfo.Arguments = "-jar " + path + "/lavalink.jar";
                lavaprocess.StartInfo.RedirectStandardOutput = true;
                lavaprocess.StartInfo.UseShellExecute = false;
                lavaprocess.Start();
                Console.WriteLine("Starting local version of lavalink");
                /// TODO: refactor this to wait for "Lavalink is ready to accept connections."
                Thread.Sleep(4000); // Wait for startup
            }

            _services = new ServiceCollection()
                .AddSingleton(_configuration)
                .AddSingleton(_socketConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton<LavaNode>()
                .AddSingleton<LavaConfig>()
                .AddLavaNode(x =>
                {
                    x.SelfDeaf = true;
                    x.Hostname = _lavacfg.GetValue<string>("lavalink:address");
                    x.Port = _lavacfg.GetValue<ushort>("lavalink:port");
                    x.Authorization = _lavacfg.GetValue<string>("lavalink:password");
                    x.LogSeverity = LogSeverity.Error;
                })
                .BuildServiceProvider();

        }

        static void Main()
        {
            new Program().RunAsync()
                .GetAwaiter()
                .GetResult();
        }

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
