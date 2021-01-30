using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

public class DiscordClient {

    public DiscordSocketClient Client;
    public Action<Event> EventCallback;

    public DiscordClient() {
        Client = new DiscordSocketClient();
        Client.MessageReceived += MessageReceivedAsync;
    }

    public async Task ConnectAsync(Action<Event> eventCallback, string token) {
        EventCallback = eventCallback;
        await Client.LoginAsync(TokenType.Bot, token);
        await Client.StartAsync();
    }

    public void SendMesssage(ISocketMessageChannel channel, string message) {
        channel.SendMessageAsync(message).GetAwaiter().GetResult();
    }

    private async Task MessageReceivedAsync(SocketMessage message) {
        // Ignore non-guild message for now.
        if(message.Author is SocketGuildUser) {
            EventCallback(new DiscordMessageReceivedEvent() {
                Message = message,
                Author = (SocketGuildUser) message.Author,
                Channel = (SocketGuildChannel) message.Channel,
            });
        }
    }
}