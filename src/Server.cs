using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

public struct Quote {

    public string Quotee;
    public string Message;
}

public class Server {

    public string Name;
    public string IRCChannelName;
    public Dictionary<string, TextCommand> TextCommands = new Dictionary<string, TextCommand>();
    public List<Quote> Quotes = new List<Quote>();

    [JsonIgnore]
    public List<Emote> Emotes = new List<Emote>();
    public int NumSlotsEmotes = 16;

    [JsonIgnore]
    public string TwitchChannelId = null;

    public void Initialize() {
        if(IRCChannelName != null) {
            TwitchChannelId = Twitch.GetUser(IRCChannelName).id;
        }
    }

    public void Serialize() {
        lock(Name) {
            File.WriteAllText("servers/" + Name + ".json", JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }

    public bool IsCommandNameInUse(string name) {
        name = name.ToLower();
        return TextCommands.ContainsKey(name) ||
               Bot.SystemCommands.ContainsKey(name);
    }
}