using System;

public delegate string SystemCommandFn(Server server, string author, Permission permission, Arguments args);

public static class SystemCommandsImpl {

    public static Random Random = new Random();

    [SystemCommand("!command", 2)]
    public static string Command(Server server, string author, Permission permission, Arguments args) {
        string commandName = args[1].ToLower();
        if(args.Matches("add .+ .+")) {
            if(server.IsCommandNameInUse(commandName)) return "Command name " + commandName + " is already in use."; // TODO: Reason?
            string message = args.Join(2, " ");
            server.TextCommands[commandName] = new TextCommand {
                Name = commandName,
                Message = message,
            };
            server.Serialize();
            return "Command " + commandName + " has been added.";
        } else if(args.Matches("edit .+ .+")) {
            if(!server.TextCommands.TryGetValue(commandName, out TextCommand textCommand)) return "Command " + commandName + " does not exist.";
            textCommand.Message = args.Join(2, " ");
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
}