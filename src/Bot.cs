using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Discord.WebSocket;

public class Keys {

    public string IRCPassword;
    public string TwitchAuthKey;
    public string TwitchClientID;
    public string TwitchSecret;
    public string SRCAuthKey;
    public string DiscordToken;
}

public class StreamData {

    public int NumSamples;

    public int SumViewers;
    public int PeakViewers;

    public int BitsDonated;

    public int NewSubs;
    public int Resubs;
    public int GiftedSubs;
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class TimedEvent : Attribute {

    public int Seconds;

    public TimedEvent(int seconds) => Seconds = seconds;
}

public delegate void SendMessageCallback(string message);

public static class Bot {

    public static Keys Keys;

    public static IRCConnection IRC;
    public static Thread IRCThread;

    public static DiscordClient Discord;
    public static Thread DiscordThread;

    public static Dictionary<string, (SystemCommand Metadata, SystemCommandFn Function)> SystemCommands;
    public static List<Server> Servers = new List<Server>();

    public static Thread TimedEventsThread;

    public static void Start() {
        Keys = JsonConvert.DeserializeObject<Keys>(File.ReadAllText("keys.json"));

        foreach(Server server in Servers) {
            server.Initialize();
        }

        FindSystemCommands();
        StartIRCThread();
        StartDiscord();
        StartTimedEvents();
    }

    public static void FindSystemCommands() {
        SystemCommands = new Dictionary<string, (SystemCommand Metadata, SystemCommandFn Function)>();

        var methods = Debug.FindMethodsWithAttribute<SystemCommand>();
        foreach(var method in methods) {
            SystemCommands[method.Attribute.Name] = (method.Attribute, (SystemCommandFn) Delegate.CreateDelegate(typeof(SystemCommandFn), method.Function));
        }
    }

    public static void StartIRCThread() {
        IRC = new IRCConnection() {
            Server = "irc.twitch.tv",
            Port = 6667,
            User = "Senjougaharabot",
            Nick = "Senjougaharabot",
            Pass = Keys.IRCPassword,
            Channels = Servers.Where(s => s.IRCChannelName != null).Select(s => s.IRCChannelName).ToArray(),
        };

        IRCThread = new Thread(() => {
            bool autoReconnect = true;

            do {
                IRC.EstablishConnection();
                IRC.ProcessMessages(OnEvent);
            } while(autoReconnect);
        });
        IRCThread.Start();
    }

    public static void StartDiscord() {
        IRCThread = new Thread(() => {
            Discord = new DiscordClient();
            Discord.ConnectAsync(OnEvent, Keys.DiscordToken).GetAwaiter().GetResult();
            Task.Delay(Timeout.Infinite);
        });
        IRCThread.Start();
    }

    public static void StartTimedEvents() {
        var timedEvents = Debug.FindMethodsWithAttribute<TimedEvent>();

        TimedEventsThread = new Thread(() => {
            ulong time = 0;
            while(true) {
                if(IRC.Connected) {
                    foreach(var timedEvent in timedEvents) {
                        if(time % (ulong) timedEvent.Attribute.Seconds == 0) {
                            timedEvent.Function.Invoke(null, new object[] { time });
                        }
                    }
                }
                Thread.Sleep(1000);
                time++;
            }
        });
        TimedEventsThread.Start();
    }

    public static void OnEvent(Event e) {
        Task.Run(() => {
            try {
                EventDispatcher dispatcher = new EventDispatcher {
                    Event = e
                };
                dispatcher.Dispatch<IRCPrivMsgEvent>(OnIRCMessage);
                dispatcher.Dispatch<IRCUserStateEvent>(OnIRCState);
                dispatcher.Dispatch<DiscordMessageReceivedEvent>(OnDiscordMessage);
            } catch(Exception e) {
                Console.WriteLine(e.ToString());
            }
        });
    }

    public static void OnIRCMessage(IRCPrivMsgEvent e) {
        Debug.Log("#{0} {1}: {2}", e.Channel, e.Author, e.Message);

        Server server = GetServerByIRCRoom(e.Channel);

        if(e.Parameters.ContainsKey("bits")) {
            server.CurrentStatistics.BitsDonated += Convert.ToInt32(e.Parameters["bits"]);
        }

        string[] badges = e.Parameters["badges"].Split(",");
        Permission permission = Permission.Chatter;
        if(badges.Any(badge => badge.StartsWith("broadcaster"))) {
            permission = Permission.Streamer;
        } else if(badges.Any(badge => badge.StartsWith("moderator"))) {
            permission = Permission.Moderator;
        } else if(badges.Any(badge => badge.StartsWith("vip"))) { // TODO: Check if this is actually named this!!!!
            permission = Permission.VIP;
        } else if(badges.Any(badge => badge.StartsWith("subscriber"))) {
            permission = Permission.Subscriber;
        }

        ExecuteTwitchCommand(e.Message, server, e.Author, permission);
    }

    public static void OnIRCState(IRCUserStateEvent e) {
        Debug.Log("Updated state in channel " + e.Channel);

        Server server = GetServerByIRCRoom(e.Channel);
        if(server.Emotes.Count == 0) {
            string[] emoteSets = e.Parameters["emote-sets"].Split(",");
            foreach(string set in emoteSets) {
                server.Emotes.AddRange(TwitchEmotes.GetSetEmotes(Convert.ToInt32(set)));
            }
            server.Emotes.AddRange(BetterTwitchTV.GetGlobalEmotes());
            List<Emote> ffz = FrankerFaceZ.GetChannelEmotes(e.Channel);
            if(ffz != null) server.Emotes.AddRange(ffz);
            //server.Emotes.AddRange(BetterTwitchTV.GetChannelEmotes(server.TwitchChannelId));
        }
    }

    public static void OnDiscordMessage(DiscordMessageReceivedEvent e) {
        Permission permission = Permission.Chatter;
        if(e.Author.GuildPermissions.Administrator) {
            permission = Permission.Streamer;
        } else if(e.Author.GuildPermissions.BanMembers) {
            permission = Permission.Moderator;
        }

        ExecuteDiscordCommand(e.Message.Channel, e.Message.Content, GetServerByDiscordGuild(e.Channel.Guild.Id), e.Author.Username, permission);
    }

    [TimedEvent(1)]
    public static void GlobalTimerCommandsUpdate(ulong time) {
        foreach(Server server in Servers) {
            foreach(TextCommand command in server.CustomCommands.Values) {
                if(command is TimerCommand) {
                    TimerCommand timerCommand = (TimerCommand) command;
                    if(time % (ulong) timerCommand.Interval == 0) {
                        ExecuteTwitchCommand(command.Name, server, IRC.Nick, Permission.Moderator);
                    }
                }
            }
        }
    }

    [TimedEvent(15)]
    public static void GlobalStreamUpdate(ulong time) {
        foreach(Server server in Servers) {
            TwitchStream stream = Twitch.GetStream(server.IRCChannelName);
            bool online = stream != null;
            bool previous = server.StreamLive;
            if(online && !previous && stream.id != server.LastStreamId) {
                // TODO: On online event
            }
            server.StreamLive = online;

            if(online) {
                server.LastStreamId = stream.id;
                StreamData data = server.CurrentStatistics;
                data.NumSamples++;
                data.SumViewers += stream.view_count;
                data.PeakViewers = Math.Max(data.PeakViewers, stream.view_count);
            }
        }
    }

    [TimedEvent(5)]
    public static void GlobalSerialization(ulong time) {
        foreach(Server server in Servers) {
            server.Serialize();
        }
    }

    public static Server GetServerByIRCRoom(string name) {
        name = name.ToLower();
        return Servers.Find(s => s.IRCChannelName != null && s.IRCChannelName.ToLower() == name);
    }

    public static Server GetServerByDiscordGuild(ulong id) {
        return Servers.Find(s => s.DiscordGuildId == id);
    }

    public static void ExecuteTwitchCommand(string message, Server server, string author, Permission permission) {
        ExecuteCommand(ChannelType.Twitch, msg => IRC.SendPrivMsg(server.IRCChannelName, msg), message, server, author, permission);
    }

    public static void ExecuteDiscordCommand(ISocketMessageChannel channel, string message, Server server, string author, Permission permission) {
        ExecuteCommand(ChannelType.Discord, msg => Discord.SendMesssage(channel, msg), message, server, author, permission);
    }

    public static void ExecuteCommand(ChannelType channelType, SendMessageCallback messageCallback, string message, Server server, string author, Permission permission) {
        string[] splitArray = message.Split(" ");
        string command = splitArray[0].ToLower();
        int firstSpace = message.IndexOf(" ");
        Arguments args = new Arguments() {
            Args = new ArraySegment<string>(splitArray, 1, splitArray.Length - 1),
            FullString = firstSpace != -1 ? message.Substring(firstSpace) : "",
        };

        if(server.Aliases.TryGetValue(command, out Alias alias)) {
            ExecuteCommand(channelType, messageCallback, alias.Command, server, author, permission);
            return;
        }

        if(!server.Users.ContainsKey(author)) {
            server.Users[author] = new User {
                SlotsWins = 0,
                CommandTimeStamps = new Dictionary<string, DateTime>(),
            };
        }

        User user = server.Users[author];

        double timeSinceLastUsage = DateTime.Now.Subtract(user.CommandTimeStamps.GetValueOrDefault(command, new DateTime(0))).TotalSeconds;
        int commandCooldown = server.CommandCooldowns.GetValueOrDefault(command, 0);
        double cooldownRemaining = commandCooldown - timeSinceLastUsage;

        if(cooldownRemaining > 0) {
            if(channelType == ChannelType.Twitch) {
                IRC.SendPrivMsg(server.IRCChannelName, "/w " + author + " Cooldown: " + cooldownRemaining + "s");
            }
            return;
        }

        bool setCooldown = true;
        bool commandExecuted = false;

        if(SystemCommands.TryGetValue(command, out (SystemCommand Metadata, SystemCommandFn Function) systemCommand)) {
            if(systemCommand.Metadata.MinArguments > args.Length()) return;
            if(systemCommand.Metadata.MinPermission > permission) return;
            if((systemCommand.Metadata.AllowedChannelTypes & channelType) == 0) return;
            messageCallback(SystemCommands[command].Function(messageCallback, server, user, author, permission, args, ref setCooldown));
            commandExecuted = true;
        }

        if(server.CustomCommands.TryGetValue(command, out TextCommand textCommand)) {
            messageCallback(textCommand.Execute(messageCallback, server, author, permission, args));
            commandExecuted = true;
        }

        if(setCooldown && commandExecuted && permission != Permission.Streamer) {
            user.CommandTimeStamps[command] = DateTime.Now;
        }
    }
}