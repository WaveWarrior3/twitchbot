using System;
using System.Linq;

public delegate string SystemCommandFn(Server server, string author, Arguments args);

public static class SystemCommandsImpl {

    public static Random Random = new Random();

    [SystemCommand("!command", 2)]
    public static string Command(Server server, string author, Arguments args) {
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
    public static string Test(Server server, string author, Arguments args) {
        return "Your connection is still working.";
    }

    [SystemCommand("!roll")]
    public static string Roll(Server server, string author, Arguments args) {
        int upperBound = 100;
        if(args.Length() > 0) {
            if(!args.Matches("\\d+")) return "Correct Syntax: !roll (upper bound)";
            upperBound = args.Int(0);
        }
        return "The roll returns " + Random.Next(upperBound) + "!";
    }
}