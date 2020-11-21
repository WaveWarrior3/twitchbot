using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

public class HttpResponse {

    public int Code;
    public string Message;

    public dynamic Unpack() {
        return JsonConvert.DeserializeObject(Message);
    }
}

public static class Http {

    public static HttpClient Client = new HttpClient();

    public static bool Get(out HttpResponse response, string url, params string[] parameters) {
        try {
            for(int i = 0; i < parameters.Length / 2; i += 2) {
                string seperator = i == 0 ? "?" : "&";
                url += seperator + parameters[i] + "=" + Uri.EscapeDataString(parameters[i + 1]);
            }
            HttpResponseMessage msg = Client.GetAsync(url).Result;

            if(msg.StatusCode == HttpStatusCode.OK) {
                response = new HttpResponse {
                    Code = (int) msg.StatusCode,
                    Message = msg.Content.ReadAsStringAsync().Result,
                };
                return true;
            }
        } catch(Exception) {
            Debug.Warning("Http Request to '{0}' failed!", url);
        }

        response = null;
        return false;
    }
}