using System.Collections.Generic;

public static class FrankerFaceZ {

    public static List<Emote> GetChannelEmotes(string channelName) {
        if(!Http.Get(out HttpResponse response, "https://api.frankerfacez.com/v1/room/" + channelName.ToLower())) {
            return null;
        }

        dynamic data = response.Unpack();
        int set = data.room.set;
        dynamic emotes = data.sets[set.ToString()].emoticons;
        List<Emote> result = new List<Emote>();
        foreach(dynamic emote in emotes) {
            result.Add(new Emote {
                Code = emote.name,
                Id = emote.id,
                Set = set,
            });
        }
        return result;
    }
}