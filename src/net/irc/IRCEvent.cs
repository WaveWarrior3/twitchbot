using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class IRCEvent : Attribute {

    public string MessageType;

    public IRCEvent(string type) => MessageType = type;
}

[IRCEvent("PRIVMSG")]
public class IRCPrivMsgEvent : Event {

    public Dictionary<string, string> Parameters;
    public string Author;
    public string Channel;
    public string Message;
}

[IRCEvent("USERSTATE")]
public class IRCUserStateEvent : Event {

    public Dictionary<string, string> Parameters;
    public string Channel;
}