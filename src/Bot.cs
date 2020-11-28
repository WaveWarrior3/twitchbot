using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

public struct Keys {

    public string IRCPassword;
    public string TwitchAuthKey;
    public string TwitchClientID;
    public string TwitchSecret;
    public string SRCAuthKey;
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

public static class Bot {

    public static Keys Keys;

    public static IRCConnection IRC;
    public static Thread IRCThread;

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
            EventDispatcher dispatcher = new EventDispatcher {
                Event = e
            };
            dispatcher.Dispatch<IRCPrivMsgEvent>(OnIRCMessage);
            dispatcher.Dispatch<IRCUserStateEvent>(OnIRCState);
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

        ExecuteCommand(e.Message, server, e.Author, permission);
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

    [TimedEvent(1)]
    public static void GlobalTimerCommandsUpdate(ulong time) {
        foreach(Server server in Servers) {
            foreach(TextCommand command in server.CustomCommands.Values) {
                if(command is TimerCommand) {
                    TimerCommand timerCommand = (TimerCommand) command;
                    if(time % (ulong) timerCommand.Interval == 0) {
                        ExecuteCommand(command.Name, server, IRC.Nick, Permission.Moderator);
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
        return Servers.Find(s => s.IRCChannelName.ToLower() == name);
    }

    public static void ExecuteCommand(string message, Server server, string author, Permission permission) {
        string[] splitArray = message.Split(" ");
        string command = splitArray[0].ToLower();
        int firstSpace = message.IndexOf(" ");
        Arguments args = new Arguments() {
            Args = new ArraySegment<string>(splitArray, 1, splitArray.Length - 1),
            FullString = firstSpace != -1 ? message.Substring(firstSpace) : "",
        };

        if(server.Aliases.TryGetValue(command, out Alias alias)) {
            ExecuteCommand(alias.Command, server, author, permission);
            return;
        }

        if(SystemCommands.TryGetValue(command, out (SystemCommand Metadata, SystemCommandFn Function) systemCommand)) {
            if(systemCommand.Metadata.MinArguments > args.Length()) return;
            IRC.SendPrivMsg(server.IRCChannelName, SystemCommands[command].Function(server, author, permission, args));
        }

        if(server.CustomCommands.TryGetValue(command, out TextCommand textCommand)) {
            IRC.SendPrivMsg(server.IRCChannelName, textCommand.Execute(server, author, permission, args));
        }
    }
}