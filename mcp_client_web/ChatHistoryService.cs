using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace mcp_client_web;

public class ChatHistoryService(IMemoryCache cache)
{
	private const string ChatHistoryKey = "ChatHistory";

	public void Save(List<ChatMessage> messages)
	{
		var history = cache.Get<List<ChatMessage>>(ChatHistoryKey) ?? new List<ChatMessage>();
		history.AddRange(messages);
		cache.Set(ChatHistoryKey, history);
	}

	public List<ChatMessage> Load()
	{
		return cache.Get<List<ChatMessage>>(ChatHistoryKey) ?? new List<ChatMessage>();
	}
}