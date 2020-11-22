using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

public delegate string SystemCommandFn(Server server, string author, Permission permission, Arguments args);

public static class SystemCommandsImpl {

    public static Random Random = new Random();

    [SystemCommand("!command", 2)]
    public static string Command(Server server, string author, Permission permission, Arguments args) {
        string commandName = args[1].ToLower();
        if(args.Matches("add .+ .+")) {
            if(server.IsCommandNameInUse(commandName)) return "Command name " + commandName + " is already in use."; // TODO: Reason?
            string message = args.Join(2, args.Length(), " ");
            server.TextCommands[commandName] = new TextCommand {
                Name = commandName,
                Message = message,
            };
            server.Serialize();
            return "Command " + commandName + " has been added.";
        } else if(args.Matches("edit .+ .+")) {
            if(!server.TextCommands.TryGetValue(commandName, out TextCommand textCommand)) return "Command " + commandName + " does not exist.";
            textCommand.Message = args.Join(2, args.Length(), " ");
            server.Serialize();
            return "Command " + commandName + " has been edited.";
        } else if(args.Matches("del .+")) {
            if(!server.TextCommands.TryGetValue(commandName, out TextCommand textCommand)) return "Command " + commandName + " does not exist.";
            server.TextCommands.Remove(commandName);
            server.Serialize();
            return "Command " + commandName + " has been removed.";
        }

        return null;
    }

    [SystemCommand("!test")]
    public static string Test(Server server, string author, Permission permission, Arguments args) {
        return "Your connection is still working.";
    }

    [SystemCommand("!roll")]
    public static string Roll(Server server, string author, Permission permission, Arguments args) {
        int upperBound = 100;
        if(args.Length() > 0) {
            if(!args.Matches("\\d+")) return "Correct Syntax: !roll (upper bound)";
            upperBound = args.Int(0);
        }
        return "The roll returns " + Random.Next(upperBound) + "!";
    }

    [SystemCommand("!uptime")]
    public static string Uptime(Server server, string author, Permission permission, Arguments args) {
        TwitchStream stream = Twitch.GetStream(server.IRCChannelName);
        if(stream == null) return server.IRCChannelName + " is not live.";

        DateTime starttime = DateTime.Parse(stream.started_at);
        TimeSpan uptime = DateTime.Now - starttime;

        string hours = uptime.Hours + " hour" + (uptime.Hours == 1 ? "" : "s");
        string minutes = uptime.Minutes + " minute" + (uptime.Minutes == 1 ? "" : "s");
        string seconds = (uptime.TotalSeconds % 60) + " second" + (uptime.Seconds == 1 ? "" : "s");

        return "Uptime: " + hours + " " + minutes + " " + seconds;
    }

    [SystemCommand("!src")]
    public static string Src(Server server, string author, Permission permission, Arguments args) {
        if(!args.TryInt(args.Length() - 1, out int place)) {
            return "Correct Syntax: !src (game handle) (category) (place)";
        }

        string gameHandle = args[0];
        SRCGame game = SRC.GetGame(gameHandle);
        if(game == null) {
            return "Error: Unable to find the game '" + gameHandle + "' on SRC";
        }

        List<SRCCategory> categories = SRC.GetCategories(game);
        if(categories == null) {
            return "Error: Unable retrieve the game's categories";
        }

        string categoryName = args.Join(1, args.Length() - 1, " ");
        SRCCategory category = categories.Find(cat => cat.Name.EqualsIgnoreCase(categoryName));
        if(category == null) {
            return "Error: Unable to find the category '" + categoryName + "'.";
        }

        List<SRCRun> runs = SRC.GetLeaderboardPlace(category, place);
        if(runs == null) {
            return "Error: No run in " + place + StringFunctions.PlaceEnding(place) + " exists.";
        }

        SRCRun run = runs[0];
        TimeSpan timestamp = XmlConvert.ToTimeSpan(run.Time);
        string formattedTime = "";
        if(timestamp.Hours > 0) formattedTime += timestamp.Hours + ":";
        if(timestamp.Minutes > 0 || timestamp.Hours > 0) formattedTime += timestamp.Minutes + ":";
        if(timestamp.Seconds > 0 || timestamp.Minutes > 0 || timestamp.Hours > 0) formattedTime += timestamp.Seconds;
        if(timestamp.Milliseconds > 0) formattedTime += "." + timestamp.Milliseconds;

        if(runs.Count == 1) {
            return place + StringFunctions.PlaceEnding(place) + " place in " + game.Name + " " + category.Name + " is held by " + run.Player.Name + " with a time of " + formattedTime + " | Video: " + run.VideoLink;
        } else {
            runs.Last().Player.Name = "and " + runs.Last().Player.Name;
            return place + StringFunctions.PlaceEnding(place) + " place in " + game.Name + " " + category.Name + " is a " + runs.Count + "-way tie between " + string.Join(", ", runs.Select(run => run.Player.Name)) + " with a time of " + formattedTime;
        }
    }
}