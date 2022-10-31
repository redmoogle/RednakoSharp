using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RednakoSharp.Helpers;
using System.Diagnostics;
using System.Reflection;
using Victoria;
using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;

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
        private readonly IServiceCollection _collection;

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

                AppDomain.CurrentDomain.ProcessExit += new EventHandler(LavalinkClose);

                Console.WriteLine("Starting local version of lavalink");
                StreamReader stdout = lavaprocess.StandardOutput;
                while (true)
                {
                    string? line = stdout.ReadLine();

                    if (line == null) continue;

                    /// Lavalink has a better message to indicate it's up but this works
                    /// Because for some reason stdout doesnt include the message (maybe stderr? but I cant hook into it)
                    if(line.Contains("Undertow started on port")) {
                        Console.WriteLine("Started local version of lavalink");
                        break;
                    }
                    Thread.Sleep(100);
                }
            }

            _collection = new ServiceCollection()
                .AddLogging()
                .AddSingleton(_configuration)
                .AddSingleton(_socketConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton<LavaNode>()
                .AddSingleton<NodeConfiguration>()
                .AddLavaNode(x =>
                {
                    x.SelfDeaf = true;
                    x.Hostname = _lavacfg.GetValue<string>("lavalink:address");
                    x.Port = _lavacfg.GetValue<ushort>("lavalink:port");
                    x.Authorization = _lavacfg.GetValue<string>("lavalink:password");
                });
            _services = _collection.BuildServiceProvider();

        }

        static void Main()
        {
            Task Program = new Program().RunAsync();
            Program.GetAwaiter().GetResult();
        }

        public void LavalinkClose(object? sender, EventArgs e)
        {
            lavaprocess?.Close();
        }

        public async Task RunAsync()
        {
            DiscordSocketClient client = _services.GetRequiredService<DiscordSocketClient>();
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
            if (!node.IsConnected)
            {
                node.ConnectAsync();
                node.OnTrackStart += TrackStart;
            }
        }

        public static async Task TrackStart(TrackStartEventArg<LavaPlayer<LavaTrack>, LavaTrack> eventArg) // I cant just put this directly into the music module due to the constructor being ran twice(wtf??)
        {
            LavaTrack track = eventArg.Track;
            Embed embed = await Embeds.TrackEmbed(track);
            eventArg.Player.TextChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogAsync(LogMessage message)
            => Console.WriteLine(message);
    }
}
