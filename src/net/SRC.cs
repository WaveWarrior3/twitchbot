using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public class SRCGame {

    public string Id;
    public string Name;
    public Dictionary<string, string> Links;
}

public class SRCCategory {

    public string Id;
    public string Name;
    public bool Misc;
    public Dictionary<string, string> Links;
}

public class SRCVariable {

    public string Id;
    public string Name;
    public bool Mandatory;
    public List<string> Values;
    public string Default;
    public Dictionary<string, string> Links;
}

public class SRCRunner {

    public string Uri;
    public string Type;

    private string Name_;
    public string Name {
        get {
            if(Name_ == null) {
                Name_ = SRC.GetRunnerName(Uri, Type);
            }

            return Name_;
        }
        set {
            Name_ = value;
        }
    }
}

public class SRCRun {

    public int Place;
    public string Id;
    public string VideoLink;
    public string Comment;
    public SRCRunner Player;
    public string Time;
}

public static class SRC {

    public static SRCGame GetGame(string handle) {
        if(!MakeAuthorizedRequest("https://www.speedrun.com/api/v1/games/" + handle, out dynamic data)) {
            return null;
        }

        return new SRCGame {
            Id = data.id,
            Name = data.names.international,
            Links = ParseLinks(data.links),
        };
    }

    public static List<SRCCategory> GetCategories(SRCGame game) {
        if(game == null || !MakeAuthorizedRequest(game.Links["categories"], out dynamic data)) {
            return null;
        }

        List<SRCCategory> categories = new List<SRCCategory>();
        foreach(dynamic category in data) {
            categories.Add(new SRCCategory {
                Id = category.id,
                Name = category.name,
                Misc = category.miscellaneous,
                Links = ParseLinks(category.links),
            });
        }

        return categories;
    }

    public static List<SRCVariable> GetVariables(SRCCategory category) {
        if(category == null || !MakeAuthorizedRequest(category.Links["variables"], out dynamic data)) {
            return null;
        }

        List<SRCVariable> variables = new List<SRCVariable>();
        foreach(dynamic variable in data) {
            List<string> values = new List<string>();
            foreach(dynamic key in variable.values.values) {
                values.Add(key.Name);
            }

            variables.Add(new SRCVariable {
                Id = variable.id,
                Name = variable.name,
                Mandatory = variable.mandatory,
                Default = variable.values["default"],
                Links = ParseLinks(variable.links),
            });
        }

        return variables;
    }

    public static List<SRCRun> GetLeaderboard(SRCCategory category, string parameters) {
        if(category == null || !MakeAuthorizedRequest(category.Links["leaderboard"] + parameters, out dynamic data)) {
            return null;
        }

        List<SRCRun> runs = new List<SRCRun>();
        foreach(dynamic runStruct in data.runs) {
            dynamic run = runStruct.run;
            runs.Add(new SRCRun {
                Place = runStruct.place,
                Id = run.Id,
                VideoLink = (run.videos == null || run.videos.links == null) ? "Unavailable" : run.videos.links[0].uri,
                Comment = run.comment,
                Player = new SRCRunner { // TODO: Support multiple runners
                    Uri = run.players[0].uri,
                    Type = run.players[0].rel,
                },
                Time = run.times.primary,
            });
        }

        return runs;
    }

    public static string GetRunnerName(string uri, string type) {
        if(!MakeAuthorizedRequest(uri, out dynamic data)) {
            return null;
        }

        return type == "user" ? data.names.international.ToString() : data.name.ToString();
    }

    // Assumes all variables at their defaults
    public static List<SRCRun> GetLeaderboardPlace(SRCCategory category, int place) {
        List<SRCVariable> variables = GetVariables(category);
        StringBuilder queryParameters = new StringBuilder();
        foreach(SRCVariable variable in variables) {
            if(variable.Default != null) {
                queryParameters.Append("?var-" + variable.Id + "=" + variable.Default);
            }
        }

        List<SRCRun> leaderboard = GetLeaderboard(category, queryParameters.ToString());

        return leaderboard.Where(run => run.Place == place).ToList();
    }

    private static bool MakeAuthorizedRequest(string url, out dynamic data) {
        data = null;
        if(!Http.Get(out HttpResponse response, url, "X-API-Key", Bot.Keys.SRCAuthKey, "User-Agent", "TBSRC/1.0")) {
            return false;
        }

        data = response.Unpack().data;
        return true;
    }

    private static Dictionary<string, string> ParseLinks(dynamic array) {
        Dictionary<string, string> links = new Dictionary<string, string>();
        foreach(dynamic link in array) {
            links[link.rel.ToString()] = link.uri.ToString();
        }
        return links;
    }
}