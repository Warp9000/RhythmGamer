using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System.Text;

namespace RhythmGamer
{
    [Group("osu")]
    [Name("osu!")]
    [Summary("osu! related commands")]
    public class osu : ModuleBase<SocketCommandContext>
    {
        public async Task profile([Name("username/id")] string? user = null, [Name("mode")][Summary("The gamemode to lookup")] string? mode = null)
        {
            var response = await osuInternal.GetUser(user ?? Program.GetUserConfig(Context.User.Id).osu.username ?? Context.User.Username, mode ?? "");
            var EmbedBuilder = Program.DefaultEmbed();
            EmbedBuilder.Author = new()
            {
                Name = response.username,
                IconUrl = $"https://osu.ppy.sh/images/flags/{response.country_code}.png"
            };
            var gc = response.statistics.grade_counts;
            EmbedBuilder.Description =
            $"**Rank:** {response.statistics.global_rank ?? 0}\n" +
            $"**Level:** {response.statistics.level.current}\n" +
            $"**PP:** {response.statistics.pp} **Acc:** {response.statistics.hit_accuracy}%\n" +
            $"**Playcount:** {response.statistics.play_count} ({response.statistics.play_time})\n" +
            $"**Ranks:** SSH´{gc.ssh}´ SS´{gc.ss}´ SH´{gc.sh}´ S´{gc.s}´ A´{gc.a}´";
            await ReplyAsync(embed: EmbedBuilder.Build());
        }
    }
    #region internal stuff
    public class osuInternal
    {
        public class config
        {
            public string? username;
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
        public static async void Authorize()
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
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cc.access_token);
                // client.DefaultRequestHeaders.Add("Authorization", "Bearer " + cc.access_token);
            }
        }
        public static async Task<osuData.osuBeatmapset> GetBeatmapset(int id)
        {
            Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/beatmapsets/{id}");
            var content = JsonConvert.DeserializeObject<osuData.osuBeatmapset>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            return content;
        }
        public static async Task<osuData.osuBeatmapsets> GetBeatmapsets(string search)
        {
            Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/beatmapsets/search?q={search}");
            var content = JsonConvert.DeserializeObject<osuData.osuBeatmapsets>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            return content;
        }
        public static async Task<osuData.osuBeatmap> GetBeatmap(int id)
        {
            Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/beatmaps/lookup?id={id}");
            var content = JsonConvert.DeserializeObject<osuData.osuBeatmap>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            return content;
        }
        public static async Task<osuData.osuScores> GetBeatmapScores(int id)
        {
            Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/beatmaps/{id}/scores");
            var content = JsonConvert.DeserializeObject<osuData.osuScores>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            return content;
        }
        public static async Task<osuData.osuScores> GetBeatmapUserScores(int mapId, int userId)
        {
            Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/beatmaps/{mapId}/scores/users/{userId}/all");
            var content = JsonConvert.DeserializeObject<osuData.osuScores>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            return content;
        }
        public static async Task<osuData.osuUser> GetUser(int id, string? mode)
        {
            Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/users/{id}/{mode ?? ""}");
            var content = JsonConvert.DeserializeObject<osuData.osuUser>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            return content;
        }
        public static async Task<osuData.osuUser> GetUser(string username, string? mode)
        {
            Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/users/{username}/{mode ?? ""}");
            var content = JsonConvert.DeserializeObject<osuData.osuUser>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            return content;
        }
        public static async Task<osuData.osuScores> GetUserTop(int id)
        {
            Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/users/{id}/scores/best");
            var content = JsonConvert.DeserializeObject<osuData.osuScores>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            return content;
        }
        public static async Task<osuData.osuScores> GetUserRecent(int id)
        {
            Authorize();
            var responseMessage = await client.GetAsync($"{baseUrl}/users/{id}/scores/recent");
            var content = JsonConvert.DeserializeObject<osuData.osuScores>(await responseMessage.Content.ReadAsStringAsync()) ?? new();
            return content;
        }
    }
    public class osuData
    {
        public class osuBeatmap
        {
            public string error = "none";
            // --------------------------------------------------
            public int beatmapset_id;
            public float difficulty_rating;
            public int id;
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
            public string artist = "";
            public string artist_unicode = "";
            public osuCovers? covers;
            public string creator = "";
            public int favourite_count;
            public int id;
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
            // --------------------------------------------------
            public int id;
            public int best_id;
            public int user_id;
            public float accuracy;
            public string[]? mods;
            public int score;
            public int max_combo;
            public bool perfect;
            public osuScoreStatistics statistics = new osuScoreStatistics();
            public bool passed;
            public float pp;
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
            public float? weight;
            public int? user;
            public string? match;
        }
        public class osuScores
        {
            public string error = "none";
            public osuScore[]? scores;
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
            public string avatar_url = "";
            public string country_code = "";
            public string default_group = "";
            public int id;
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
            public int[]? monthly_playcounts;
            public int page;
            public int pending_beatmapset_count;
            public string[]? previous_usernames;
            public int[]? rank_history;
            public int ranked_beatmapset_count;
            public int replays_watched_counts;
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
            public int play_count;
            public float play_time;
            public float pp;
            public int? global_rank;
            public int ranked_score;
            public int replays_watched_by_others;
            public int total_hits;
            public int total_score;
        }
    }
    #endregion
}