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
        public async Task SetUser([Summary("Username")] string username, [Summary("Gamemode")] QuaverInternal.QuaverStructures.QuaverMode mode = QuaverInternal.QuaverStructures.QuaverMode.Key4)
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
                await RespondAsync(ex.ToString());
            }
        }

        [SlashCommand("profile", "Get a Quaver users profile")]
        public async Task Profile([Summary("Username")] string? username = null, [Summary("Gamemode")] QuaverInternal.QuaverStructures.QuaverMode? mode = null)
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
                            mode = userConfig.quaver.mode ?? QuaverInternal.QuaverStructures.QuaverMode.Key4;
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
                    mode = QuaverInternal.QuaverStructures.QuaverMode.Key4;
                var userMode = mode == QuaverInternal.QuaverStructures.QuaverMode.Key4 ? user.keys4 : user.keys7;
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
                await RespondAsync(ex.ToString());
            }
        }

        [SlashCommand("recent", "Get a Quaver users recent plays")]
        public async Task Recent([Summary("Username")] string? username = null, [Summary("Gamemode")] QuaverInternal.QuaverStructures.QuaverMode? mode = null)
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
                            mode = userConfig.quaver.mode ?? QuaverInternal.QuaverStructures.QuaverMode.Key4;
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
                    mode = QuaverInternal.QuaverStructures.QuaverMode.Key4;
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
                await RespondAsync(ex.ToString());
            }
        }

        [SlashCommand("top", "Get a Quaver users top plays")]
        public async Task Top([Summary("Username")] string? username = null, [Summary("Gamemode")] QuaverInternal.QuaverStructures.QuaverMode? mode = null, [Summary("page")] int page = 0)
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
                            mode = userConfig.quaver.mode ?? QuaverInternal.QuaverStructures.QuaverMode.Key4;
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
                    mode = QuaverInternal.QuaverStructures.QuaverMode.Key4;
                var response = await QuaverAPI.API.ApiCallAsync($"/users/scores/best?id={userId}&mode={(int)mode!}&limit=5&page={page}");
                var scores = JsonConvert.DeserializeObject<QuaverInternal.Scores>(response)!.scores;
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
                await RespondAsync(ex.ToString());
            }
        }

        [SlashCommand("map", "Get a Quaver map")]
        public async Task Map([Summary("id")] int? mapId = null)
        {
            try
            {
                if (mapId == null)
                {
                    // find map in chat
                }
            }
            catch (Exception ex)
            {
                l.Critical(ex.Message, "Map", ex);
                await RespondAsync(ex.ToString());
            }
        }
        [SlashCommand("mapset", "Set your default Quaver user")]
        public async Task Mapset([Summary("id")] int? mapsetId = null, [Summary("Search")] string? search = null, [Summary("Gamemode")] QuaverInternal.QuaverStructures.QuaverMode? mode = null)
        {
            try
            {
                if (mapsetId == null)
                {
                    if (search == null)
                    {
                        if (QuaverInternal.GetLastMapId(Context.Channel.Id, Context.Guild.Id) == -1)
                        {
                            await RespondAsync("No id/Search specified and no mapset found in chat");
                            return;
                        }
                        mapsetId = QuaverInternal.GetLastMapId(Context.Channel.Id, Context.Guild.Id);
                    }
                    else
                    {
                        var response = await QuaverAPI.API.ApiCallAsync($"/mapsets/maps/search?search={search}&mode={(int)mode!}&limit=1");
                        var maps = JsonConvert.DeserializeObject<QuaverInternal.Mapsets>(response)!;
                        if (maps.mapsets.Count == 0)
                        {
                            await RespondAsync("No mapsets found");
                            return;
                        }
                        mapsetId = maps.mapsets.First().id;
                    }
                }
                if (mapsetId == null)
                {
                    await RespondAsync("No mapset found");
                    return;
                }
                var mapset = await QuaverAPI.API.GetMapsetAsync(mapsetId.Value);
                if (mapset == null)
                {
                    await RespondAsync("Mapset not found");
                    return;
                }
                QuaverInternal.SetLastMapId(Context.Channel.Id, Context.Guild.Id, mapset.maps.First().id);
            }
            catch (Exception ex)
            {
                l.Critical(ex.Message, "Mapset", ex);
                await RespondAsync(ex.ToString());
            }
        }
    }
    public class QuaverInternal
    {
        public class userConfig
        {
            public string? username;
            public QuaverStructures.GameMode? mode;
        }
        public static int GetLastMapId(ulong channelId, ulong guildId)
        {
            if (Program.GetServerConfig(guildId).quaver.lastMapChannel.TryGetValue(channelId, out int mapId))
                return mapId;
            return -1;
        }
        public static void SetLastMapId(ulong channelId, ulong guildId, int mapId)
        {
            var sc = Program.GetServerConfig(guildId);
            if (sc.quaver.lastMapChannel.ContainsKey(channelId))
                sc.quaver.lastMapChannel[channelId] = mapId;
            else
                sc.quaver.lastMapChannel.Add(channelId, mapId);
            Program.SetServerConfig(guildId, sc);
        }
        public class guildConfig
        {
            public Dictionary<ulong, int> lastMapChannel = new();
        }
    }
    public class QuaverAPI
    {

    }
    public class QuaverStructures
    {
        public enum GameMode
        {
            Key4 = 1,
            Key7 = 2
        }
        public class FullUser
        {
            [JsonProperty("status")]
            public int status { get; set; }
            [JsonProperty("user")]
            public User user { get; set; }
        }
        public class User
        {
            [JsonProperty("info")]
            public UserInfo userInfo { get; set; }
            [JsonProperty("profile_badges")]
            public List<UserBadge> profileBadges { get; set; }
            [JsonProperty("activity_feed")]
            public List<UserActivity> activityFeed { get; set; }
            [JsonProperty("keys4")]
            public UserKeymode keys4 { get; set; }
            [JsonProperty("keys7")]
            public UserKeymode keys7 { get; set; }
        }
        public class UserInfo : UserInfoShort
        {
            [JsonProperty("allowed")]
            public int allowed { get; set; }
            [JsonProperty("mute_endtime")]
            public DateTime muteEndtime { get; set; }
            [JsonProperty("userpage")]
            public string userpage { get; set; }
            [JsonProperty("information")]
            public UserSocials userSocials { get; set; }
            [JsonProperty("online")]
            public bool online { get; set; }
        }
        public class UserInfoShort
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("username")]
            public string username { get; set; }
            [JsonProperty("steam_id")]
            public string steamId { get; set; }
            [JsonProperty("country")]
            public string country { get; set; }
            [JsonProperty("privileges")]
            public int privileges { get; set; }
            [JsonProperty("usergroups")]
            public int usergroups { get; set; }
            [JsonProperty("time_registered")]
            public DateTime timeRegistered { get; set; }
            [JsonProperty("latest_activity")]
            public DateTime latestActivity { get; set; }
            [JsonProperty("avatar_url")]
            public string avatarUrl { get; set; }
        }
        public class UserBadge
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("name")]
            public string name { get; set; }
            [JsonProperty("description")]
            public string description { get; set; }
        }
        public class UserActivity
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("type")]
            public int type { get; set; }
            [JsonProperty("timestamp")]
            public DateTime time { get; set; }
            [JsonProperty("map")]
            public UserActivityMap map { get; set; }
        }
        public class UserActivityMap
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("name")]
            public string name { get; set; }
        }
        public class UserKeymode
        {
            [JsonProperty("globalRank")]
            public int globalRank { get; set; }
            [JsonProperty("countryRank")]
            public int countryRank { get; set; }
            [JsonProperty("multiplayerWinRank")]
            public int multiplayerWinRank { get; set; }
            [JsonProperty("stats")]
            public UserKeymodeStats stats { get; set; }
        }
        public class UserKeymodeStats
        {
            [JsonProperty("user_id")]
            public int userId { get; set; }
            [JsonProperty("total_score")]
            public ulong totalScore { get; set; }
            [JsonProperty("ranked_score")]
            public ulong rankedScore { get; set; }
            [JsonProperty("overall_accuracy")]
            public double overallAccuracy { get; set; }
            [JsonProperty("overall_performance_rating")]
            public double overallPerformanceRating { get; set; }
            [JsonProperty("play_count")]
            public int playCount { get; set; }
            [JsonProperty("fail_count")]
            public int failCount { get; set; }
            [JsonProperty("max_combo")]
            public int maxCombo { get; set; }
            [JsonProperty("replays_watched")]
            public int replaysWatched { get; set; }
            [JsonProperty("total_marv")]
            public int totalMarvelous { get; set; }
            [JsonProperty("total_perf")]
            public int totalPerfect { get; set; }
            [JsonProperty("total_great")]
            public int totalGreat { get; set; }
            [JsonProperty("total_good")]
            public int totalGood { get; set; }
            [JsonProperty("total_okay")]
            public int totalOkay { get; set; }
            [JsonProperty("total_miss")]
            public int totalMiss { get; set; }
            [JsonProperty("total_pauses")]
            public int totalPauses { get; set; }
            [JsonProperty("multiplayer_wins")]
            public int multiplayerWins { get; set; }
            [JsonProperty("multiplayer_loses")]
            public int multiplayerLoses { get; set; }
            [JsonProperty("multiplayer_ties")]
            public int multiplayerTies { get; set; }
        }
        public class UserSocials
        {
            [JsonProperty("discord")]
            public string discord { get; set; }
            [JsonProperty("twitter")]
            public string twitter { get; set; }
            [JsonProperty("twitch")]
            public string twitch { get; set; }
            [JsonProperty("youtube")]
            public string youtube { get; set; }
        }


        public class FullMapset
        {
            [JsonProperty("status")]
            public int status { get; set; }
            [JsonProperty("mapset")]
            public Mapset mapset { get; set; }
        }
        public class Mapset
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("creator_id")]
            public int creatorId { get; set; }
            [JsonProperty("creator_username")]
            public string creatorUsername { get; set; }
            [JsonProperty("creator_avatar_url")]
            public string creatorAvatarUrl { get; set; }
            [JsonProperty("artist")]
            public string artist { get; set; }
            [JsonProperty("title")]
            public string title { get; set; }
            [JsonProperty("source")]
            public string source { get; set; }
            [JsonProperty("tags")]
            public string tags { get; set; }
            [JsonProperty("description")]
            public string description { get; set; }
            [JsonProperty("date_submitted")]
            public DateTime dateSubmitted { get; set; }
            [JsonProperty("date_last_updated")]
            public DateTime dateLastUpdated { get; set; }
            [JsonProperty("ranking_queue_status")]
            public int rankingQueueStatus { get; set; }
            [JsonProperty("ranking_queue_last_updated")]
            public DateTime rankingQueueLastUpdated { get; set; }
            [JsonProperty("ranking_queue_vote_count")]
            public int rankingQueueVoteCount { get; set; }
            [JsonProperty("mapset_ranking_queue_id")]
            public int mapsetRankingQueueId { get; set; }
            [JsonProperty("maps")]
            public Map[] maps { get; set; }
        }
        public class Map
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("mapset_id")]
            public int mapsetId { get; set; }
            [JsonProperty("md5")]
            public string md5 { get; set; }
            [JsonProperty("alternative_md5")]
            public string alternativeMd5 { get; set; }
            [JsonProperty("creator_id")]
            public int creatorId { get; set; }
            [JsonProperty("creator_username")]
            public string creatorUsername { get; set; }
            [JsonProperty("game_mode")]
            public GameMode gameMode { get; set; }
            [JsonProperty("ranked_status")]
            public int rankedStatus { get; set; }
            [JsonProperty("artist")]
            public string artist { get; set; }
            [JsonProperty("title")]
            public string title { get; set; }
            [JsonProperty("source")]
            public string source { get; set; }
            [JsonProperty("tags")]
            public string tags { get; set; }
            [JsonProperty("description")]
            public string description { get; set; }
            [JsonProperty("difficulty_name")]
            public string difficultyName { get; set; }
            [JsonProperty("length")]
            public int length { get; set; }
            [JsonProperty("bpm")]
            public double bpm { get; set; }
            [JsonProperty("difficulty_rating")]
            public double difficultyRating { get; set; }
            [JsonProperty("count_hitobject_normal")]
            public int countNotes { get; set; }
            [JsonProperty("count_hitobject_long")]
            public int countLongNotes { get; set; }
            [JsonProperty("play_count")]
            public int playCount { get; set; }
            [JsonProperty("fail_count")]
            public int failCount { get; set; }
            [JsonProperty("mods_pending")]
            public int modsPending { get; set; }
            [JsonProperty("mods_accepted")]
            public int modsAccepted { get; set; }
            [JsonProperty("mods_denied")]
            public int modsDenied { get; set; }
            [JsonProperty("mods_ignored")]
            public int modsIgnored { get; set; }
            [JsonProperty("date_submitted")]
            public DateTime? dateSubmitted { get; set; }
            [JsonProperty("date_last_updated")]
            public DateTime? dateLastUpdated { get; set; }
        }
        public class SearchMapsets
        {
            [JsonProperty("status")]
            public int status { get; set; }
            [JsonProperty("mapsets")]
            public Mapset[] mapsets { get; set; }
        }
        public class SearchMapset : Mapset
        {
            [JsonProperty("ranked_status")]
            public int rankedStatus { get; set; }
            [JsonProperty("bpms")]
            public double[] bpms { get; set; }
            [JsonProperty("game_modes")]
            public GameMode[] gameModes { get; set; }
            [JsonProperty("difficulty_names")]
            public string[] difficultyNames { get; set; }
            [JsonProperty("difficulty_range")]
            public double[] difficultyRange { get; set; }
            [JsonProperty("min_length_seconds")]
            public int minLengthSeconds { get; set; }
            [JsonProperty("max_length_seconds")]
            public int maxLengthSeconds { get; set; }
            [JsonProperty("min_ln_percent")]
            public float minLnPercent { get; set; }
            [JsonProperty("max_ln_percent")]
            public float maxLnPercent { get; set; }
            [JsonProperty("min_play_count")]
            public int minPlayCount { get; set; }
            [JsonProperty("max_play_count")]
            public int maxPlayCount { get; set; }
            [JsonProperty("min_date_submitted")]
            public DateTime minDateSubmitted { get; set; }
            [JsonProperty("max_date_submitted")]
            public DateTime maxDateSubmitted { get; set; }
            [JsonProperty("min_date_last_updated")]
            public DateTime minDateLastUpdated { get; set; }
            [JsonProperty("max_date_last_updated")]
            public DateTime maxDateLastUpdated { get; set; }
            [JsonProperty("min_combo")]
            public int minCombo { get; set; }
            [JsonProperty("max_combo")]
            public int maxCombo { get; set; }


            private new string creatorAvatarUrl { get; set; } = "Not in search";
            private new int rankingQueueStatus { get; set; } = 0;
            private new DateTime rankingQueueLastUpdated { get; set; } = DateTime.MinValue;
            private new int rankingQueueVoteCount { get; set; } = 0;
            private new int mapsetRankingQueueId { get; set; } = 0;
            private new Map[] maps { get; set; } = new Map[0];
        }


        public class Scores
        {
            [JsonProperty("status")]
            public int status { get; set; }
            [JsonProperty("scores")]
            public Score[] scores { get; set; }
        }
        public class Score
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("map_md5")]
            public string mapMd5 { get; set; }
            [JsonProperty("time")]
            public DateTime time { get; set; }
            [JsonProperty("mode")]
            public GameMode mode { get; set; }
            [JsonProperty("mods")]
            public int mods { get; set; }
            [JsonProperty("mods_string")]
            public string modsString { get; set; }
            [JsonProperty("performance_rating")]
            public double performanceRating { get; set; }
            [JsonProperty("total_score")]
            public int totalScore { get; set; }
            [JsonProperty("accuracy")]
            public double accuracy { get; set; }
            [JsonProperty("grade")]
            public string grade { get; set; }
            [JsonProperty("max_combo")]
            public int maxCombo { get; set; }
            [JsonProperty("count_marv")]
            public int countMarvelous { get; set; }
            [JsonProperty("count_perf")]
            public int countPerfect { get; set; }
            [JsonProperty("count_great")]
            public int countGreat { get; set; }
            [JsonProperty("count_good")]
            public int countGood { get; set; }
            [JsonProperty("count_okay")]
            public int countOkay { get; set; }
            [JsonProperty("count_miss")]
            public int countMiss { get; set; }
            [JsonProperty("user")]
            public UserInfoShort user { get; set; }
        }
    }
}