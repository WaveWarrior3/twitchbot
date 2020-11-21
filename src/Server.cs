using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

public class Server {

    public string Name;
    public string IRCChannelName;
    public Dictionary<string, TextCommand> TextCommands;

    [JsonIgnore]
    public List<Emote> Emotes = new List<Emote>();
    public int NumSlotsEmotes;

    public void Initialize() {
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