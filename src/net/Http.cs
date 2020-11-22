using System;
using System.Linq;
using System.Collections.Generic;
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

public enum HttpMethod {
    Get,
    Post,
}

public static class Http {

    public static HttpClient Client = new HttpClient();

    public static bool Get(out HttpResponse response, string url, params string[] parameters) {
        return MakeRequest(out response, HttpMethod.Get, url, parameters);
    }

    public static bool Post(out HttpResponse response, string url, params string[] parameters) {
        return MakeRequest(out response, HttpMethod.Post, url, parameters);
    }

    private static bool MakeRequest(out HttpResponse response, HttpMethod method, string url, string[] parameters) {
        try {
            Client.DefaultRequestHeaders.Clear();
            Dictionary<string, string> header = new Dictionary<string, string>();
            for(int i = 0; i < parameters.Length; i += 2) {
                Client.DefaultRequestHeaders.Add(parameters[i], parameters[i + 1]);
                header[parameters[i]] = parameters[i + 1];
            }
            HttpResponseMessage msg = null;
            switch(method) {
                case HttpMethod.Get: msg = Client.GetAsync(url).Result; break;
                case HttpMethod.Post: msg = Client.PostAsync(url, new FormUrlEncodedContent(header)).Result; break;
            }

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