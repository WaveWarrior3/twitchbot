using System.Globalization;
using System.Numerics;
using System.Collections.Generic;

public static class BetterTwitchTV {

    public static List<Emote> GetGlobalEmotes() {
        if(!Http.Get(out HttpResponse response, "https://api.betterttv.net/3/cached/emotes/global")) {
            return null;
        }

        return ParseEmotes(response.Unpack());
    }

    public static List<Emote> GetChannelEmotes(string channelId) {
        if(!Http.Get(out HttpResponse response, "https://api.betterttv.net/3/cached/users/twitch/" + channelId)) {
            return null;
        }

        return ParseEmotes(response.Unpack().sharedEmotes);
    }

    private static List<Emote> ParseEmotes(dynamic array) {
        List<Emote> result = new List<Emote>();
        foreach(dynamic emote in array) {
            result.Add(new Emote {
                Code = emote.code,
                Id = -1,
                Set = -1, // both of these return a 96-bit integer, which are awkward to parse
            });
        }
        return result;
    }
}