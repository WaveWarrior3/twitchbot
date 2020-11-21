using System;
using System.Linq;

public static class SlotsCommand {

    public static Random Random = new Random();

    [SystemCommand("!slots")]
    public static string Slots(Server server, string author, Permission permission, Arguments args) {
        if((args.Matches("emotes \\d+") || args.Matches("setemotes \\d+")) && permission >= Permission.Moderator) {
            int newEmotes = args.Int(1);
            if(newEmotes < 1) return "The number of emotes must be at least 1.";

            server.NumSlotsEmotes = newEmotes;
            server.Serialize();
            return "Slots use a pool of " + newEmotes + " emotes now (1/" + (int) (Math.Pow(newEmotes, 2)) + " chance to win).";
        }

        if(args.Matches("odds")) {
            return "1/" + (int) (Math.Pow(server.NumSlotsEmotes, 2)) + " chance to win.";
        }

        const int slotsSize = 3;

        string[] emotePool = server.Emotes.OrderBy(x => Random.Next()).Take(server.NumSlotsEmotes).Select(emote => emote.Code).ToArray();
        string[] emotes = new string[slotsSize];
        for(int i = 0; i < emotes.Length; i++) {
            emotes[i] = emotePool[Random.Next(emotePool.Length)];
        }

        int numUniques = emotes.Distinct().Count();

        Bot.IRC.SendPrivMsg(server.IRCChannelName, string.Join(" | ", emotes));
        if(numUniques == 1) {
            Bot.IRC.SendPrivMsg(server.IRCChannelName, author + " has won the slots! " + emotes[0]);
        }

        return null;
    }
}