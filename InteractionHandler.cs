﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using RednakoSharp.Helpers;
using System.Reflection;

namespace RednakoSharp
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _handler;
        public readonly RednakoConfig configuration;
        private readonly IServiceProvider _services;

        public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services, RednakoConfig config)
        {
            _client = client;
            _handler = handler;
            _services = services;
            configuration = config;
        }

        public async Task InitializeAsync()
        {
            // Process when the client is ready, so we can register our commands.
            _client.Ready += ReadyAsync;
            _handler.Log += LogAsync;

            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Process the InteractionCreated payloads to execute Interactions commands
            _client.InteractionCreated += HandleInteraction;
        }

        #pragma warning disable CS1998 // This is pure async no sync here
        private static async Task LogAsync(LogMessage log)
            => Console.WriteLine(log);

        private async Task ReadyAsync()
        {
            // Context & Slash commands can be automatically registered, but this process needs to happen after the client enters the READY state.
            // Since Global Commands take around 1 hour to register, we should use a test guild to instantly update and test our commands.
            #if DEBUG
            await _handler.RegisterCommandsToGuildAsync(_configuration.GetValue<ulong>("discord:testGuild"));
            #else
            await _handler.RegisterCommandsGloballyAsync();
            #endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Bot shouldn't die")]
        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new SocketInteractionContext(_client, interaction);

                // Execute the incoming command.
                var result = await _handler.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            // implement
                            break;
                        case InteractionCommandError.UnknownCommand:
                            break;
                        case InteractionCommandError.ConvertFailed:
                            break;
                        case InteractionCommandError.BadArgs:
                            break;
                        case InteractionCommandError.Exception:
                            break;
                        case InteractionCommandError.Unsuccessful:
                            break;
                        case InteractionCommandError.ParseFailed:
                            break;
                    }
            }
            catch
            {
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                {
                    var originalResponse = await interaction.GetOriginalResponseAsync();
                    await originalResponse.DeleteAsync();
                }
            }
        }
    }
}
