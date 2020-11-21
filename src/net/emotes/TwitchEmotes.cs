using System.Linq;
using System.Collections.Generic;

public static class TwitchEmotes {

    public static List<Emote> GetGlobalEmotes() {
        return GetChannelEmotes("0");
    }

    public static List<Emote> GetChannelEmotes(string channelId) {
        if(!Http.Get(out HttpResponse response, "https://api.twitchemotes.com/api/v4/channels/" + channelId)) {
            return null;
        }

        dynamic emotes = response.Unpack().emotes;
        List<Emote> result = new List<Emote>();
        foreach(dynamic emote in emotes) {
            result.Add(new Emote {
                Code = emote.code,
                Id = emote.id,
                Set = emote.emoticon_set
            });
        }

        return result;
    }

    public static List<Emote> GetSetEmotes(int setId) {
        if(!Http.Get(out HttpResponse response, "https://api.twitchemotes.com/api/v4/sets", "id", setId.ToString())) {
            return null;
        }

        if(response.Message == "[]") return new List<Emote>();

        dynamic set = response.Unpack();
        List<Emote> channelEmotes = GetChannelEmotes(set[0].channel_id.ToString());
        return channelEmotes.Where(emote => emote.Set == setId).ToList();
    }
}