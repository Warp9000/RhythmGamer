using Discord;
using Discord.Interactions;
using Newtonsoft.Json;
using System.Text;

namespace RhythmGamer
{
    // [Group("osu", "osu! related commands")]
    public class OsuModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("setuser", "Set your default osu! user")]
        public async Task SetUser([Summary("Username", "Your osu! username which the bot should default to")] string username)
        {
            try
            {
                var response = await osuInternal.GetUser(username, null);
                var EmbedBuilder = Program.DefaultEmbed();
                if (response.http_code == 404)
                {
                    EmbedBuilder.Title = "404";
                    EmbedBuilder.Description = "User not found";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (response.error != "none")
                {
                    EmbedBuilder.Title = response.http_code.ToString();
                    EmbedBuilder.Description = "Please report this to Warp#8703";
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (Program.UserConfigs.Exists(x => x.id == Context.User.Id))
                {
                    Program.UserConfigs.Find(x => x.id == Context.User.Id)!.osu.username = response.username;
                }
                else
                {
                    UserConfig user = new()
                    {
                        id = Context.User.Id,
                        osu =
                    {
                        username = response.username
                    }
                    };
                    Program.UserConfigs.Add(user);
                }
                EmbedBuilder.Description = $"Set default user to `{response.username}`";
                await RespondAsync(embed: EmbedBuilder.Build());
            }
            catch (Exception ex)
            {
                l.Error(ex.ToString());
            }
        }

        [SlashCommand("profile", "Get an osu! profile")]
        public async Task Profile([Summary("Username", "The user to lookup")] string? user = null, [Summary("GameMode", "The gamemode to lookup")] osuData.GameMode? mode = null)
        {
            try
            {
                var response = await osuInternal.GetUser(user ?? Program.GetUserConfig(Context.User.Id).osu.username ?? Context.User.Username, mode.ToString());
                var EmbedBuilder = Program.DefaultEmbed();
                if (response.http_code == 404)
                {
                    EmbedBuilder.Title = "404";
                    EmbedBuilder.Description = "User not found";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (response.error != "none")
                {
                    EmbedBuilder.Title = response.http_code.ToString();
                    EmbedBuilder.Description = "Please report this to Warp#8703";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (mode == null)
                    mode = (osuData.GameMode)Enum.Parse(typeof(osuData.GameMode), response.playmode);
                EmbedBuilder.Author = new()
                {
                    Name = response.username + " - " + mode.ToString(),
                    IconUrl = $"https://osu.ppy.sh/images/flags/{response.country_code}.png"
                };
                var gc = response.statistics.grade_counts;
                EmbedBuilder.Description =
                $"**Rank:** {response.statistics.global_rank ?? 0}\n" +
                $"**Level:** {response.statistics.level.current}\n" +
                $"**PP:** {response.statistics.pp} **Acc:** {Math.Round(response.statistics.hit_accuracy, 2)}%\n" +
                $"**Playcount:** {response.statistics.play_count} ({(ulong)response.statistics.play_time / 3600} hrs)\n" +
                $"**Ranks:** SSH`{gc.ssh}` SS`{gc.ss}` SH`{gc.sh}` S`{gc.s}` A`{gc.a}`";
                await RespondAsync(embed: EmbedBuilder.Build());
            }
            catch (Exception ex)
            {
                l.Error(ex.ToString());
            }
        }
        [SlashCommand("top", "Get the top plays from a user")]
        public async Task Top([Summary("Username", "The user to lookup")] string? user = null, [Summary("GameMode", "The gamemode to lookup")] osuData.GameMode? mode = null)
        {
            try
            {
                var response = await osuInternal.GetUser(user ?? Program.GetUserConfig(Context.User.Id).osu.username ?? Context.User.Username, mode.ToString());
                var EmbedBuilder = Program.DefaultEmbed();
                if (response.http_code == 404)
                {
                    EmbedBuilder.Title = "404";
                    EmbedBuilder.Description = "User not found";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (response.error != "none")
                {
                    EmbedBuilder.Title = response.http_code.ToString();
                    EmbedBuilder.Description = "Please report this to Warp#8703";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                var response2 = await osuInternal.GetUserTop(response.id, mode.ToString());
                // if (response2.http_code == 404)
                // {
                //     EmbedBuilder.Title = "404";
                //     EmbedBuilder.Description = "User not found";
                //     EmbedBuilder.Color = 0xff0000;
                //     await RespondAsync(embed: EmbedBuilder.Build());
                //     return;
                // }
                // if (response2.error != "none")
                // {
                //     EmbedBuilder.Title = response2.http_code.ToString();
                //     EmbedBuilder.Description = "Please report this to Warp#8703";
                //     EmbedBuilder.Color = 0xff0000;
                //     await RespondAsync(embed: EmbedBuilder.Build());
                //     return;
                // }
                if (mode == null)
                    mode = (osuData.GameMode)Enum.Parse(typeof(osuData.GameMode), response.playmode);
                EmbedBuilder.Author = new()
                {
                    Name = response.username + " - " + mode.ToString() + " top plays",
                    IconUrl = $"https://osu.ppy.sh/images/flags/{response.country_code}.png"
                };
                osuData.osuScore currScore = response2[0];
                osuData.osuBeatmap currMap = new();
                string mods = "";
                for (int i = 0; i < 5; i++)
                {
                    currScore = response2[i];
                    currMap = await osuInternal.GetBeatmap(currScore.beatmap.id);
                    mods = "";
                    foreach (var mod in currScore.mods)
                    {
                        mods += mod;
                    }
                    switch (currMap.mode)
                    {
                        case "mania":
                            EmbedBuilder.Description +=
                                $"**{i + 1}. [{currScore.beatmapset.title}]({currScore.beatmap.url}) +{(string.IsNullOrEmpty(mods) ? "No Mod" : mods.ToUpper())}** [{currScore.beatmap.difficulty_rating}★]\n" +
                                $"`{currScore.rank.ToUpper()}` | **{Math.Round(currScore.pp ?? -1, 2)}pp** | {Math.Round(currScore.accuracy * 100, 2)}%\n" +
                                $"`{currScore.score}` | x{currScore.max_combo}/{currMap.max_combo} | [{currScore.statistics.count_geki}/{currScore.statistics.count_300}/{currScore.statistics.count_katu}/{currScore.statistics.count_100}/{currScore.statistics.count_50}/{currScore.statistics.count_miss}]\n" +
                                $"Score set <t:{((DateTimeOffset)currScore.created_at).ToUnixTimeSeconds()}:R> (<t:{((DateTimeOffset)currScore.created_at).ToUnixTimeSeconds()}:f>)\n";
                            break;
                        default:
                            EmbedBuilder.Description +=
                                $"**{i + 1}. [{currScore.beatmapset.title}]({currScore.beatmap.url}) +{(string.IsNullOrEmpty(mods) ? "No Mod" : mods.ToUpper())}** [{currScore.beatmap.difficulty_rating}★]\n" +
                                $"`{currScore.rank.ToUpper()}` | **{Math.Round(currScore.pp ?? -1, 2)}pp** | {Math.Round(currScore.accuracy * 100, 2)}%\n" +
                                $"`{currScore.score}` | x{currScore.max_combo}/{currMap.max_combo} | [{currScore.statistics.count_300}/{currScore.statistics.count_100}/{currScore.statistics.count_50}/{currScore.statistics.count_miss}]\n" +
                                $"Score set <t:{((DateTimeOffset)currScore.created_at).ToUnixTimeSeconds()}:R> (<t:{((DateTimeOffset)currScore.created_at).ToUnixTimeSeconds()}:f>)\n";
                            break;
                    }

                }

                await RespondAsync(embed: EmbedBuilder.Build());
            }
            catch (Exception ex)
            {
                l.Error(ex.ToString());
            }
        }
        [SlashCommand("map", "Get a beatmap")]
        public async Task Map([Summary("Name", "Name of the map to lookup")] string? name = null, [Summary("Url", "The url of the map")] string? url = null, [Summary("GameMode", "The gamemode to lookup")] osuData.GameMode? mode = null)
        {
            try
            {
                var EmbedBuilder = Program.DefaultEmbed();
                if (name == null || url != null)
                {
                    ulong mapId = 0;
                    if (url == null)
                    {
                        var a = Program.ServerConfigs.Find(x => x.id == Context.Guild.Id).osu.lastMapChannel;
                        foreach (var item in a)
                        {
                            if (item.Key == Context.Channel.Id)
                            {
                                mapId = item.Value;
                                break;
                            }
                        }
                        if (mapId == 0)
                        {
                            EmbedBuilder.Title = "No map found in chat";
                            EmbedBuilder.Description = @"¯\_(ツ)_/¯";
                            EmbedBuilder.Color = 0xff0000;
                            await RespondAsync(embed: EmbedBuilder.Build());
                            return;
                        }
                    }
                    else
                    {
                        // https://osu.ppy.sh/beatmapsets/919633#mania/1920615
                        if (url.StartsWith("https://osu.ppy.sh/beatmapsets/"))
                        {
                            var s = url.TrimEnd('/').Split('/');
                            ulong id = 0;
                            if (ulong.TryParse(s[s.Count() - 1].ToString().Trim('/'), out id))
                            {
                                mapId = id;
                            }
                        }
                        if (mapId == 0)
                        {
                            EmbedBuilder.Title = "No map found with provided url";
                            EmbedBuilder.Description = @"¯\_(ツ)_/¯";
                            EmbedBuilder.Color = 0xff0000;
                            await RespondAsync(embed: EmbedBuilder.Build());
                            return;
                        }
                    }
                    if (mapId != 0)
                    {
                        var sc = Program.GetServerConfig(Context.Guild.Id);
                        if (sc.osu.lastMapChannel.ContainsKey(Context.Channel.Id))
                            sc.osu.lastMapChannel[Context.Channel.Id] = mapId;
                        else
                            sc.osu.lastMapChannel.Add(Context.Channel.Id, mapId);
                        Program.SetServerConfig(Context.Guild.Id, sc);
                        var response = await osuInternal.GetBeatmap(mapId);
                        EmbedBuilder.WithTitle((response.beatmapset ?? new()).title).WithUrl(response.url).WithThumbnailUrl($"https://b.ppy.sh/thumb/{response.beatmapset_id}l.jpg")
                        .WithDescription(
                            $"**{response.version}**\n" +
                            $"{response.difficulty_rating}★ | x{response.max_combo} | {(int)TimeSpan.FromSeconds(response.total_length).TotalMinutes}:{TimeSpan.FromSeconds(response.total_length).Seconds.ToString("D2")}\n" +
                            $"**OD:** {response.ar} | **HP:** {response.drain} | **CS:** {response.cs}"
                            );
                        await RespondAsync(embed: EmbedBuilder.Build());
                        return;
                    }
                    else
                    {
                        EmbedBuilder.Title = "idfk";
                        EmbedBuilder.Description = @"¯\_(ツ)_/¯";
                        EmbedBuilder.Color = 0xff0000;
                        await RespondAsync(embed: EmbedBuilder.Build());
                        return;
                    }
                }
                var response2 = await osuInternal.GetBeatmapsets(name);
                if (response2.http_code == 404)
                {
                    EmbedBuilder.Title = "404";
                    EmbedBuilder.Description = "Map not found";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (response2.error != "none" && response2.http_code != 200)
                {
                    EmbedBuilder.Title = response2.http_code.ToString();
                    EmbedBuilder.Description = "Please report this to Warp#8703";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                var mapset = response2.beatmapsets![0];
                EmbedBuilder.WithTitle(mapset.title).WithUrl(mapset.beatmaps[0].url).WithThumbnailUrl($"https://b.ppy.sh/thumb/{mapset.id}l.jpg");
                foreach (var item in mapset.beatmaps)
                {
                    EmbedBuilder.AddField($"**{item.version}**",
                        $"{item.difficulty_rating}★ | x{item.max_combo} | {(int)TimeSpan.FromSeconds(item.total_length).TotalMinutes}:{TimeSpan.FromSeconds(item.total_length).Seconds.ToString("D2")}\n" +
                        $"**OD:** {item.ar} | **HP:** {item.drain} | **CS:** {item.cs}"
                        );
                }
                await RespondAsync(embed: EmbedBuilder.Build());
            }
            catch (Exception ex)
            {
                l.Error(ex.ToString());
            }
        }
        [SlashCommand("recent", "Get a users recent plays within 24 hours")]
        public async Task Recent([Summary("Username", "The user to lookup")] string? user = null, [Summary("GameMode", "The gamemode to lookup")] osuData.GameMode? mode = null)
        {
            try
            {
                var EmbedBuilder = Program.DefaultEmbed();
                var userR = await osuInternal.GetUser(user ?? Program.GetUserConfig(Context.User.Id).osu.username ?? Context.User.Username, mode.ToString());
                if (userR.http_code == 404)
                {
                    EmbedBuilder.Title = "404";
                    EmbedBuilder.Description = "User not found";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (userR.error != "none")
                {
                    EmbedBuilder.Title = userR.http_code.ToString();
                    EmbedBuilder.Description = "Please report this to Warp#8703";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                var response = await osuInternal.GetUserRecent(userR.id, mode.ToString());
                if (response.Length == 0)
                {
                    EmbedBuilder.Title = "No recent plays found for user";
                    EmbedBuilder.Description = @"¯\_(ツ)_/¯";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (response[0].error != "none")
                {
                    EmbedBuilder.Title = response[0].http_code.ToString();
                    EmbedBuilder.Description = "Please report this to Warp#8703";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                var map = await osuInternal.GetBeatmap(response[0].beatmap.id);
                EmbedBuilder.WithTitle(response[0].beatmapset.title).WithUrl(response[0].beatmap.url).WithThumbnailUrl($"https://b.ppy.sh/thumb/{response[0].beatmapset.id}l.jpg")
                .WithDescription(
                    $"`{response[0].rank.ToUpper()}` | **{response[0].pp ?? -1} PP** | {Math.Round(response[0].accuracy * 100, 2)}%\n" +
                    $"{response[0].score} | x{response[0].max_combo}/{map.max_combo}"
                ).WithAuthor(new EmbedAuthorBuilder()
                    .WithName(userR.username).WithIconUrl(userR.avatar_url).WithUrl("https://osu.ppy.sh/users/" + userR.id));
                await RespondAsync(embed: EmbedBuilder.Build());
            }
            catch (Exception ex)
            {
                l.Error(ex.ToString());
            }
        }
        [SlashCommand("compare", "Get a users scores on the last map in chat")]
        public async Task Compare([Summary("Username", "The user to lookup")] string? user = null)
        {
            try
            {
                var EmbedBuilder = Program.DefaultEmbed();
                var userR = await osuInternal.GetUser(user ?? Program.GetUserConfig(Context.User.Id).osu.username ?? Context.User.Username, null);
                if (userR.http_code == 404)
                {
                    EmbedBuilder.Title = "404";
                    EmbedBuilder.Description = "User not found";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (userR.error != "none")
                {
                    EmbedBuilder.Title = userR.http_code.ToString();
                    EmbedBuilder.Description = "Please report this to Warp#8703";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                var mapR = await osuInternal.GetBeatmap(Program.GetServerConfig(Context.Guild.Id).osu.lastMapChannel[Context.Channel.Id]);
                if (mapR.http_code == 404)
                {
                    EmbedBuilder.Title = "404";
                    EmbedBuilder.Description = "Map not found";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (mapR.error != "none")
                {
                    EmbedBuilder.Title = mapR.http_code.ToString();
                    EmbedBuilder.Description = "Please report this to Warp#8703";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                osuData.osuScores response = await osuInternal.GetBeatmapUserScores(mapR.id, userR.id);
                if (response.scores.Count() == 0)
                {
                    EmbedBuilder.Title = "No scores found for user";
                    EmbedBuilder.Description = @"¯\_(ツ)_/¯";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                if (response.error != "none")
                {
                    EmbedBuilder.Title = response.http_code.ToString();
                    EmbedBuilder.Description = "Please report this to Warp#8703";
                    EmbedBuilder.Color = 0xff0000;
                    await RespondAsync(embed: EmbedBuilder.Build());
                    return;
                }
                response.scores.Sort((x, y) => (y.pp ?? -1).CompareTo((x.pp ?? -1)));
                var score = response.scores[0];
                EmbedBuilder.WithTitle(mapR.beatmapset.title).WithUrl(mapR.url).WithThumbnailUrl($"https://b.ppy.sh/thumb/{mapR.beatmapset.id}l.jpg")
                .WithDescription(
                    $"`{score.rank.ToUpper()}` | **{score.pp ?? -1} PP** | {Math.Round(score.accuracy * 100, 2)}%\n" +
                    $"{score.score} | x{score.max_combo}/{mapR.max_combo}"
                ).WithAuthor(new EmbedAuthorBuilder()
                    .WithName(userR.username).WithIconUrl(userR.avatar_url).WithUrl("https://osu.ppy.sh/users/" + userR.id));
                await RespondAsync(embed: EmbedBuilder.Build());
            }
            catch (Exception ex)
            {
                l.Error(ex.ToString());
            }
        }
    }
    #region internal stuff
    public class osuInternal
    {
        public class userConfig
        {
            public string? username;
        }
        public class guildConfig
        {
            public Dictionary<ulong, ulong> lastMapChannel = new();
        }
        static public HttpClient client = new();
        public static Dictionary<string, string> Headers = new();
        public class ClientCredentials
        {
            public string token_type = "";
            public int expires_in = 0;
            public string access_token = "";
            public DateTime created = DateTime.Now;
        }
        static public ClientCredentials cc = new();
        const string baseUrl = "https://osu.ppy.sh/api/v2";
        public static async Task Authorize()
        {
            if (cc.created.AddSeconds(cc.expires_in) < DateTime.Now)
            {
                Headers = new Dictionary<string, string>
                {
                    { "client_id", Program.Config.osuConfig.client_id},
                    { "client_secret", Program.Config.osuConfig.client_secret},
                    { "grant_type", "client_credentials"},
                    { "scope", "public "}
                };
                var json = JsonConvert.SerializeObject(Headers);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var responseMessage = await client.PostAsync("https://osu.ppy.sh/oauth/token", content);
                cc = JsonConvert.DeserializeObject<ClientCredentials>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
                cc.created = DateTime.Now;
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + cc.access_token);
            }
        }
        public static async Task<osuData.osuBeatmapset> GetBeatmapset(int id)
        {
            await Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/beatmapsets/{id}");
            File.WriteAllText("GetBeatmapset", $"// {baseUrl}/beatmapsets/{id}" + "\n\n" + await responseMessage.Content.ReadAsStringAsync());
            var content = JsonConvert.DeserializeObject<osuData.osuBeatmapset>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            content.http_code = (int)responseMessage.StatusCode;
            return content;
        }
        public static async Task<osuData.osuBeatmapsets> GetBeatmapsets(string search)
        {
            await Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/beatmapsets/search?q={search}");
            File.WriteAllText("GetBeatmapsets", $"// {baseUrl}/beatmapsets/search?q={search}" + "\n\n" + await responseMessage.Content.ReadAsStringAsync());
            var content = JsonConvert.DeserializeObject<osuData.osuBeatmapsets>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            content.http_code = (int)responseMessage.StatusCode;
            return content;
        }
        public static async Task<osuData.osuBeatmap> GetBeatmap(ulong id)
        {
            await Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/beatmaps/lookup?id={id}");
            File.WriteAllText("GetBeatmap", $"// {baseUrl}/beatmaps/lookup?id={id}" + "\n\n" + await responseMessage.Content.ReadAsStringAsync());
            var content = JsonConvert.DeserializeObject<osuData.osuBeatmap>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            content.http_code = (int)responseMessage.StatusCode;
            return content;
        }
        public static async Task<osuData.osuScores> GetBeatmapScores(int id)
        {
            await Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/beatmaps/{id}/scores");
            File.WriteAllText("GetBeatmapScores", $"// {baseUrl}/beatmaps/{id}/scores" + "\n\n" + await responseMessage.Content.ReadAsStringAsync());
            var content = JsonConvert.DeserializeObject<osuData.osuScores>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            content.http_code = (int)responseMessage.StatusCode;
            return content;
        }
        public static async Task<osuData.osuScores> GetBeatmapUserScores(ulong mapId, ulong userId)
        {
            await Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/beatmaps/{mapId}/scores/users/{userId}/all");
            File.WriteAllText("GetBeatmapUserScores", $"// {baseUrl}/beatmaps/{mapId}/scores/users/{userId}/all" + "\n\n" + await responseMessage.Content.ReadAsStringAsync());
            var content = JsonConvert.DeserializeObject<osuData.osuScores>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            content.http_code = (int)responseMessage.StatusCode;
            return content;
        }
        public static async Task<osuData.osuUser> GetUser(int id, string? mode)
        {
            await Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/users/{id}/{mode ?? ""}");
            File.WriteAllText("GetUserId", $"// {baseUrl}/users/{id}/{mode ?? ""}" + "\n\n" + await responseMessage.Content.ReadAsStringAsync());
            var content = JsonConvert.DeserializeObject<osuData.osuUser>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            content.http_code = (int)responseMessage.StatusCode;
            return content;
        }
        public static async Task<osuData.osuUser> GetUser(string username, string? mode)
        {
            await Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/users/{username}/{mode ?? ""}");
            File.WriteAllText("GetUserString", $"// {baseUrl}/users/{username}/{mode ?? ""}" + "\n\n" + await responseMessage.Content.ReadAsStringAsync());
            var content = JsonConvert.DeserializeObject<osuData.osuUser>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            content.http_code = (int)responseMessage.StatusCode;
            return content;
        }
        public static async Task<osuData.osuScore[]> GetUserTop(ulong id, string? mode)
        {
            await Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/users/{id}/scores/best" + (string.IsNullOrEmpty(mode) ? "" : $"?q={mode}"));
            File.WriteAllText("GetUserTop", $"// {baseUrl}/users/{id}/scores/best" + (string.IsNullOrEmpty(mode) ? "" : $"?q={mode}") + "\n\n" + await responseMessage.Content.ReadAsStringAsync());
            var content = JsonConvert.DeserializeObject<osuData.osuScore[]>(await responseMessage.Content.ReadAsStringAsync()) ?? new osuData.osuScore[1];
            content[0].http_code = (int)responseMessage.StatusCode;
            return content;
        }
        public static async Task<osuData.osuScore[]> GetUserRecent(ulong id, string? mode)
        {
            await Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/users/{id}/scores/recent?include_fails=1" + (string.IsNullOrEmpty(mode) ? "" : $"&q={mode}"));
            File.WriteAllText("GetUserRecent", $"// {baseUrl}/users/{id}/scores/recent?include_fails=1" + (string.IsNullOrEmpty(mode) ? "" : $"$q={mode}") + "\n\n" + await responseMessage.Content.ReadAsStringAsync());
            var content = JsonConvert.DeserializeObject<osuData.osuScore[]>(await responseMessage.Content.ReadAsStringAsync()) ?? new osuData.osuScore[1];
            // if (content.Length == 0)
            //     content = new osuData.osuScore[1];
            // content[0].http_code = (int)responseMessage.StatusCode;
            return content;
        }
    }
    public class osuData
    {
        public class osuBeatmap
        {
            public string error = "none";
            public int http_code;
            // --------------------------------------------------
            public int beatmapset_id;
            public float difficulty_rating;
            public ulong id;
            public string mode = "";
            public string status = "";
            public int total_length;
            public int user_id;
            public string version = "";
            // --------------------------------------------------
            public float accuracy; // OD
            public float ar;
            // public int beatmapset_id;
            public float? bpm;
            public bool convert;
            public int count_circles;
            public int count_sliders;
            public int count_spinners;
            public float cs;
            public DateTime? deleted_at;
            public float drain;
            public int hit_length;
            public bool is_scoreable;
            public DateTime last_updated;
            public int mode_int;
            public int passcount;
            public int playcount;
            public int ranked;
            public string url = "";
            // --------------------------------------------------
            public osuBeatmapset? beatmapset;
            public string? checksum;
            public osuFailTimes? failtimes;
            public int? max_combo;

        }
        public class osuBeatmapset
        {
            public string error = "none";
            public int http_code;
            public string artist = "";
            public string artist_unicode = "";
            public osuCovers? covers;
            public string creator = "";
            public int favourite_count;
            public ulong id;
            public bool nsfw;
            public int play_count;
            public string preview_url = "";
            public string source = "";
            public string status = "";
            public string title = "";
            public string title_unicode = "";
            public int user_id;
            public bool video;
            // --------------------------------------------------
            // availability.download_disabled
            // availability.more_information
            public float bpm;
            public bool can_be_hyped;
            public bool discussion_locked;
            // hype.required
            // hype.current
            public bool is_scoreable;
            public DateTime last_updated;
            public string? legacy_thread_url;
            // nominations.current
            // nominations.required
            public int ranked;
            public DateTime? ranked_date;
            public bool storyboard;
            public DateTime? submitted_date;
            public string tags = "";
            // --------------------------------------------------
            public osuBeatmap[]? beatmaps;
        }
        public class osuBeatmapsets
        {
            public string error = "none";
            public int http_code;
            public osuBeatmapset[]? beatmapsets;
        }
        public class osuFailTimes
        {
            public int[]? exit;
            public int[]? fail;
        }
        public class osuCovers
        {
            public string cover = "";
            public string card = "";
            public string list = "";
            public string slimcover = "";
        }


        public class osuScore
        {
            public string error = "none";
            public int http_code;
            // --------------------------------------------------
            public ulong id;
            public ulong? best_id;
            public ulong user_id;
            public float accuracy;
            public string[]? mods;
            public int score;
            public int max_combo;
            public bool perfect;
            public osuScoreStatistics statistics = new osuScoreStatistics();
            public bool passed;
            public float? pp;
            public string rank = "";
            public DateTime created_at;
            public string mode = "";
            public int mode_int;
            public bool replay;
            // --------------------------------------------------
            public osuBeatmap? beatmap;
            public osuBeatmapset? beatmapset;
            public int? rank_country;
            public int? rank_global;
            public _weight? weight;
            public class _weight
            {
                public float percentage;
                public float pp;
            }
            public osuUser? user;
            public string? match;
        }
        public class osuScores
        {
            public string error = "none";
            public int http_code;
            public List<osuScore>? scores;
        }
        public class osuScoreStatistics
        {
            [JsonProperty("count_50")]
            public int count_50;
            [JsonProperty("count_100")]
            public int count_100;
            [JsonProperty("count_300")]
            public int count_300;
            [JsonProperty("count_geki")]
            public int count_geki;
            [JsonProperty("count_katu")]
            public int count_katu;
            [JsonProperty("count_miss")]
            public int count_miss;
        }


        public class osuUser
        {
            public string error = "none";
            public int http_code;
            public string avatar_url = "";
            public string country_code = "";
            public string default_group = "";
            public ulong id;
            public bool is_active;
            public bool is_bot;
            public bool is_deleted;
            public bool is_online;
            public bool is_supporter;
            public DateTime? last_visit;
            public bool pm_friends_only;
            public string? profile_colour;
            public string username = "";
            // --------------------------------------------------
            // TODO: account_history
            // TODO: active_tournament_banner
            // TODO: badges
            public int beatmap_playcounts_count;
            public int favourite_beatmapset_count;
            public int follower_count;
            public int graveyard_beatmapset_count;
            // groups
            public bool? is_restricted;
            public int loved_beatmapset_count;
            public _monthly_playcounts[]? monthly_playcounts;
            public class _monthly_playcounts
            {
                public string start_date = "";
                public int count;
            }
            public _page page = new();
            public class _page
            {
                public string html = "";
                public string raw = "";
            }
            public int pending_beatmapset_count;
            public string[]? previous_usernames;
            public _rank_istory rank_history = new();
            public class _rank_istory
            {
                public string mode = "";
                public int[]? data;
            }
            public int ranked_beatmapset_count;
            public _replays_watched_counts[]? replays_watched_counts;
            public class _replays_watched_counts
            {
                public string start_date = "";
                public int count;
            }
            public int scores_best_count;
            public int scores_first_count;
            public int scores_recent_count;
            public osuUserStatistics statistics = new();
            // statistics_rulesets
            public int support_level;
            // unread_pm_count
            // user_achievements
            // user_preferences
            // --------------------------------------------------            
            public string cover_url = "";
            public string? discord;
            public bool has_supported;
            public string? interests;
            public DateTime join_date;
            // kudosu.available
            // kudosu.total
            public string? location;
            public int max_blocks;
            public int max_friends;
            public string? occupation;
            public string playmode = "";
            public string[]? playstyle;
            public int post_count;
            public string[]? profile_order;
            public string? title;
            public string? title_url;
            public string? twitter;
            public string? website;
        }
        public class osuUserStatistics
        {
            public _grade_counts grade_counts = new();
            public class _grade_counts
            {
                public int a;
                public int s;
                public int sh;
                public int ss;
                public int ssh;
            }
            public float hit_accuracy;
            public bool is_ranked;
            public _level level = new();
            public class _level
            {
                public int current;
                public float progress;
            }
            public int maximum_combo;
            public ulong play_count;
            public float play_time;
            public float pp;
            public ulong? global_rank;
            public ulong ranked_score;
            public int replays_watched_by_others;
            public ulong total_hits;
            public ulong total_score;
        }
        public enum GameMode
        {
            osu = 1,
            taiko = 2,
            fruits = 3,
            mania = 4
        }
    }
    #endregion
}