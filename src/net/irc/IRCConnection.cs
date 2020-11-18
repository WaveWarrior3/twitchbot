using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Sockets;

public class IRCConnection : IDisposable {

    public string Server;
    public int Port;
    public string User;
    public string Nick;
    public string Pass;
    public string[] Channels;

    private List<string> EventKeywords;

    public TcpClient IRC;
    public NetworkStream Stream;
    public StreamReader Reader;
    public StreamWriter Writer;

    public delegate void OnEvent(IRCEvent e);

    public void Dispose() {
        IRC.Dispose();
        Stream.Dispose();
        Reader.Dispose();
        Writer.Dispose();
    }

    public void EstablishConnection() {
        if(IRC != null) {
            Dispose();
        }

        IRC = new TcpClient(Server, Port);
        Stream = IRC.GetStream();
        Reader = new StreamReader(Stream);
        Writer = new StreamWriter(Stream);
        Writer.AutoFlush = true;

        EventKeywords = new List<string>();
        Assembly assembly = Assembly.GetEntryAssembly();
        foreach(Type type in assembly.GetTypes()) {
            object[] attributes = type.GetCustomAttributes(typeof(IRCEvent), true);
            if(attributes.Length > 0) {
                EventKeywords.Add(((IRCEvent) attributes[0]).MessageType);
            }
        }
    }

    public void ProcessMessages(Action<Event> eventCallback) {
        try {
            Writer.WriteLine("PASS {0}", Pass);
            Writer.WriteLine("NICK {0}", Nick);
            Writer.WriteLine("USER {0}", User);
            Writer.WriteLine("CAP REQ :twitch.tv/commands");
            Writer.WriteLine("CAP REQ :twitch.tv/membership");
            Writer.WriteLine("CAP REQ :twitch.tv/tags");

            foreach(string channel in Channels) {
                Writer.WriteLine("JOIN #{0}", channel);
            }

            while(true) {
                string line;
                string[] splitArray = null;
                Dictionary<string, string> parameters = null;
                while((line = Reader.ReadLine()) != null) {
                    if(EventKeywords.Any(keyword => line.Contains(keyword))) {
                        splitArray = line.Split(" ");
                        parameters = splitArray[0].Split(";").ToDictionary(substring => substring.Split("=")[0], substring => substring.Split("=")[1]);
                    }

                    if(line.Contains("USERSTATE")) {
                        IRCUserStateEvent stateEvent = new IRCUserStateEvent() {
                            Parameters = parameters,
                            Channel = splitArray[3].Substring(1),
                        };
                        eventCallback(stateEvent);
                    }

                    if(line.Contains("PRIVMSG")) {
                        IRCPrivMsgEvent messageEvent = new IRCPrivMsgEvent() {
                            Parameters = parameters,
                            Author = splitArray[1].Until("!").Substring(1),
                            Channel = splitArray[3].Substring(1),
                        };
                        messageEvent.Message = line.After("PRIVMSG #" + messageEvent.Channel + " :");
                        eventCallback(messageEvent);
                    }
                }
            }
        } catch(Exception) {
            Debug.Warning("IRC Connection lost!");
        }
    }

    public void SendPrivMsg(string channel, string message) {
        Writer.WriteLine("PRIVMSG #{0} :{1}", channel, message);
    }
}