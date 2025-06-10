using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace basic_sse_server;

#pragma warning disable S6931

[ApiController]
public class SseController : ControllerBase
{
	[HttpGet("/sse")]
	public async Task Stream()
	{
		var logger = HttpContext.RequestServices.GetRequiredService<ILogger<SseController>>();
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
				logger.LogInformation("Received message: {Message}", data.message);
				var dataJson = JsonSerializer.Serialize(data, jsonOptions);
				await Response.WriteAsync($"data: {dataJson}\n");
				await Response.Body.FlushAsync();
			}
			await Task.Delay(500);
		}
	}
}
