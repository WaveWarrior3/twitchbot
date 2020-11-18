using System;

public class Event { }

public class EventDispatcher {

    public Event Event;

    public void Dispatch<T>(Action<T> func) where T : Event {
        if(Event.GetType() == typeof(T)) {
            func((T) Event);
        }
    }
}