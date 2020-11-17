using System;
using System.Collections.Generic;

public class IRCEvent { }

[IRCEventType("PRIVMSG")]
public class IRCPrivMsgEvent : IRCEvent {

    public Dictionary<string, string> Parameters;
    public string Author;
    public string Channel;
    public string Message;
}

[IRCEventType("USERSTATE")]
public class IRCUserStateEvent : IRCEvent {

    public Dictionary<string, string> Parameters;
    public string Channel;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class IRCEventType : Attribute {

    public string MessageType;

    public IRCEventType(string type) => MessageType = type;
}

public class EventDispatcher {

    public IRCEvent Event;

    public void Dispatch<T>(Action<T> func) where T : IRCEvent {
        if(Event.GetType() == typeof(T)) {
            func((T) Event);
        }
    }
}