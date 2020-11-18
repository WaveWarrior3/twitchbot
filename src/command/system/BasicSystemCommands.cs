using System;

public delegate string SystemCommandFn(string author, Arguments args);

public static class BasicSystemCommands {

    public static Random Random = new Random();

    [SystemCommand("!test")]
    public static string Test(string author, Arguments args) {
        return "Your connection is still working.";
    }

    [SystemCommand("!roll")]
    public static string Roll(string author, Arguments args) {
        int upperBound = 100;
        if(args.Length() > 0) {
            if(!args.Matches("\\d+")) return "Correct Syntax: !roll (upper bound)";
            upperBound = args.Int(0);
        }
        return "The roll returns " + Random.Next(upperBound) + "!";
    }
}