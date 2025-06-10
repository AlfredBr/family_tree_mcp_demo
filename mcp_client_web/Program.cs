using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Console;

using OpenAI;

var llmModel = "o4-mini";

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
	options.TimestampFormat = "HH:mm:ss ";
	options.ColorBehavior = LoggerColorBehavior.Enabled;
	options.SingleLine = true;
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// Get OpenAI API key from environment variable
var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrEmpty(openAIApiKey))
{
	// ```pwsh
	// [System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "my_api_key_value_from_https://platform.openai.com/settings/organization/api-keys", "User")
	// ```
	throw new InvalidOperationException("OPENAI_API_KEY environment variable is required");
}

// Register an IChatClient using Microsoft.Extensions.AI.OpenAI
builder.Services.AddSingleton<IChatClient>(provider =>
	new ChatClientBuilder(new OpenAI.Chat.ChatClient(llmModel, openAIApiKey).AsIChatClient())
		.UseFunctionInvocation()
		.Build());

// Add Swagger
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new() { Title = "MCP Client Web API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/hello", () =>
{
    return "Hello World!";
})
.WithName("SayHello");

app.MapDefaultEndpoints();

await app.RunAsync();
