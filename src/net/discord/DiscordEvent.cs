using Discord.WebSocket;

public class DiscordMessageReceivedEvent : Event {

    public SocketMessage Message;
    public SocketGuildUser Author;
    public SocketGuildChannel Channel;
}