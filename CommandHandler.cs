using Discord;
using Discord.Commands;

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
    }
}