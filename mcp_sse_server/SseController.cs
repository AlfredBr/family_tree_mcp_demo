using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace mcp_sse_server;

[ApiController]
[Route("/")]
public class SseController : ControllerBase
{
	[HttpGet("stream")]
	public async Task Stream()
	{
		Response.Headers.Append("Content-Type", "text/event-stream");
		var bridge = HttpContext.RequestServices.GetRequiredService<Bridge>();
		var jsonOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};

		while (!HttpContext.RequestAborted.IsCancellationRequested)
		{
			if (bridge.Reader.TryRead(out var msg))
			{
				var data = new
				{
					timestamp = DateTime.Now.ToString("O"),
					message = msg ?? string.Empty
				};
				if (string.IsNullOrEmpty(data.message))
				{
					continue;
				}
				var dataJson = JsonSerializer.Serialize(data, jsonOptions);
				await Response.WriteAsync($"data: {dataJson}\n");
				await Response.Body.FlushAsync();
			}
			await Task.Delay(1000);
		}
	}
}
