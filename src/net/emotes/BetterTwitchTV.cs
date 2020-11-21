using System.Globalization;
using System.Numerics;
using System.Collections.Generic;

public static class BetterTwitchTV {

    public static List<Emote> GetGlobalEmotes() {
        return GetEmotesInternal("emotes/global");
    }

    public static List<Emote> GetChannelEmotes(string channelId) {
        return GetEmotesInternal("users/twitch/" + channelId.ToLower());
    }

    private static List<Emote> GetEmotesInternal(string url) {
        if(!Http.Get(out HttpResponse response, "https://api.betterttv.net/3/cached/" + url)) {
            return null;
        }

        dynamic emotes = response.Unpack();
        List<Emote> result = new List<Emote>();
        foreach(dynamic emote in emotes) {
            result.Add(new Emote {
                Code = emote.code,
                Id = -1,
                Set = -1, // both of these return a 96-bit integer, which are awkward to parse
            });
        }
        return result;
    }
}