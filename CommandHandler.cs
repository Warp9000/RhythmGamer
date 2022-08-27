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
            _client.ButtonExecuted += HandleButtonAsync;
            await _commands.AddModulesAsync(assembly: System.Reflection.Assembly.GetEntryAssembly(),
                                            services: null);

            Program.Commands = _commands.Commands.Count();
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
        private async Task HandleButtonAsync(Discord.WebSocket.SocketMessageComponent component)
        {
            try
            {
                switch (component.Data.CustomId)
                {
                    default:
                        await component.RespondAsync("Unknown button");
                        return;
                }
            }
            catch (Exception ex)
            {
                l.Error(ex.StackTrace!, "HelpButtonHandler");
            }
        }
    }
}