using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RednakoSharp.Helpers;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using Victoria;
using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;

#pragma warning disable CS1998 // No sync operation

namespace RednakoSharp
{
    internal sealed class Program : IDisposable
    {
        /// <summary>
        /// Discord Configuration
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Services modules can pull from
        /// </summary>
        private readonly IServiceProvider _services;

        /// <summary>
        /// Represents running lavalink process
        /// </summary>
        private readonly Process? _lavaprocess;

        private readonly CancellationTokenSource _delaySource = new();
        private readonly CancellationToken _delayToken;

        private readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            LogGatewayIntentWarnings = false,
            AlwaysDownloadUsers = true,
        };

        private Program()
        {
            _delayToken = _delaySource.Token;
            // As far as my C# understanding goes GetDirectoryName can return null but this code *should* never
            // return null or something has gone terribly wrong.
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            // Local Services Configuration
            IConfiguration localservices = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "services")
                .AddJsonFile("appsettings.json")
                .Build();

            // Discord.Net Configuration
            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "discord")
                .AddJsonFile("appsettings.json")
                .Build();

            // Lavalink/Victoria Configuration
            IConfiguration lavaconfiguration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "lavalink")
                .AddJsonFile("appsettings.json")
                .Build();

            if (localservices.GetValue<bool>("services:useLocalLavalink"))
            {
                _lavaprocess = new Process();
                _lavaprocess.StartInfo.FileName = "java";
                _lavaprocess.StartInfo.Arguments = "-jar " + path + "/Lavalink.jar";
                _lavaprocess.StartInfo.UseShellExecute = false;
                _lavaprocess.Start();

                AppDomain.CurrentDomain.ProcessExit += LavalinkClose;


                Console.WriteLine("Starting local version of lavalink");
                while (true)
                {
                    IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                    IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();

                    if(tcpEndPoints.Any(p => p.Port == lavaconfiguration.GetValue<ushort>("lavalink:port")))
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
                Console.WriteLine("Started Lavalink");
            }

            IServiceCollection collection = new ServiceCollection()
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
                    x.Hostname = lavaconfiguration.GetValue<string>("lavalink:address");
                    x.Port = lavaconfiguration.GetValue<ushort>("lavalink:port");
                    x.Authorization = lavaconfiguration.GetValue<string>("lavalink:password");
                });
            _services = collection.BuildServiceProvider();

        }

        static void Main()
        {
            var bot = new Program();
            
            
            Task program = bot.RunAsync();
            program.GetAwaiter().GetResult();
            
            bot.Dispose();
        }

        private void LavalinkClose(object? sender, EventArgs e)
        {
            Dispose();
            _lavaprocess?.Close();
        }

        private async Task RunAsync()
        {
            DiscordSocketClient client = _services.GetRequiredService<DiscordSocketClient>();
            client.Log += LogAsync;
            client.Ready += OnReadyAsync;

            // Here we can initialize the service that will register and execute our commands
            await _services.GetRequiredService<InteractionHandler>()
                .InitializeAsync();

            // Bot token can be provided from the Configuration object we set up earlier
            var token = _configuration.GetValue<string>("discord:token");
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // Never quit the program until manually forced to.
            await Task.Delay(Timeout.Infinite, _delayToken);
        }

        private async Task OnReadyAsync()
        {
            Console.WriteLine("Connection Ready");
            LavaNode node = _services.GetRequiredService<LavaNode>();
            if (!node.IsConnected)
            {
                node.ConnectAsync();
                node.OnTrackStart += TrackStart;
            }
        }

        private static async Task TrackStart(TrackStartEventArg<LavaPlayer<LavaTrack>, LavaTrack> eventArg) // I cant just put this directly into the music module due to the constructor being ran twice(wtf??)
        {
            LavaTrack track = eventArg.Track;
            Embed embed = await Embeds.TrackEmbed(track);
            eventArg.Player.TextChannel.SendMessageAsync(embed: embed);
        }

        private static async Task LogAsync(LogMessage message)
            => Console.WriteLine(message);

        public void Dispose()
        {
            if(_lavaprocess != null)
            {
                _lavaprocess.Close();
                _lavaprocess.Dispose();
            }
            DiscordSocketClient client = _services.GetRequiredService<DiscordSocketClient>();
            client.StopAsync().GetAwaiter().GetResult();
            _delaySource.Dispose();
            Environment.Exit(0);
        }
    }
}
