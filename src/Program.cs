using System;
using System.Linq;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Discord;

public class Program {

    public static JsonSerializerSettings JsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

    // Temp code to convert old json data to the new format
    private static void ConvertOldData() {
        Server s = new Server() {
            Name = "hollow_gaze",
        };
        s.Initialize();
        dynamic old = JsonConvert.DeserializeObject(File.ReadAllText("franchewbacca.json"));
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

        Bot senjo = new Bot("SenjougharaBot", Bot.Keys.SenjoIRCPassword, Bot.Keys.SenjoDiscordToken, "with Gunner's feelings");
        senjo.Servers.Add(MakeServer("gunnermaniac", "gunnermaniac", 131475899483684864, (bot, stream) => {
            bot.Discord.SendMessage(131475899483684864, 265583889827758081, "@everyone Gunner is LIVE <https://www.twitch.tv/gunnermaniac>\n" + stream.title);
        }));
        senjo.Servers.Add(MakeServer("gchat", null, 665362563349086289));
        senjo.Servers.Add(MakeServer("franchewbacca", "franchewbacca", 0));
        senjo.Start();

        Bot umi = new Bot("Umi_Sonoda_Bot", Bot.Keys.UmiIRCPassword, Bot.Keys.UmiDiscordToken);
        umi.Servers.Add(MakeServer("pokeguy", "pokeguy", 492036928485720074));
        umi.Start();

        Bot poochers = new Bot("DesertCandy", Bot.Keys.DesertCandyIRCPassword, Bot.Keys.DesertCandyDiscordToken);
        poochers.Servers.Add(MakeServer("shiru", "shiru666", 297359525504090112));
        poochers.Start();

        Bot betino = new Bot("Betinobot", Bot.Keys.BetinoIRCPassword, Bot.Keys.BetinoDiscordToken);
        betino.Servers.Add(MakeServer("hollow_gaze", "hollow_gaze", 297359525504090112));
        betino.Start();

        senjo.Discord.Client.SetActivityAsync(new Game("with Gunner's feelings", ActivityType.Playing)).GetAwaiter().GetResult();
    }

    public static Server MakeServer(string name, string twitch, ulong discord, Action<Bot, TwitchStream> onLive = null) {
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
        server.OnStreamLive = onLive;
        return server;
    }
}