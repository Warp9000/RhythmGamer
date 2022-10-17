using Discord;
using Discord.Interactions;
using Newtonsoft.Json;
using System.Text;


namespace RhythmGamer
{
    [Group("quaver", "Quaver related commands")]
    public class QuaverModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("setuser", "Set your default Quaver user")]
        public async Task SetUser([Summary("Username")] string username, [Summary("Gamemode")] QuaverInternal.QuaverMode mode = QuaverInternal.QuaverMode.Key4)
        {
            try
            {
                var user = (await QuaverAPI.API.SearchUsersAsync(username)).First();
                if (user == null)
                {
                    await RespondAsync("User not found");
                    return;
                }
                if (Program.UserConfigs.Exists(x => x.id == Context.User.Id))
                {
                    Program.UserConfigs.Find(x => x.id == Context.User.Id)!.quaver.username = user.username;
                    Program.UserConfigs.Find(x => x.id == Context.User.Id)!.quaver.mode = mode;
                }
                else
                {
                    UserConfig userc = new()
                    {
                        id = Context.User.Id,
                        quaver = new()
                        {
                            username = user.username,
                            mode = mode
                        }
                    };
                    Program.UserConfigs.Add(userc);
                }
                var embed = Program.DefaultEmbed()
                    .WithAuthor(user.username, $"https://static.quavergame.com/img/flags/{user.country}.png")
                    .WithDescription($"Set your default Quaver user to **{user.username}**")
                    .WithColor(Color.Blue)
                    .WithThumbnailUrl(user.avatarUrl)
                    .Build();
                await RespondAsync(embed: embed);
            }
            catch (Exception ex)
            {
                l.Critical(ex.Message, "SetUser", ex);
                await RespondAsync("An error occured");
            }
        }

        [SlashCommand("profile", "Get a Quaver users profile")]
        public async Task Profile([Summary("Username")] string? username = null, [Summary("Gamemode")] QuaverInternal.QuaverMode? mode = null)
        {
            try
            {
                int userId = 0;
                if (username == null)
                {
                    var userConfig = Program.GetUserConfig(Context.User.Id);
                    if (userConfig.quaver.username == null)
                    {
                        await RespondAsync("You have not set a default Quaver user and you did not provide one");
                        return;
                    }
                    else
                    {
                        username = userConfig.quaver.username;
                        if (mode == null)
                            mode = userConfig.quaver.mode ?? QuaverInternal.QuaverMode.Key4;
                    }
                }
                if (!int.TryParse(username, out userId))
                {
                    var userSearch = (await QuaverAPI.API.SearchUsersAsync(username)).First();
                    if (userSearch == null)
                    {
                        await RespondAsync("User not found");
                        return;
                    }
                    userId = userSearch.id;
                }
                var user = await QuaverAPI.API.GetUserAsync(userId);
                if (user == null)
                {
                    await RespondAsync("User not found");
                    return;
                }
                if (mode == null)
                    mode = QuaverInternal.QuaverMode.Key4;
                var userMode = mode == QuaverInternal.QuaverMode.Key4 ? user.keys4 : user.keys7;
                var embed = Program.DefaultEmbed()
                    .WithAuthor(user.info.username, $"https://static.quavergame.com/img/flags/{user.info.country}.png")
                    .WithDescription($"[Profile](https://quavergame.com/user/{user.info.id})")
                    .WithColor(Color.Blue)
                    .WithThumbnailUrl(user.info.avatarUrl)
                    .AddField("Rank", $"**Global**\t#{userMode.globalRank.ToString("N0")}\n**Country**\t#{userMode.countryRank.ToString("N0")} {user.info.country}", true)
                    .AddField("Stats", $"**Rating**\t{userMode.stats.rating.ToString("N2")}\n**Accuracy**\t{userMode.stats.accuracy.ToString("N2")}%\n**Max Combo**\t{userMode.stats.maxCombo.ToString("N0")}", true)
                    .AddField("Score", $"**Total**\t{userMode.stats.totalScore.ToString("N0")}\n**Ranked**\t{userMode.stats.rankedScore.ToString("N0")}\n**Play Count**\t{userMode.stats.playCount.ToString("N0")}", true)
                    .AddField("Multiplayer", $"**Wins**\t{userMode.stats.multiplayerWins.ToString("N0")}\n**Losses**\t{userMode.stats.multiplayerLosses.ToString("N0")}\n**Wins Rank**\t#{userMode.multiplayerWinRank.ToString("N0")}", true)
                    .Build();
                await RespondAsync(embed: embed);
            }
            catch (Exception ex)
            {
                l.Critical(ex.Message, "Profile", ex);
                await RespondAsync("An error occured");
            }
        }

        [SlashCommand("recent", "Get a Quaver users recent plays")]
        public async Task Recent([Summary("Username")] string? username = null, [Summary("Gamemode")] QuaverInternal.QuaverMode? mode = null)
        {
            try
            {
                int userId = 0;
                if (username == null)
                {
                    var userConfig = Program.GetUserConfig(Context.User.Id);
                    if (userConfig.quaver.username == null)
                    {
                        await RespondAsync("You have not set a default Quaver user and you did not provide one");
                        return;
                    }
                    else
                    {
                        username = userConfig.quaver.username;
                        if (mode == null)
                            mode = userConfig.quaver.mode ?? QuaverInternal.QuaverMode.Key4;
                    }
                }
                if (!int.TryParse(username, out userId))
                {
                    var userSearch = (await QuaverAPI.API.SearchUsersAsync(username)).First();
                    if (userSearch == null)
                    {
                        await RespondAsync("User not found");
                        return;
                    }
                    userId = userSearch.id;
                }
                var user = await QuaverAPI.API.GetUserAsync(userId);
                if (user == null)
                {
                    await RespondAsync("User not found");
                    return;
                }
                if (mode == null)
                    mode = QuaverInternal.QuaverMode.Key4;
                var scores = await QuaverAPI.API.GetUserRecentAsync(userId, (int)mode!);
                if (scores.Count == 0)
                {
                    await RespondAsync("No recent plays found");
                    return;
                }
                var score = scores.First();
                var embed = Program.DefaultEmbed()
                    .WithAuthor($"{score.map.title} [{score.map.difficultyName}]", user.info.avatarUrl)
                    .WithDescription($"[Map](https://quavergame.com/mapsets/map/{score.map.id})")
                    .WithColor(Color.Blue)
                    .WithThumbnailUrl($"https://cdn.quavergame.com/mapsets/{score.map.id}.jpg")
                    .AddField("Score", $"**Score**\t{score.totalScore.ToString("N0")}\n**Accuracy**\t{score.accuracy.ToString("N2")}%\n**Max Combo**\t{score.maxCombo.ToString("N0")}\n**P-Rating**\t{score.rating.ToString("N2")}", true)
                    .AddField("Stats", $"**Mods**\t{score.modsString}\n**Scroll Sp.**\t{(score.scrollSpeed / 10).ToString("N1")}\n**Grade**\t{score.grade}", true)
                    .AddField("Judgements", $"**Marv**\t{score.countMarvellous.ToString("N0")}\n**Perf**\t{score.countPerfect.ToString("N0")}\n**Great**\t{score.countGreat.ToString("N0")}\n**Good**\t{score.countGood.ToString("N0")}\n**Okay**\t{score.countOkay.ToString("N0")}\n**Miss**\t{score.countMiss.ToString("N0")}", true)
                    .Build();
                await RespondAsync(embed: embed);
            }
            catch (Exception ex)
            {
                l.Critical(ex.Message, "Recent", ex);
                await RespondAsync("An error occured");
            }
        }

        [SlashCommand("top", "Get a Quaver users top plays")]
        public async Task Top([Summary("Username")] string? username = null, [Summary("Gamemode")] QuaverInternal.QuaverMode? mode = null, [Summary("page")] int page = 0)
        {
            try
            {
                int userId = 0;
                if (username == null)
                {
                    var userConfig = Program.GetUserConfig(Context.User.Id);
                    if (userConfig.quaver.username == null)
                    {
                        await RespondAsync("You have not set a default Quaver user and you did not provide one");
                        return;
                    }
                    else
                    {
                        username = userConfig.quaver.username;
                        if (mode == null)
                            mode = userConfig.quaver.mode ?? QuaverInternal.QuaverMode.Key4;
                    }
                }
                if (!int.TryParse(username, out userId))
                {
                    var userSearch = (await QuaverAPI.API.SearchUsersAsync(username)).First();
                    if (userSearch == null)
                    {
                        await RespondAsync("User not found");
                        return;
                    }
                    userId = userSearch.id;
                }
                var user = await QuaverAPI.API.GetUserAsync(userId);
                if (user == null)
                {
                    await RespondAsync("User not found");
                    return;
                }
                if (mode == null)
                    mode = QuaverInternal.QuaverMode.Key4;
                var response = await QuaverAPI.API.ApiCallAsync($"/users/scores/best?id={userId}&mode={(int)mode!}&limit=5&page={page}");
                var scores = JsonConvert.DeserializeObject<QuaverInternal.qscores>(response)!.scores;
                if (scores.Count == 0)
                {
                    await RespondAsync("No plays found");
                    return;
                }
                Embed[] embeds = new Embed[5];
                for (int i = 0; i < Math.Min(scores.Count, 5); i++)
                {
                    var score = scores[i];
                    embeds[i] = Program.DefaultEmbed()
                    .WithAuthor($"{score.map.title} [{score.map.difficultyName}]", user.info.avatarUrl)
                    .WithDescription($"[Map](https://quavergame.com/mapsets/map/{score.map.id})")
                    .WithColor(Color.Blue)
                    .WithThumbnailUrl($"https://cdn.quavergame.com/mapsets/{score.map.id}.jpg")
                    .AddField("Score", $"**Score**\t{score.totalScore.ToString("N0")}\n**Accuracy**\t{score.accuracy.ToString("N2")}%\n**Max Combo**\t{score.maxCombo.ToString("N0")}\n**P-Rating**\t{score.rating.ToString("N2")}", true)
                    .AddField("Stats", $"**Mods**\t{score.modsString}\n**Scroll Sp.**\t{(score.scrollSpeed / 10).ToString("N1")}\n**Grade**\t{score.grade}", true)
                    .AddField("Judgements", $"**Marv**\t{score.countMarvellous.ToString("N0")}\n**Perf**\t{score.countPerfect.ToString("N0")}\n**Great**\t{score.countGreat.ToString("N0")}\n**Good**\t{score.countGood.ToString("N0")}\n**Okay**\t{score.countOkay.ToString("N0")}\n**Miss**\t{score.countMiss.ToString("N0")}", true)
                    .Build();
                }
                await RespondAsync(embeds: embeds);
            }
            catch (Exception ex)
            {
                l.Critical(ex.Message, "Top", ex);
                await RespondAsync("An error occured");
            }
        }

        [SlashCommand("map", "Get a Quaver map")]
        public async Task Map([Summary("id")] int mapId, [Summary("Search")] string search, [Summary("Gamemode")] QuaverInternal.QuaverMode? mode = null)
        {
            await RespondAsync("Not implemented yet");
        }
    }
    public class QuaverInternal
    {
        public class userConfig
        {
            public string? username;
            public QuaverMode? mode;
        }
        public enum QuaverMode
        {
            Key4 = 1,
            Key7 = 2
        }
        public class score : QuaverAPI.Structures.Score
        {
            [JsonProperty("mods")]
            public new long mods;
        }
        public class qscores
        {
            [JsonProperty("status")]
            public int status;
            [JsonProperty("scores")]
            public List<score> scores;
        }
    }
}