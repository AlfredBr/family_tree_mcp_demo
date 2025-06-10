using mcp_client_web;

using Microsoft.Extensions.AI;

var llmModel = "o4-mini";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSimpleConsoleLogging();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Get OpenAI API key from environment variable
var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrEmpty(openAIApiKey))
{
	// ```pwsh
	// [System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "my_api_key_value_from_https://platform.openai.com/settings/organization/api-keys", "User")
	// ```
	throw new InvalidOperationException("OPENAI_API_KEY environment variable is required");
}

builder.Services.AddHttpClient<McpSseClient>(client =>
{
	// This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
	// Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
	client.BaseAddress = new("https+http://mcp-sse-server");
});

// Register an IChatClient using Microsoft.Extensions.AI.OpenAI
builder.Services.AddSingleton<IChatClient>(provider =>
	new ChatClientBuilder(new OpenAI.Chat.ChatClient(llmModel, openAIApiKey).AsIChatClient())
		.UseFunctionInvocation()
		.Build());

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
	options.SwaggerEndpoint("/openapi/v1.json", "SwaggerUI");
	options.EnableTryItOutByDefault();
});
app.UseHttpsRedirection();

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
