using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

public class Server {

    public string Name;
    public string IRCChannelName;
    public Dictionary<string, TextCommand> CustomCommands = new Dictionary<string, TextCommand>();
    public Dictionary<string, Alias> Aliases = new Dictionary<string, Alias>();
    public List<Quote> Quotes = new List<Quote>();

    public int NumSlotsEmotes = 16;

    [JsonIgnore]
    public List<Emote> Emotes = new List<Emote>();
    [JsonIgnore]
    public string TwitchChannelId = null;

    public void Initialize() {
        CustomCommands = new Dictionary<string, TextCommand>(CustomCommands, StringComparer.OrdinalIgnoreCase);
        Aliases = new Dictionary<string, Alias>(Aliases, StringComparer.OrdinalIgnoreCase);
        if(IRCChannelName != null) {
            TwitchChannelId = Twitch.GetUser(IRCChannelName).id;
        }
    }

    public void Serialize() {
        lock(Name) {
            File.WriteAllText("servers/" + Name + ".json", JsonConvert.SerializeObject(this, Formatting.Indented, Program.JsonSettings));
        }
    }

    public bool IsCommandNameInUse(string name, out CommandType type) {
        if(CustomCommands.ContainsKey(name)) {
            type = CommandType.CustomCommand;
            return true;
        }

        if(Aliases.ContainsKey(name)) {
            type = CommandType.Alias;
            return true;
        }

        if(Bot.SystemCommands.ContainsKey(name)) {
            type = CommandType.SystemCommand;
            return true;
        }

        type = CommandType.None;
        return false;
    }
}