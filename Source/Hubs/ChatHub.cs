using Microsoft.AspNetCore.SignalR;

namespace AspNetCore.API.Hubs;

public sealed class ChatHub : Hub<IChatHub>
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine(Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Ping(string text)
    {
        await Clients.All.Pong(text);
    }
}

public interface IChatHub
{
    public Task Pong(string text);
}

// {"protocol":"json","version":1}
// {"type":6}
// {"type":1,"target":"Ping","arguments":["text"]}