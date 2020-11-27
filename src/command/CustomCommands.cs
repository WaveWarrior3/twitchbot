using System;
using Newtonsoft.Json;

public class TextCommand {

    public const string Type = "text";

    public string Name;
    public string Message;

    public virtual string Execute(Server server, string author, Permission permission, Arguments args) {
        return FormatMessage(Message, server, author, permission, args);
    }

    public virtual string FormatMessage(string message, Server server, string author, Permission permission, Arguments args) {
        message = message.Replace("%author%", author);
        message = message.Replace("%args%", args.Join(0, args.Length(), " "));
        message = ReplaceWithArgument(message, "rand", (arg) => {
            string[] choices = arg.Split("|");
            return Random.Next(choices);
        });
        return message;
    }

    protected string ReplaceWithArgument(string message, string key, Func<string, string> fn) {
        int start = 0;
        while((start = message.IndexOf("%" + key)) != -1) {
            int equals = message.IndexOf("=", start) + 1;
            int end = message.IndexOf("%", equals) + 1;
            string argument = message[(equals)..(end - 1)];
            message = message.Replace(message[start..end], fn(argument));
        }
        return message;
    }
}

public class CounterCommand : TextCommand {

    public new const string Type = "counter";

    public int Counter;

    public override string Execute(Server server, string author, Permission permission, Arguments args) {
        if(args.Length() > 0) {
            if(args.TryInt(0, out int newValue)) {
                Counter = newValue;
            } else {
                int numPlus = args[0].NumOccurrences("+");
                int numMinus = args[0].NumOccurrences("-");
                int total = numPlus - numMinus;
                Counter += total;
            }
        }

        server.Serialize();
        return FormatMessage(Message, server, author, permission, args);
    }

    public override string FormatMessage(string message, Server server, string author, Permission permission, Arguments args) {
        message = base.FormatMessage(message, server, author, permission, args);
        message = message.Replace("%counter%", Counter.ToString());
        return message;
    }
}

public class FractionCommand : TextCommand {

    public new const string Type = "fraction";

    public int Numerator;
    public int Denominator;

    public override string Execute(Server server, string author, Permission permission, Arguments args) {
        if(args.Length() > 0) {
            if(args.Matches("setnumerator \\d+")) {
                Numerator = args.Int(1);
            } else if(args.Matches("setdenominator \\d+")) {
                Denominator = args.Int(1);
            } else {
                int numPlus = args[0].NumOccurrences("+");
                int numMinus = args[0].NumOccurrences("-");
                Numerator += numPlus;
                Denominator += numPlus + numMinus;
            }
        }

        return FormatMessage(Message, server, author, permission, args);
    }

    public override string FormatMessage(string message, Server server, string author, Permission permission, Arguments args) {
        message = base.FormatMessage(message, server, author, permission, args);
        message = message.Replace("%numerator%", Numerator.ToString());
        message = message.Replace("%denominator%", Denominator.ToString());
        message = message.Replace("%fraction%", ((float) Numerator / (float) Denominator).ToString());
        return message;
    }
}

public class TimerCommand : TextCommand {

    public new const string Type = "timer";

    public int Interval;

    public override string Execute(Server server, string author, Permission permission, Arguments args) {
        if(args.Length() > 0) {
            if(args.TryInt(0, out int newValue)) {
                Interval = newValue;
                return "The interval of the timer-command " + Name + " has been set to " + newValue + " seconds.";
            }
        }

        return FormatMessage(Message, server, author, permission, args);
    }

    public override string FormatMessage(string message, Server server, string author, Permission permission, Arguments args) {
        message = base.FormatMessage(message, server, author, permission, args);
        message = message.Replace("%interval%", Interval.ToString());
        return message;
    }
}