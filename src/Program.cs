using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Program {

    public static void Main(string[] args) {
        RegisterServer("stringflow77");
        Bot.Start();
    }

    public static void RegisterServer(string name) {
        string path = "servers/" + name + ".json";
        Server server;
        if(!File.Exists(path)) {
            server = new Server {
                Name = name,
                IRCChannelName = name,
                TextCommands = new Dictionary<string, TextCommand>(),
            };
        } else {
            server = JsonConvert.DeserializeObject<Server>(File.ReadAllText(path));
        }

        Bot.Servers.Add(server);
    }
}