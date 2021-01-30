using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Program {

    public static JsonSerializerSettings JsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

    // Temp code to convert old json data to the new format
    private static void ConvertOldData() {
        Server s = new Server() {
            Name = "pokeguy",
        };
        s.Initialize();
        dynamic old = JsonConvert.DeserializeObject(File.ReadAllText("pokeguy.json"));
        foreach(var v in old.aliases) {
            s.Aliases.Add(v.Name, new Alias {
                Name = v.Name,
                Command = v.Value,
            });
        }
        foreach(var c in old.commands) {
            switch(c.type.ToString()) {
                case "text":
                    s.CustomCommands.Add(c.name.ToString(), new TextCommand {
                        Name = c.name.ToString(),
                        Message = c.message.ToString(),
                    });
                    break;
                case "fraction":
                    s.CustomCommands.Add(c.name.ToString(), new FractionCommand {
                        Name = c.name.ToString(),
                        Message = c.message.ToString(),
                        Numerator = Convert.ToInt32(c.numerator.ToString()),
                        Denominator = Convert.ToInt32(c.denominator.ToString()),
                    });
                    break;
                case "counter":
                    s.CustomCommands.Add(c.name.ToString(), new CounterCommand {
                        Name = c.name,
                        Message = c.message,
                        Counter = Convert.ToInt32(c.counter),
                    });
                    break;
                default:
                    break;
            }
        }
        foreach(var q in old.quotes) {
            s.Quotes.Add(new Quote {
                Message = q.text.ToString(),
                Quotee = q.quotee.ToString(),
            });
        }
        s.Serialize();
    }

    public static void Main(string[] args) {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        //RegisterServer("stringflow", 0);
        RegisterServer("gunnermaniac", "gunnermaniac", 131475899483684864);
        RegisterServer("gchat", null, 665362563349086289);
        RegisterServer("pokeguy", "pokeguy", 492036928485720074);
        Bot.Start();
    }

    public static void RegisterServer(string name, string twitch, ulong discord) {
        string path = "servers/" + name + ".json";
        Server server;
        if(!File.Exists(path)) {
            server = new Server();
            server.Name = name;
        } else {
            server = JsonConvert.DeserializeObject<Server>(File.ReadAllText(path), JsonSettings);
        }

        server.IRCChannelName = twitch;
        server.DiscordGuildId = discord;
        Bot.Servers.Add(server);
    }
}