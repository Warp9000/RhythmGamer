using Discord;
using Discord.Interactions;
using Newtonsoft.Json;
using System.Text;

namespace RhythmGamer
{
    public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("info", "Get info about the bot")]
        public async Task Info()
        {
            var e = Program.DefaultEmbed();
            e.Title = "RhythmGamer";
            e.Description = "By Warp#8703";
            e.ThumbnailUrl = Program.embedIconUrl;
            // e.Url = "https://warp.tf";
            e.AddField("Links", "[Discord](https://discord.gg/7GtmSe7npQ)\n[Source Code](https://github.com/WarpABoi/RhythmGamer)");
            await RespondAsync(embed: e.Build());
        }
        [SlashCommand("feedback", "Sends some feedback to Warp")]
        public async Task Feedback([Summary("Feedback", "The feedback you want to give")] string feedback)
        {
            var warp = Context.Client.GetUser(Program.Warp);
            var dm = await warp.CreateDMChannelAsync();
            var e = Program.DefaultEmbed();
            e.Title = "Feedback";
            e.Description = feedback;
            e.Author = new EmbedAuthorBuilder()
            {
                Name = Context.User.Username + "#" + Context.User.Discriminator,
                IconUrl = Context.User.GetAvatarUrl()
            };
            await dm.SendMessageAsync("", false, e.Build());
            await RespondAsync("Sent!", ephemeral: true);
        }
    }
}