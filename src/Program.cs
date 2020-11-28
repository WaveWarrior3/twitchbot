using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Program {

    public static JsonSerializerSettings JsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

    public static void Main(string[] args) {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        RegisterServer("stringflow77");
        Bot.Start();
    }

    public static void RegisterServer(string name) {
        string path = "servers/" + name + ".json";
        Server server;
        if(!File.Exists(path)) {
            server = new Server();
            server.Name = name;
            server.IRCChannelName = name;
        } else {
            server = JsonConvert.DeserializeObject<Server>(File.ReadAllText(path), JsonSettings);
        }

        Bot.Servers.Add(server);
    }
}