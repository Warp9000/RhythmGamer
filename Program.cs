using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace RhythmGamer
{
    public class GlobalConfig
    {
        public string token { get; set; } = "";
        public string prefix { get; set; } = "";
        public string status { get; set; } = "";
        public string[] game { get; set; } = new string[1];
        public string[] gameType { get; set; } = new string[1];
        public OsuConfig osuConfig { get; set; } = new();
    }
    public class OsuConfig
    {
        public string client_id { get; set; } = "";
        public string client_secret { get; set; } = "";
    }
    public class ServerConfig
    {
        public ulong id { get; set; } = 0;
        public string prefix { get; set; } = "No Prefix Set";
    }
    public class UserConfig
    {
        public ulong id { get; set; } = 0;
        public osuInternal.config osu { get; set; } = new();
    }
    public class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();
        public static string version = "0.0 Dev Build";
        public static Discord.WebSocket.DiscordSocketClient? _client;
        public static Discord.WebSocket.DiscordSocketConfig config = new Discord.WebSocket.DiscordSocketConfig();
        public static GlobalConfig Config = new GlobalConfig();
        public static DateTime startTime = DateTime.Now;
        public static int Commands = 0;
        public static ulong Warp = 408615875252322305;
        int CurrentActivity = 0;
        public static List<ServerConfig> ServerConfigs = new List<ServerConfig>();
        public static List<UserConfig> UserConfigs = new List<UserConfig>();
        public async Task MainAsync()
        {
            Console.Title = "RhythmGamer " + version;
            Console.BufferHeight = 1000;
            config = new Discord.WebSocket.DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 250,
                LogLevel = LogSeverity.Debug,
                GatewayIntents = GatewayIntents.All
            };
            _client = new Discord.WebSocket.DiscordSocketClient(config);


            Directory.CreateDirectory("Logs");
            if (File.Exists("Logs/latest.log"))
            {
                File.Move("Logs/latest.log", "Logs/log-" + File.GetLastWriteTime("Logs/latest.log").ToString("yyyy-MM-dd-HH-mm-ss") + ".log", true);
                File.Delete("Logs/latest.log");
            }

            _client.Log += Log;

            Directory.CreateDirectory("Data");
            if (File.Exists("Data/config.json"))
            {
                Config = JsonConvert.DeserializeObject<GlobalConfig>(File.ReadAllText("Data/config.json"))!;
            }
            else
            {
                l.Critical("config.json not found!");
                File.WriteAllText("Data/config.json", JsonConvert.SerializeObject(Config, Formatting.Indented));
                l.Critical("Please fill out the config.json file in the Data directory and restart the bot.");
                Console.ReadKey();
                return;
            }



            if (string.IsNullOrEmpty(Config.token))
            {
                l.Critical("Token is empty!", "MainAsync");
                Console.ReadKey();
                return;
            }
            if (string.IsNullOrEmpty(Config.status))
            {
                l.Info("No status set. Defaulting to online", "MainAsync");
                Config.status = "Online";
            }

            await _client.LoginAsync(TokenType.Bot, Config.token);
            await _client.StartAsync();
            await _client.SetActivityAsync(new Game(Config.game[0], (ActivityType)Enum.Parse(typeof(ActivityType), Config.gameType[0]), details: "https://warp.tf/"));
            // await _client.SetGameAsync(Config.game);
            await _client.SetStatusAsync((UserStatus)Enum.Parse(typeof(UserStatus), Config.status));

            CommandServiceConfig csc = new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                // DefaultRunMode = RunMode.Async,
                LogLevel = config.LogLevel
            };

            var ch = new RhythmGamer.CommandHandler(_client, new CommandService(csc));
            await ch.InstallCommandsAsync();

            _client.Ready += () =>
            {
                Directory.CreateDirectory("Data/Guilds");
                Directory.CreateDirectory("Data/Users");

                if (Directory.GetFiles("Data/Guilds").Length != 0)
                {
                    foreach (var config in Directory.GetFiles("Data/Guilds"))
                    {
                        var server = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText(config))!;
                        ServerConfigs.Add(server);
                    }
                }
                if (Directory.GetFiles("Data/Users").Length != 0)
                {
                    foreach (var config in Directory.GetFiles("Data/Users"))
                    {
                        var user = JsonConvert.DeserializeObject<UserConfig>(File.ReadAllText(config))!;
                        UserConfigs.Add(user);
                    }
                }
                List<ulong> servers = new List<ulong>();
                foreach (var server in _client.Guilds)
                {
                    servers.Clear();
                    for (var i = 0; i < ServerConfigs.Count; i++)
                    {
                        servers.Add(ServerConfigs[i].id);
                    }

                    if (!servers.Contains(server.Id))
                    {
                        l.Verbose($"Adding server {server.Name} to config", "MainAsync");
                        ServerConfigs.Add(new ServerConfig
                        {
                            id = server.Id,
                            prefix = Config.prefix
                        });
                    }
                }
                foreach (var item in ServerConfigs)
                {
                    File.WriteAllText($"Data/Guilds/{item.id}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
                }

                _client.Ready -= () =>
                {
                    return Task.CompletedTask;
                };
                return Task.CompletedTask;
            };

            var timer = new System.Timers.Timer(15000);
            timer.Elapsed += async (sender, e) =>
            {
                if (_client.LoginState != LoginState.LoggedIn)
                    return;
                if (CurrentActivity >= Config.game.Length)
                    CurrentActivity = 0;
                await _client.SetActivityAsync(new Game(Config.game[CurrentActivity], (ActivityType)Enum.Parse(typeof(ActivityType), Config.gameType[CurrentActivity]), details: "https://warp.tf/"));
                CurrentActivity++;
            };
            timer.Start();

            _client.JoinedGuild += (g) =>
            {
                l.Info($"Adding server {g.Name} to config", "MainAsync");
                ServerConfigs.Add(new ServerConfig
                {
                    id = g.Id,
                    prefix = Config.prefix
                });

                // foreach (var item in ServerConfigs)
                // {
                //     File.WriteAllText($"Data/Guilds/{item.id}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
                // }
                return Task.CompletedTask;
            };
            _client.LeftGuild += (g) =>
            {
                l.Info($"Removing server {g.Name} from config", "MainAsync");
                if (ServerConfigs.Exists(x => x.id == g.Id))
                    ServerConfigs.Remove(ServerConfigs.Find(x => x.id == g.Id)!);
                // foreach (var item in ServerConfigs)
                // {
                //     File.WriteAllText($"Data/Guilds/{item.id}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
                // }
                return Task.CompletedTask;
            };


            // console commands
            while (true)
            {
                string? input = Console.ReadLine();
                switch (input)
                {
                    case "exit":
                        foreach (var item in ServerConfigs)
                        {
                            File.WriteAllText($"Data/Guilds/{item.id}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
                        }
                        foreach (var item in UserConfigs)
                        {
                            File.WriteAllText($"Data/Users/{item.id}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
                        }
                        await _client.SetStatusAsync(UserStatus.Invisible);
                        await _client.LogoutAsync();
                        Environment.Exit(0);
                        break;
                    case "reload":
                        Config = JsonConvert.DeserializeObject<GlobalConfig>(File.ReadAllText("Data/config.json"))!;
                        break;
                    case "save":
                        var servers = new List<ulong>();
                        foreach (var server in _client.Guilds)
                        {
                            servers.Clear();
                            for (var i = 0; i < ServerConfigs.Count; i++)
                            {
                                servers.Add(ServerConfigs[i].id);
                            }
                            l.Info($"Adding server {server.Name} to config", "MainAsync");
                            if (!servers.Contains(server.Id))
                                ServerConfigs.Add(new ServerConfig
                                {
                                    id = server.Id,
                                    prefix = Config.prefix
                                });
                        }
                        foreach (var item in ServerConfigs)
                        {
                            File.WriteAllText($"Data/Guilds/{item.id}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
                        }
                        break;
                    case "help":
                        Console.WriteLine("exit - exits the program");
                        Console.WriteLine("reload - reloads the config");
                        Console.WriteLine("save - saves the server configs");
                        // Console.WriteLine("minimize - minimizes the console");
                        Console.WriteLine("help - shows this message");
                        break;
                    default:
                        Console.WriteLine("Unknown command. Use 'help' to see a list of commands.");
                        break;
                }
            }

            // Block this task until the program is closed.
            // await Task.Delay(-1);
        }
        public Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    l.Critical(msg.Message, msg.Source, msg.Exception);
                    break;
                case LogSeverity.Error:
                    l.Error(msg.Message, msg.Source, msg.Exception);
                    break;
                case LogSeverity.Warning:
                    l.Warning(msg.Message, msg.Source, msg.Exception);
                    break;
                case LogSeverity.Info:
                    l.Info(msg.Message, msg.Source);
                    break;
                case LogSeverity.Verbose:
                    l.Verbose(msg.Message, msg.Source);
                    break;
                case LogSeverity.Debug:
                    l.Debug(msg.Message, msg.Source);
                    break;
            }
            return Task.CompletedTask;
        }
        public static EmbedBuilder DefaultEmbed()
        {
            return new EmbedBuilder().WithColor(new Discord.Color(0xff6000)).WithCurrentTimestamp().WithFooter(new EmbedFooterBuilder()
            {
                Text = $"RhythmGamer {version}",
                IconUrl = "https://a.ppy.sh/24087648?1642782431.jpeg"
            });
        }
        public static ServerConfig GetServerConfig(ulong id)
        {
            foreach (var server in ServerConfigs)
            {
                if (server.id == id)
                    return server;
            }
            return new ServerConfig();
        }
        public static UserConfig GetUserConfig(ulong id)
        {
            foreach (var user in UserConfigs)
            {
                if (user.id == id)
                    return user;
            }
            return new UserConfig();
        }
    }

    public class l
    {
        public static void Log(LogMessage msg)
        {
            if (msg.Severity <= Program.config.LogLevel)
                Console.WriteLine($"[{System.DateTime.Now.ToString()}] [{msg.Severity}] [{msg.Source}] {msg.Message} {msg.Exception}");
            try
            {
                File.AppendAllText("Logs/latest.log", $"[{System.DateTime.Now.ToString()}] [{msg.Severity}] [{msg.Source}] {msg.Message} {msg.Exception}\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to write to log file: {e.Message}");
            }
        }
        public static void Debug(string msg, string source = "?")
        {
            Log(new LogMessage(LogSeverity.Debug, source, msg));
        }
        public static void Verbose(string msg, string source = "?")
        {
            Log(new LogMessage(LogSeverity.Verbose, source, msg));
        }
        public static void Info(string msg, string source = "?")
        {
            Log(new LogMessage(LogSeverity.Info, source, msg));
        }
        public static void Warning(string msg, string source = "?", Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Log(new LogMessage(LogSeverity.Warning, source, msg, ex));
            Console.ResetColor();
        }
        public static void Error(string msg, string source = "?", Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log(new LogMessage(LogSeverity.Error, source, msg, ex));
            Console.ResetColor();
        }
        public static void Critical(string msg, string source = "?", Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Log(new LogMessage(LogSeverity.Critical, source, msg, ex));
            Console.ResetColor();
        }
    }


}