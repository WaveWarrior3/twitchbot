using System;
using System.IO;
using Newtonsoft.Json;

public struct Keys {

    public string IRCPassword;
}

public class Program {

    public static void Main(string[] args) {
        Keys keys = JsonConvert.DeserializeObject<Keys>(File.ReadAllText("keys.json"));

        IRCConnection connection = new IRCConnection() {
            Server = "irc.twitch.tv",
            Port = 6667,
            User = "Senjougaharabot",
            Nick = "Senjougaharabot",
            Pass = keys.IRCPassword,
            Channels = new string[] { "stringflow77" },
        };

        bool autoReconnect = true;

        while(true) {
            connection.EstablishConnection();
            connection.ProcessMessages(OnEvent);
            if(!autoReconnect) break;
        }
    }

    public static void OnEvent(IRCEvent e) {
        EventDispatcher dispatcher = new EventDispatcher {
            Event = e
        };
        dispatcher.Dispatch<IRCPrivMsgEvent>(OnIRCMessage);
        dispatcher.Dispatch<IRCUserStateEvent>(OnIRCState);
    }

    public static void OnIRCMessage(IRCPrivMsgEvent e) {
        Debug.Log("#{0} {1}: {2}", e.Channel, e.Author, e.Message);
    }

    public static void OnIRCState(IRCUserStateEvent e) {
        Debug.Log("Joined channel " + e.Channel);
    }
}
