using Microsoft.AspNetCore.Mvc;

namespace mcp_sse_server;

[ApiController]
[Route("[controller]")]
public class SseController : ControllerBase
{
	[HttpGet("stream")]
	public async Task Stream()
	{
		Response.Headers.Append("Content-Type", "text/event-stream");

		while(!HttpContext.RequestAborted.IsCancellationRequested)
		{
			var data = $"Server time: {DateTime.Now:O}";
			await Response.WriteAsync($"data: {data}\n\n");
			await Response.Body.FlushAsync();

			await Task.Delay(1000);  // send every second
		}
	}
}
