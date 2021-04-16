using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

public class User {

    public Dictionary<string, DateTime> CommandTimeStamps;
    public int SlotsWins;
}

public class Server {

    public string Name;
    public string IRCChannelName;
    public ulong DiscordGuildId;

    public Dictionary<string, TextCommand> CustomCommands = new Dictionary<string, TextCommand>();
    public Dictionary<string, Alias> Aliases = new Dictionary<string, Alias>();
    public List<Quote> Quotes = new List<Quote>();
    public Dictionary<string, int> CommandCooldowns = new Dictionary<string, int>();
    public Dictionary<string, User> Users = new Dictionary<string, User>();
    public Dictionary<DateTime, StreamData> Statistics = new Dictionary<DateTime, StreamData>();

    public int NumSlotsEmotes = 16;
    public bool StreamLive = false;
    public string LastStreamId = "";

    [JsonIgnore]
    public Action<Bot, TwitchStream> OnStreamLive;

    [JsonIgnore]
    public List<Emote> Emotes = new List<Emote>();
    [JsonIgnore]
    public string TwitchChannelId = null;

    [JsonIgnore]
    public StreamData CurrentStatistics {
        get {
            DateTime date = DateTime.Now.Date;
            if(!Statistics.ContainsKey(date)) {
                Statistics[date] = new StreamData();
            }

            return Statistics[date];
        }
    }

    public void Initialize() {
        // Initalize all dictionaries to be case insensitive.
        CustomCommands = new Dictionary<string, TextCommand>(CustomCommands, StringComparer.OrdinalIgnoreCase);
        Aliases = new Dictionary<string, Alias>(Aliases, StringComparer.OrdinalIgnoreCase);
        CommandCooldowns = new Dictionary<string, int>(CommandCooldowns, StringComparer.OrdinalIgnoreCase);
        Users = new Dictionary<string, User>(Users, StringComparer.OrdinalIgnoreCase);
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

    public bool TryGetCommand(string name, out TextCommand command) {
        if(CustomCommands.TryGetValue(name, out command)) {
            return true;
        }

        if(Aliases.TryGetValue(name, out Alias alias)) {
            command = CustomCommands[alias.Command];
            return true;
        }

        command = null;
        return false;
    }

    public List<string> FindAliases(string command) {
        List<string> ret = new List<string>();
        foreach(Alias alias in Aliases.Values) {
            if(alias.Command.EqualsIgnoreCase(command)) {
                ret.Add(alias.Name);
            }
        }

        return ret;
    }
}