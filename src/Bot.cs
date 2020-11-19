using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

public struct Keys {

    public string IRCPassword;
}

public static class Bot {

    public static Keys Keys;

    public static IRCConnection IRC;
    public static Thread IRCThread;

    public static Dictionary<string, (SystemCommand Metadata, SystemCommandFn Function)> SystemCommands;
    public static List<Server> Servers = new List<Server>();

    public static void Start() {
        Keys = JsonConvert.DeserializeObject<Keys>(File.ReadAllText("keys.json"));

        foreach(Server server in Servers) {
            server.Initialize();
        }

        FindSystemCommands();
        StartIRCThread();
    }

    public static void FindSystemCommands() {
        SystemCommands = new Dictionary<string, (SystemCommand, SystemCommandFn)>();

        Assembly asm = Assembly.GetExecutingAssembly();
        foreach(Type type in asm.GetTypes()) {
            foreach(MethodInfo method in type.GetMethods()) {
                object[] attributes = method.GetCustomAttributes(typeof(SystemCommand), false);
                if(attributes.Length > 0) {
                    SystemCommand attrib = (SystemCommand) attributes[0];
                    SystemCommands[attrib.Name.ToLower()] = (attrib, (SystemCommandFn) Delegate.CreateDelegate(typeof(SystemCommandFn), method));
                }
            }
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

            while(true) {
                IRC.EstablishConnection();
                IRC.ProcessMessages(OnEvent);
                if(!autoReconnect) break;
            }
        });
        IRCThread.Start();
    }

    public static void OnEvent(Event e) {
        EventDispatcher dispatcher = new EventDispatcher {
            Event = e
        };
        dispatcher.Dispatch<IRCPrivMsgEvent>(OnIRCMessage);
        dispatcher.Dispatch<IRCUserStateEvent>(OnIRCState);
    }

    public static void OnIRCMessage(IRCPrivMsgEvent e) {
        Debug.Log("#{0} {1}: {2}", e.Channel, e.Author, e.Message);

        string[] splitArray = e.Message.Split(" ");
        string command = splitArray[0].ToLower();
        int firstSpace = e.Message.IndexOf(" ");
        Arguments args = new Arguments() {
            Args = new ArraySegment<string>(splitArray, 1, splitArray.Length - 1),
            FullString = firstSpace != -1 ? e.Message.Substring(firstSpace) : "",
        };
        Server server = Servers.Find(s => s.IRCChannelName.ToLower() == e.Channel.ToLower());

        ExecuteCommand(command, server, e.Author, args);
    }

    public static void OnIRCState(IRCUserStateEvent e) {
        Debug.Log("Updated state in channel " + e.Channel);
    }

    public static void ExecuteCommand(string command, Server server, string author, Arguments args) {
        if(SystemCommands.TryGetValue(command, out (SystemCommand Metadata, SystemCommandFn Function) systemCommand)) {
            if(systemCommand.Metadata.MinArguments > args.Length()) return;
            IRC.SendPrivMsg(server.IRCChannelName, SystemCommands[command].Function(server, author, args));
        }

        if(server.TextCommands.TryGetValue(command, out TextCommand textCommand)) {
            IRC.SendPrivMsg(server.IRCChannelName, textCommand.Execute(server, author, args));
        }
    }
}