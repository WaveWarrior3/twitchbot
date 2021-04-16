using System;
using System.Threading;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using org.mariuszgromada.math.mxparser;

public delegate string SystemCommandFn(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown);

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class SystemCommand : Attribute {

    public string Name;
    public Permission MinPermission;
    public int MinArguments;
    public ChannelType AllowedChannelTypes;

    public SystemCommand(string name, Permission minPermission = Permission.Chatter, int minArguments = 0, ChannelType allowedChannelTypes = ChannelType.All) {
        Name = name;
        MinPermission = minPermission;
        MinArguments = minArguments;
        AllowedChannelTypes = allowedChannelTypes;
    }
}

public class Quote {

    public string Quotee;
    public string Message;
}

public class Alias {

    public string Name;
    public string Command;
}

public static class SystemCommandsImpl {

    [SystemCommand("!command", Permission.Moderator, 2)]
    public static string Command(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        string commandName = args[1].ToLower();
        if(args.Matches("add .+ .+")) {
            if(server.IsCommandNameInUse(commandName, out CommandType type)) return "The name " + commandName + " is already in use by " + type.Format() + ".";
            string message = args.Join(2, args.Length(), " ");
            server.CustomCommands[commandName] = new TextCommand {
                Name = commandName,
                Message = message,
            };
            return "Command " + commandName + " has been added.";
        } else if(args.Matches("edit .+ .+")) {
            if(!server.CustomCommands.TryGetValue(commandName, out TextCommand textCommand)) return "Command " + commandName + " does not exist.";
            textCommand.Message = args.Join(2, args.Length(), " ");
            return "Command " + commandName + " has been edited.";
        } else if(args.Matches("del .+")) {
            if(!server.CustomCommands.TryGetValue(commandName, out TextCommand textCommand)) return "Command " + commandName + " does not exist.";
            server.CustomCommands.Remove(commandName);
            return "Command " + commandName + " has been removed.";
        } else if(args.Matches("transform .+ .+")) {
            if(!server.CustomCommands.TryGetValue(commandName, out TextCommand oldCommand)) return "Command " + commandName + " does not exist.";
            server.CustomCommands.Remove(commandName);
            string type = args[2].ToLower();

            switch(type) {
                case TextCommand.Type:
                    server.CustomCommands[commandName] = new TextCommand {
                        Name = oldCommand.Name,
                        Message = oldCommand.Message,
                    };
                    break;
                case CounterCommand.Type:
                    server.CustomCommands[commandName] = new CounterCommand {
                        Name = oldCommand.Name,
                        Message = oldCommand.Message,
                        Counter = 0,
                    };
                    break;
                case FractionCommand.Type:
                    server.CustomCommands[commandName] = new FractionCommand {
                        Name = oldCommand.Name,
                        Message = oldCommand.Message,
                        Numerator = 0,
                        Denominator = 0,
                    };
                    break;
                case TimerCommand.Type:
                    server.CustomCommands[commandName] = new TimerCommand {
                        Name = oldCommand.Name,
                        Message = oldCommand.Message,
                        Interval = int.MaxValue,
                    };
                    break;
                default:
                    server.CustomCommands[commandName] = oldCommand;
                    return type + " is not a valid command type.";
            }

            return "Command " + commandName + " has been transformed to a " + type + "-command.";
        }

        setCooldown = false;
        return null;
    }

    [SystemCommand("!alias", Permission.Moderator, 2)]
    public static string Alias(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        string aliasName = args[1].ToLower();
        if(args.Matches("add .+ .+")) {
            if(server.IsCommandNameInUse(aliasName, out CommandType type)) return "The name " + aliasName + " is already in use by " + type.Format() + ".";
            string command = args.Join(2, args.Length(), " ");
            server.Aliases[aliasName] = new Alias {
                Name = aliasName,
                Command = command,
            };
            return "Alias " + aliasName + " has been added.";
        } else if(args.Matches("edit .+ .+")) {
            if(!server.Aliases.TryGetValue(aliasName, out Alias alias)) return "Alias " + aliasName + " does not exist.";
            alias.Command = args.Join(2, args.Length(), " ");
            return "Alias " + aliasName + " has been edited.";
        } else if(args.Matches("del .+")) {
            if(!server.Aliases.TryGetValue(aliasName, out Alias alias)) return "Alias " + aliasName + " does not exist.";
            server.Aliases.Remove(aliasName);
            return "Alias " + aliasName + " has been removed.";
        }

        setCooldown = false;
        return null;
    }

    [SystemCommand("!test")]
    public static string Test(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        return "Your connection is still working.";
    }

    [SystemCommand("!roll")]
    public static string Roll(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        int upperBound = 100;
        if(args.Length() > 0) {
            if(!args.TryInt(0, out int ret)) return "Correct Syntax: !roll (upper bound)";
            upperBound = ret;
        }
        return "The roll returns " + (Random.Next(0, Math.Max(1, upperBound)) + 1) + ".";
    }

    [SystemCommand("!slots", AllowedChannelTypes = ChannelType.Twitch)]
    public static string Slots(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        if((args.Matches("emotes \\d+") || args.Matches("setemotes \\d+")) && permission >= Permission.Moderator) {
            int newEmotes = args.Int(1);
            if(newEmotes < 1) return "The number of emotes must be at least 1.";

            server.NumSlotsEmotes = newEmotes;
            setCooldown = false;
            return "Slots use a pool of " + newEmotes + " emotes now (1/" + (int) (Math.Pow(newEmotes, 2)) + " chance to win).";
        }

        if(args.Matches("odds")) {
            setCooldown = false;
            return "1/" + (int) (Math.Pow(server.NumSlotsEmotes, 2)) + " chance to win.";
        }

        if(args.Matches("winners")) {
            setCooldown = false;
            string[] winners = server.Users.Where(u => u.Value.SlotsWins > 0).Select(u => u.Key).ToArray();
            if(winners.Length == 0) return "No !slots winners, yet.";
            else return string.Join(", ", winners);
        }

        const int slotsSize = 3;

        string[] emotePool = server.Emotes.OrderBy(x => Random.Next()).Take(server.NumSlotsEmotes).Select(emote => emote.Code).ToArray();
        string[] emotes = new string[slotsSize];
        for(int i = 0; i < emotes.Length; i++) {
            emotes[i] = Random.Next(emotePool);
        }

        int numUniques = emotes.Distinct().Count();
        string slots = string.Join(" | ", emotes);

        if(numUniques == 1) {
            user.SlotsWins++;
            messageCallback(slots);
            return author + " has won the slots! " + emotes[0];
        } else {

        }

        return slots;
    }

    [SystemCommand("!uptime", AllowedChannelTypes = ChannelType.Twitch)]
    public static string Uptime(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        TwitchStream stream = Twitch.GetStream(server.IRCChannelName);
        if(stream == null) return server.IRCChannelName + " is not live.";

        DateTime starttime = DateTime.Parse(stream.started_at);
        TimeSpan uptime = DateTime.Now - starttime;

        string hours = uptime.Hours + " hour" + (uptime.Hours == 1 ? "" : "s");
        string minutes = uptime.Minutes + " minute" + (uptime.Minutes == 1 ? "" : "s");
        string seconds = (uptime.TotalSeconds % 60) + " second" + (uptime.Seconds == 1 ? "" : "s");

        return "Uptime: " + hours + " " + minutes + " " + seconds;
    }

    public static Dictionary<string, string> SrcCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [SystemCommand("!src")]
    public static string Src(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        if(args.Matches("clear")) {
            SrcCache.Clear();
            return "Cache cleared.";
        }

        string query = args.Join(0, args.Length(), " ");
        if(SrcCache.ContainsKey(query)) {
            return SrcCache[query];
        }

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
        string formattedTime = timestamp.ToString("g");
        string result = "";

        if(runs.Count == 1) {
            result = place + StringFunctions.PlaceEnding(place) + " place in " + game.Name + " " + category.Name + " is held by " + run.Player.Name + " with a time of " + formattedTime + " | Video: " + run.VideoLink;
        } else {
            runs.Last().Player.Name = "and " + runs.Last().Player.Name;
            result = place + StringFunctions.PlaceEnding(place) + " place in " + game.Name + " " + category.Name + " is a " + runs.Count + "-way tie between " + string.Join(", ", runs.Select(run => run.Player.Name)) + " with a time of " + formattedTime;
        }

        SrcCache[query] = result;
        return result;
    }

    [SystemCommand("!quote")]
    public static string Quote(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        if(args.Length() > 0 && permission >= Permission.Moderator) {
            if(args[0].EqualsIgnoreCase("add")) {
                if(!args.Matches("add .+ .+")) return "Correct Syntax: !quote add (quotee) (message)";
                string quotee = args[1];
                string message = args.Join(2, args.Length(), " ");
                server.Quotes.Add(new Quote { Quotee = quotee, Message = message });
                return "Quote #" + server.Quotes.Count() + " by " + quotee + " has been added.";
            } else if(args[0].EqualsIgnoreCase("edit")) {
                if(!args.Matches("edit \\d+ .+ .+")) return "Correct Syntax: !quote edit (quote number) (quotee) (message)";
                int quoteNumber = args.Int(1) - 1;
                string quotee = args[2];
                string message = args.Join(3, args.Length(), " ");
                if(quoteNumber < 0 || quoteNumber >= server.Quotes.Count) return "Quote #" + args[1] + " does not exist.";
                server.Quotes[quoteNumber] = new Quote { Quotee = quotee, Message = message };
                return "Quote #" + args[1] + " has been edited.";
            } else if(args[0].EqualsIgnoreCase("del")) {
                if(!args.Matches("del \\d+")) return "Correct Syntax: !quote del (quote number)";
                int quoteNumber = args.Int(1) - 1;
                if(quoteNumber < 0 || quoteNumber >= server.Quotes.Count) return "Quote #" + args[1] + " does not exist.";
                server.Quotes.RemoveAt(quoteNumber);
                return "Quote #" + args[1] + " has been removed.";
            } else if(args[0].EqualsIgnoreCase("search")) {
                string[] words = args.Sub(1, args.Length()).Select(x => x.ToLower()).ToArray();
                HashSet<Quote> matchingQuotes = new HashSet<Quote>();
                foreach(Quote q in server.Quotes) {
                    string message = q.Message.ToLower();
                    if(words.Any(w => message.Contains(w))) {
                        matchingQuotes.Add(q);
                    }
                }

                if(matchingQuotes.Count == 0) return "No matching quotes found.";

                foreach(Quote q in matchingQuotes) {
                    messageCallback(FormatQuote(server, q));
                }

                return null;
            }
        }

        if(server.Quotes.Count == 0) return "No quotes have been added, yet.";

        if(args.Matches("\\d+")) {
            int quoteNumber = args.Int(0) - 1;
            if(quoteNumber < 0 || quoteNumber >= server.Quotes.Count) return "Quote #" + args[1] + " does not exist.";
            return FormatQuote(server, server.Quotes[quoteNumber]);
        }

        return FormatQuote(server, Random.Next(server.Quotes));

        string FormatQuote(Server server, Quote quote) {
            int id = server.Quotes.IndexOf(quote) + 1;
            return "Quote #" + id + " by " + quote.Quotee + ": \"" + quote.Message + "\"";
        }
    }

    [SystemCommand("!choose", Permission.Chatter, 1)]
    public static string Choose(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        string result = Random.Next(args.Join(0, args.Length(), " ").Split("|")).Trim();
        if(result.StartsWith(".") || result.StartsWith("!")) {
            return "AngryVoHiYo";
        }

        return result;
    }

    private static readonly string[] ConchshellAnswers = {
        "It is certain.",
        "Without a doubt.",
        "Yes - definitely.",
        "As I see it, yes.",
        "Signs point to yes.",
        "Ask again later.",
        "Better not tell you now.",
        "Cannot predict now.",
        "Don't count on it.",
        "My reply is no.",
        "My sources say no.",
        "Outlook not so good.",
        "Very doubtful.",
    };

    [SystemCommand("!conchshell")]
    public static string Conchshell(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        return Random.Next(ConchshellAnswers);
    }

    [SystemCommand("!isredbar")]
    public static string IsRedbar(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        if(!args.Matches("\\d+/\\d+")) {
            return "Correct Syntax: !isredbar (fraction)";
        }

        string[] splitArray = args[0].Split("/");
        if(!(int.TryParse(splitArray[0], out int currentHp) && int.TryParse(splitArray[1], out int maxHp))) {
            return "Correct Syntax: !isredbar (fraction)";
        }

        if(maxHp == 0) return "AngryVoHiYo";
        bool redbar = currentHp * 48 / maxHp < 10;
        return currentHp + "/" + maxHp + " is " + (redbar ? "" : "not ") + "red bar.";
    }

    [SystemCommand("!istorrent")]
    public static string IsTorrent(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        if(!args.Matches("\\d+/\\d+")) {
            return "Correct Syntax: !istorrent (fraction)";
        }

        string[] splitArray = args[0].Split("/");
        if(!(int.TryParse(splitArray[0], out int currentHp) && int.TryParse(splitArray[1], out int maxHp))) {
            return "Correct Syntax: !istorrent (fraction)";
        }

        if(maxHp == 0) return "AngryVoHiYo";
        bool torrent = currentHp <= maxHp / 3;
        return currentHp + "/" + maxHp + " is " + (torrent ? "" : "not ") + "torrent.";
    }

    [SystemCommand("!isprime")]
    public static string IsPrime(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        if(!args.Matches("\\d+")) {
            return "Correct Syntax: !isprime (number)";
        }

        int number = args.Int(0);
        bool prime = true;

        if(number <= 1) prime = false;
        else if(number % 2 == 0) prime = false;
        else if(number != 2) {
            int max = (int) Math.Floor(Math.Sqrt(number));
            for(int i = 3; i <= max; i += 2) {
                if(number % i == 0) {
                    prime = false;
                    break;
                }
            }
        }

        return number + " is " + (prime ? "" : "not ") + "a prime number.";
    }

    [SystemCommand("!data", AllowedChannelTypes = ChannelType.Twitch)]
    public static string Data(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        DateTime date = DateTime.Now.Date;

        if(!server.Statistics.ContainsKey(date)) return "No data has been collected for today.";

        StreamData data = server.Statistics[date];
        float viewerAverage = (float) data.SumViewers / (float) data.NumSamples;

        return string.Format("Average viewers: {0}, Peak viewers: {1}, Bits donated: {2}", viewerAverage, data.PeakViewers, data.BitsDonated);
    }

    [SystemCommand("!expr")]
    public static string Expr(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        if(args.Length() == 0) return "Correct Syntax: !expr (math expression)";
        Expression expr = new Expression(args.Join(0, args.Length(), " "));
        if(!expr.checkSyntax()) return "Invalid expression.";

        return expr.calculate().ToString();
    }

    [SystemCommand("!winner", Permission.Moderator, AllowedChannelTypes = ChannelType.Twitch)]
    public static string Winner(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        messageCallback("And the winning user is...");
        Thread.Sleep(2500);
        return Random.Next(Twitch.GetChatters(server.IRCChannelName)) + "!";
    }

    [SystemCommand("!loser", Permission.Moderator, AllowedChannelTypes = ChannelType.Twitch)]
    public static string Loser(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        messageCallback("And the losing user is...");
        Thread.Sleep(2500);
        return Random.Next(Twitch.GetChatters(server.IRCChannelName)) + "!";
    }

    [SystemCommand("!cooldown")]
    public static string Cooldown(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        if(args[0].EqualsIgnoreCase("set") && permission >= Permission.Moderator) {
            if(!args.Matches("set .+ \\d+")) return "Correct Syntax: !cooldown set (command) (cooldown in seconds)";
            string command = args[1];
            int cooldown = args.Int(2);
            if(!server.CustomCommands.ContainsKey(command) && !Bot.SystemCommands.ContainsKey(command)) return "A command with the name " + command + " was not found.";
            server.CommandCooldowns[command] = cooldown;
            return "The cooldown for " + command + " has been set to " + cooldown + " seconds.";
        } else if(args.Length() > 0) {
            string command = args[0];
            if(!server.CustomCommands.ContainsKey(command) && !Bot.SystemCommands.ContainsKey(command)) return "A command with the name " + command + " was not found.";
            return "The cooldown for " + command + " is currently set to " + server.CommandCooldowns.GetValueOrDefault(command, 0) + " seconds.";
        }

        return null;
    }

    [SystemCommand("!liftcooldown", Permission.Moderator)]
    public static string LiftCooldown(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        if(!args.Matches(".+ .+")) return "Correct Syntax: !liftcooldown (user) (command name)";

        string username = args[0];
        string command = args[1];

        if(!server.Users.ContainsKey(username)) return "User " + username + " was not found.";
        if(!server.CustomCommands.ContainsKey(command) && !Bot.SystemCommands.ContainsKey(command)) return "A command with the name " + command + " was not found.";

        server.Users[username].CommandTimeStamps[command] = new DateTime(0);
        return "The " + command + " cooldown for " + username + " has been lifted.";
    }

    [SystemCommand("!critrate")]
    public static string CritRate(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        if(!args.Matches(".+")) return "Correct Syntax: !critrate (pokemon name)";
        string pokemon = args[0];
        RbySpecies species = Rby.Species.Where(s => s.Name.EqualsIgnoreCase(pokemon)).FirstOrDefault();

        if(species == default) {
            return "The pokemon " + pokemon + " does not exist.";
        } else {
            int x = (int) (species.BaseSpeed / 2);
            float regular = x / 2.56f;
            float high = Math.Min(255, x * 8) / 2.56f;
            return "Regular Crit: " + regular + "%, High Crit: " + high + "%";
        }
    }

    [SystemCommand("!metronome")]
    public static string Metronome(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        return "Enemy Clefairy used " + Random.Next(Gsc.MetronomeMoves).Name + "!";
    }

    [SystemCommand("!randmon")]
    public static string RandMon(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        return Random.Next(Gsc.RandSpecies).Name;
    }

    [SystemCommand("!randteam")]
    public static string RandTeam(SendMessageCallback messageCallback, Server server, User user, string author, Permission permission, Arguments args, ref bool setCooldown) {
        List<GscSpecies> pool = new List<GscSpecies>(Gsc.RandSpecies);
        List<GscSpecies> team = new List<GscSpecies>();
        for(int i = 0; i < 6; i++) {
            GscSpecies species = Random.Next(pool);
            pool.Remove(species);
            team.Add(species);
        }

        team.Sort((s1, s2) => s1.Name.CompareTo(s2.Name));

        return string.Join(", ", team.Select(s => s.Name));
    }
}