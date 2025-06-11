using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Timeout;

namespace blazor_frontend;

public class McpChatClient(HttpClient httpClient)
{
	public async Task<string?> Chat([FromBody] string message, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return null;
		}

		// Create a timeout policy with a 60-second limit
		var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(60));

		// Use the policy when sending your request
		var response = await timeoutPolicy.ExecuteAsync(async ct =>
			await httpClient.PostAsJsonAsync("/chat", message, ct), cancellationToken);

		if (response.IsSuccessStatusCode)
		{
			return await response.Content.ReadAsStringAsync(cancellationToken);
		}
		var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
		throw new HttpRequestException($"Chat request failed with status code {response.StatusCode}: {errorContent}");
	}
}
