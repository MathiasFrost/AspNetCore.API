using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]/[action]")]
public sealed class TestController : ControllerBase
{
    [HttpGet]
    public async Task Get(CancellationToken token)
    {
        Response.ContentType = "text/event-stream";
        for (var i = 0; !token.IsCancellationRequested; i++) // Infinite loop to keep the stream open
        {
            await Task.Delay(1000, token); // Simulate some delay

            // Construct and send an SSE message
            await Response.WriteAsync($"data: Event {i}\n\n", cancellationToken: token);
            await Response.Body.FlushAsync(token); // Important to flush the response stream
        }
    }
}