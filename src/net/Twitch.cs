using Newtonsoft.Json;

public class TwitchUser {

    public string id;
    public string login;
    public string display_name;
    public string type;
    public string broadcaster_type;
    public string description;
    public string profile_image_url;
    public string offline_image_url;
    public int view_count;
    public string email;
}

public class TwitchStream {

    public string id;
    public string user_id;
    public string user_name;
    public string game_id;
    public string type;
    public string title;
    public int view_count;
    public string started_at;
    public string language;
    public string thumbnail_url;
}

public static class Twitch {

    public static string GenerateAuthToken() {
        if(!Http.Post(out HttpResponse response, "https://id.twitch.tv/oauth2/token", "client_id", Bot.Keys.TwitchClientID, "client_secret", Bot.Keys.TwitchSecret, "grant_type", "client_credentials")) {
            return null;
        }

        return response.Unpack().access_token;
    }

    public static TwitchUser GetUser(string name) {
        if(!Http.Get(out HttpResponse response, "https://api.twitch.tv/helix/users?login=" + name, "Authorization", Bot.Keys.TwitchAuthKey, "Client-Id", Bot.Keys.TwitchClientID)) {
            return null;
        }

        dynamic data = response.Unpack().data;
        if(data.Count == 0) return null;

        return JsonConvert.DeserializeObject<TwitchUser>(data[0].ToString());
    }

    public static TwitchStream GetStream(string name) {
        if(!Http.Get(out HttpResponse response, "https://api.twitch.tv/helix/streams?user_login=" + name, "Authorization", Bot.Keys.TwitchAuthKey, "Client-Id", Bot.Keys.TwitchClientID)) {
            return null;
        }

        dynamic data = response.Unpack().data;
        if(data.Count == 0) return null;

        return JsonConvert.DeserializeObject<TwitchStream>(data[0].ToString());
    }
}