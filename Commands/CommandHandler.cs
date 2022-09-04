using System.Collections.Generic;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace RhythmGamer
{
    public class CommandHandler
    {
        public readonly Discord.WebSocket.DiscordSocketClient _client;
        public static InteractionService _InteractionService;

        public CommandHandler(Discord.WebSocket.DiscordSocketClient client)
        {
            _client = client;
            _InteractionService = new(_client);
        }

        public async Task InstallCommandsAsync()
        {
            l.Verbose("Starting...", "CommandHandler");
            _client.Ready += () =>
            {
                Task.Run(RegisterCommands);
                return Task.CompletedTask;
                l.Debug("Added RegisterCommands to ready handler", "InstallCommands");
            };
            await _InteractionService.AddModulesAsync(assembly: System.Reflection.Assembly.GetExecutingAssembly(),
                                            services: null);

            l.Debug(_InteractionService.Modules.Count() + " modules loaded:", "InstallCommands");
            foreach (var module in _InteractionService.Modules)
            {
                l.Debug(module.Name, "InstallCommands");
            }
            l.Debug(_InteractionService.SlashCommands.Count + " commands loaded:", "InstallCommands");
            foreach (var command in _InteractionService.SlashCommands)
            {
                l.Debug(command.Name, "InstallCommands");
            }
            _client.InteractionCreated += HandleInteraction;
        }

        private async Task RegisterCommands()
        {
            foreach (var guild in _client.Guilds)
            {
                await _InteractionService.RegisterCommandsToGuildAsync(guild.Id, true);
                l.Debug("Added commands to " + guild.Id, "RegisterCommands");
            }
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new SocketInteractionContext(_client, interaction);

                // Execute the incoming command.
                var result = await _InteractionService.ExecuteCommandAsync(context, null);

                if (!result.IsSuccess)
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            // implement
                            break;
                        default:
                            break;
                    }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}