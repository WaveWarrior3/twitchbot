using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public static class TwitchEmotes {

    public static List<Emote> GetGlobalEmotes() {
        return GetChannelEmotes("0");
    }

    public static List<Emote> GetChannelEmotes(string channelId, string set = null) {
        if(!Http.Get(out HttpResponse response, "https://api.twitchemotes.com/api/v4/channels/" + channelId)) {
            return null;
        }

        dynamic emotes = response.Unpack().emotes;
        List<Emote> result = new List<Emote>();
        foreach(dynamic emote in emotes) {
            if(set == null || set == emote.emoticon_set.ToString()) {
                result.Add(new Emote {
                    Code = emote.code,
                    Id = emote.id,
                    Set = emote.emoticon_set
                });
            }
        }

        return result;
    }

    public static List<Emote> GetSetEmotes(string setIds) {
        if(!Http.Get(out HttpResponse response, "https://api.twitchemotes.com/api/v4/sets?id=" + setIds)) {
            return null;
        }

        dynamic sets = response.Unpack();
        List<Emote> ret = new List<Emote>();
        foreach(dynamic set in sets) {
            List<Emote> emotes = GetChannelEmotes(set.channel_id.ToString(), set.set_id.ToString());
            if(emotes != null) ret.AddRange(emotes);
        }

        return ret;
    }
}