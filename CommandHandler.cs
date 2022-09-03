using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace RhythmGamer
{
    public class CommandHandler
    {
        public readonly Discord.WebSocket.DiscordSocketClient _client;
        public static CommandService _commands = new();

        public CommandHandler(Discord.WebSocket.DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            l.Verbose("Starting...", "CommandHandler");
            _client.MessageReceived += HandleCommandAsync;
            _client.Ready += CreateSlashCommands;
            _client.SlashCommandExecuted += SlashCommandHandler;
            await _commands.AddModulesAsync(assembly: System.Reflection.Assembly.GetExecutingAssembly(),
                                            services: null);

            Program.Commands = _commands.Commands.Count();
            l.Debug(_commands.Modules.Count() + " modules loaded:", "InstallCommands");
            foreach (var module in _commands.Modules)
            {
                l.Debug(module.Name, "InstallCommands");
            }
            l.Debug(Program.Commands + " commands loaded:", "InstallCommands");
            foreach (var command in _commands.Commands)
            {
                l.Debug(command.Name, "InstallCommands");
            }
        }

        private async Task HandleCommandAsync(Discord.WebSocket.SocketMessage messageParam)
        {
            var message = messageParam as Discord.WebSocket.SocketUserMessage;
            if (message == null) return;

            int argPos = 0;
            if (message.Author.IsBot)
                return;
            if (!message.HasStringPrefix(Program.Config.prefix, ref argPos) &&
                !message.HasStringPrefix(Program.GetServerConfig(new SocketCommandContext(_client, message).Guild.Id).prefix, ref argPos) &&
                !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            l.Verbose($"\"{messageParam.Content}\" sent by {messageParam.Author.Username + "#" + messageParam.Author.Discriminator} in #{messageParam.Channel.Name}({messageParam.Channel.Id})", "CommandHandler");

            var context = new SocketCommandContext(_client, message);

            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);
        }
        private async Task CreateSlashCommands()
        {
            try
            {
                List<SlashCommandBuilder> _cmds = new();
                foreach (var module in _commands.Modules)
                {
                    var guildCommand = new SlashCommandBuilder();
                    guildCommand.WithName(module.Group).WithDescription(module.Summary ?? "");
                    foreach (var command in module.Commands)
                    {
                        var scob = new SlashCommandOptionBuilder()
                            .WithName(command.Name)
                            .WithDescription(command.Summary ?? "?")
                            .WithType(ApplicationCommandOptionType.SubCommand);
                        foreach (var param in command.Parameters)
                        {
                            scob.AddOption(new SlashCommandOptionBuilder()
                            .WithName(param.Name ?? "Unknown")
                            .WithRequired(!param.IsOptional)
                            .WithDescription(param.Summary ?? "?")
                            .WithType(ApplicationCommandOptionType.String)
                            .AddChannelType(ChannelType.Text)
                            .WithDefault(false));

                        }
                        guildCommand.AddOption(scob);
                    }
                    _cmds.Add(guildCommand);
                }
                foreach (var guild in _client.Guilds)
                {
                    foreach (var cmd in _cmds)
                    {
                        try
                        {
                            await guild.CreateApplicationCommandAsync(cmd.Build());
                        }
                        catch (Exception ex)
                        {
                            l.Error("", "CreateSlashCommands", ex);
                        }
                    }
                }
                _client.Ready -= CreateSlashCommands;
                return;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {

        }
    }
}