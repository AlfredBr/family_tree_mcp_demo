using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using FamilyTreeApp;

var llmModel = "o4-mini";

Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine($"🌳 Family Tree Chatbot powered by {llmModel}");
Console.WriteLine("Ask me anything about the family tree!");
Console.WriteLine("Type 'exit' to quit.");
Console.WriteLine();
Console.WriteLine("Examples:");
Console.WriteLine("- List all people in the family");
Console.WriteLine("- Who are the parents of Elizabeth Carter?");
Console.WriteLine("- What is the relationship between Emily Smith and William Carter?");
Console.WriteLine("- Get details for person p5.");
Console.WriteLine();
Console.ResetColor();

// Get OpenAI API key
string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
	Console.Write("Enter your OpenAI API key: ");
	apiKey = Console.ReadLine();
}

if (string.IsNullOrWhiteSpace(apiKey))
{
	Console.WriteLine("API key is required.");
	return;
}

// get the path to the mcp_library project
var mcpServerProject = Path.Combine(
	AppContext.BaseDirectory,
	"..",
	"..",
	"..",
	"..",
	"mcp_server",
	"mcp_server.csproj"
);
if (!File.Exists(mcpServerProject))
{
	Console.WriteLine($"MCP library project not found at {mcpServerProject}.");
	return;
}

// Start the MCP server process
var mcpProcess = new System.Diagnostics.Process
{
	StartInfo = new System.Diagnostics.ProcessStartInfo
	{
		FileName = "dotnet",
		Arguments = $"run --project \"{mcpServerProject}\"",
		RedirectStandardInput = true,
		RedirectStandardOutput = true,
		RedirectStandardError = true,
		UseShellExecute = false,
		CreateNoWindow = true,
		WorkingDirectory = AppContext.BaseDirectory,
	},
};

Console.WriteLine("Starting MCP server...");
mcpProcess.Start();

// grab the input and output streams for the MCP server
var mcpInput = mcpProcess.StandardInput;
var mcpOutput = mcpProcess.StandardOutput;

// start a background task to read the MCP server's standard output and standard error streams
_ = Task.Run(async () =>
{
	string? line;
	while ((line = await mcpProcess.StandardError.ReadLineAsync()) != null)
	{
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"[MCP STDERR] {line}");
		Console.ResetColor();
	}
});

// Wait a moment for MCP server to start
await Task.Delay(10_000);

// Create HTTP client for OpenAI API
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

// Define mapping between OpenAI function names and MCP method names
var functionToMethodMap = new Dictionary<string, string>
{
	{ "GetFamily", "FamilyTreeApp.FamilyTools.GetFamily" },
	{ "GetPerson", "FamilyTreeApp.FamilyTools.GetPerson" },
	{ "AddPerson", "FamilyTreeApp.FamilyTools.AddPerson" },
	{ "UpdatePerson", "FamilyTreeApp.FamilyTools.UpdatePerson" },
	{ "DeletePerson", "FamilyTreeApp.FamilyTools.DeletePerson" },
	{ "AddSpouse", "FamilyTreeApp.FamilyTools.AddSpouse" },
	{ "AddChild", "FamilyTreeApp.FamilyTools.AddChild" }
};

// Define system prompt with pre-prompt instructions

var systemPrompt = string.Join(" ", Prompt.PrePromptInstructions);

// Initialize chat history
var chatHistory = new List<Dictionary<string, object>>
{
	new() { ["role"] = "system", ["content"] = systemPrompt },
};

Console.WriteLine("\nAsk me anything about the family tree!");
Console.WriteLine("Examples:");
Console.WriteLine("- List all people in the family.");
Console.WriteLine("- Who are the parents of Elizabeth Carter?");
Console.WriteLine("- What is the relationship between Emily Smith and William Carter?");
Console.WriteLine("- Get details for person p5.");
Console.WriteLine();

// Chat loop
while (true)
{
	Console.ForegroundColor = ConsoleColor.Green;
	Console.Write("You: ");
	Console.ResetColor();

	// Read user input
	string? userInput = Console.ReadLine();
	if (string.IsNullOrWhiteSpace(userInput))
	{
		continue;
	}
	if (userInput.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) ||
		userInput.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
	{
		break;
	}

	chatHistory.Add(new() { ["role"] = "user", ["content"] = userInput });

	Console.ForegroundColor = ConsoleColor.DarkGray;
	Console.WriteLine("Assistant is thinking...");
	Console.ResetColor();

	try
	{
		// Create request body for OpenAI API
		var requestBody = new
		{
			model = llmModel,
			messages = chatHistory,
			tools = new object[]
			{
				new
				{
					type = "function",
					function = new
					{
						name = "GetFamily",
						description = "Get a list of all of the people in a family.",
						parameters = new { type = "object", properties = new { } },
					},
				},
				new
				{
					type = "function",
					function = new
					{
						name = "GetPerson",
						description = "Get a particular member of the family by id.",
						parameters = new
						{
							type = "object",
							properties = new
							{
								id = new { type = "string", description = "The id of the person in the family" }
							},
							required = new[] { "id" }
						}
					},
				},
				new
				{
					type = "function",
					function = new
					{
						name = "AddPerson",
						description = "Add a new person to the family. This creates a new family member record.",
						parameters = new
						{
							type = "object",
							properties = new
							{
								personJson = new { type = "string", description = "The JSON representation of the person to add." }
							},
							required = new[] { "personJson" }
						}
					},
				},
				new
				{
					type = "function",
					function = new
					{
						name = "UpdatePerson",
						description = "Update an existing person in the family. This modifies the details of a family member.",
						parameters = new
						{
							type = "object",
							properties = new
							{
								id = new { type = "string", description = "The id of the person to update" },
								personJson = new { type = "string", description = "The JSON representation of the updated person." }
							},
							required = new[] { "id", "personJson" }
						}
					},
				},
				new
				{
					type = "function",
					function = new
					{
						name = "DeletePerson",
						description = "Delete a person from the family. This removes a family member record completely.",
						parameters = new
						{
							type = "object",
							properties = new
							{
								id = new { type = "string", description = "The id of the person to delete" }
							},
							required = new[] { "id" }
						}
					},
				},
				new
				{
					type = "function",
					function = new
					{
						name = "AddSpouse",
						description = "Add a spouse relationship between two family members.",
						parameters = new
						{
							type = "object",
							properties = new
							{
								id = new { type = "string", description = "The id of the primary person" },
								spouseId = new { type = "string", description = "The id of the person to add as a spouse" }
							},
							required = new[] { "id", "spouseId" }
						}
					},
				},
				new
				{
					type = "function",
					function = new
					{
						name = "AddChild",
						description = "Add a child relationship to a parent family member.",
						parameters = new
						{
							type = "object",
							properties = new
							{
								parentId = new { type = "string", description = "The id of the parent person" },
								childId = new { type = "string", description = "The id of the person to add as a child" }
							},
							required = new[] { "parentId", "childId" }
						}
					},
				}
			},
			tool_choice = "auto",
		};

		var content = new StringContent(
			JsonSerializer.Serialize(requestBody),
			Encoding.UTF8,
			"application/json"
		);

#pragma warning disable S1075
		const string chatCompletionsUri = "https://api.openai.com/v1/chat/completions";
#pragma warning restore S1075

		// Send request to OpenAI
		var response = await httpClient.PostAsync(
			chatCompletionsUri,
			content
		);

		var responseString = await response.Content.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(responseString);
		var choices = doc.RootElement.GetProperty("choices");
		var message = choices[0].GetProperty("message");

		// Handle tool calls if present
		if (message.TryGetProperty("tool_calls", out var toolCalls))
		{
			// Add the assistant message with tool calls to chat history
			chatHistory.Add(JsonSerializer.Deserialize<Dictionary<string, object>>(message.GetRawText())!);

			// Process each tool call
			foreach (var toolCall in toolCalls.EnumerateArray())
			{
				var function = toolCall.GetProperty("function");
				var openAiName = function.GetProperty("name").GetString()!;
				var arguments = function.GetProperty("arguments").GetRawText();
				var toolCallId = toolCall.GetProperty("id").GetString()!;

				// Map OpenAI function name to MCP method name
				if (!functionToMethodMap.TryGetValue(openAiName, out var mcpMethodName))
				{
					Console.WriteLine($"[ERROR] Unknown function: {openAiName}");
					continue;
				}

				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.WriteLine($"Calling tool: {openAiName} (MCP method: {mcpMethodName}) with ID '{toolCallId}' and Arguments: {arguments}");
				Console.ResetColor();

				// Create JSON-RPC request for MCP
				var mcpRequest = JsonSerializer.Serialize(
					new
					{
						jsonrpc = "2.0",
						id = Guid.NewGuid().ToString(),
						method = mcpMethodName,
						@params = JsonSerializer.Deserialize<JsonElement>(arguments),
					}
				);

				// Send request to MCP server
				await mcpInput.WriteLineAsync(mcpRequest);
				await mcpInput.FlushAsync();

				// Get MCP response
				string mcpResult = await mcpOutput.ReadLineAsync() ?? "";
				string toolContent = mcpResult;

				// Parse MCP response to extract result
				try
				{
					using var mcpJson = JsonDocument.Parse(mcpResult);
					if (mcpJson.RootElement.TryGetProperty("result", out var resultProp))
					{
						toolContent = resultProp.GetRawText();
					}
					else if (mcpJson.RootElement.TryGetProperty("error", out var errorProp))
					{
						toolContent = errorProp.GetRawText();
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[MCP PARSE ERROR] {ex.Message}");
				}

				// Add tool result to chat history
				chatHistory.Add(new Dictionary<string, object>
				{
					["role"] = "tool",
					["tool_call_id"] = toolCallId,
					["name"] = openAiName,
					["content"] = toolContent
				});
			}

			// Get final response from OpenAI with tool results
			var followupRequestBody = new
			{
				model = llmModel,
				messages = chatHistory
			};

			var followupContent = new StringContent(
				JsonSerializer.Serialize(followupRequestBody),
				Encoding.UTF8,
				"application/json"
			);

			var followupResponse = await httpClient.PostAsync(
				chatCompletionsUri,
				followupContent
			);

			var followupString = await followupResponse.Content.ReadAsStringAsync();
			using var followupDoc = JsonDocument.Parse(followupString);
			var followupChoices = followupDoc.RootElement.GetProperty("choices");
			var followupMessage = followupChoices[0].GetProperty("message");
			var finalContent = followupMessage.GetProperty("content").GetString()!;

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write($"\nAssistant: ");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine($"{finalContent}");
			Console.ResetColor();

			chatHistory.Add(new() { ["role"] = "assistant", ["content"] = finalContent });
		}
		else
		{
			var contentText = message.GetProperty("content").GetString()!;

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"\nAssistant: {contentText}");
			Console.ResetColor();

			chatHistory.Add(new() { ["role"] = "assistant", ["content"] = contentText });
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error: {ex.Message}");
	}

	Console.WriteLine();
}

Console.WriteLine("Goodbye!");

// Cleanup
mcpProcess.Kill();