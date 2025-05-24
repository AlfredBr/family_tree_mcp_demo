using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet(
	"/people",
	() =>
	{
		var json = File.ReadAllText("people.json");
		var doc = JsonDocument.Parse(json);
		var options = new JsonSerializerOptions { WriteIndented = true };
		var formattedJson = JsonSerializer.Serialize(doc.RootElement, options);
		return Results.Text(formattedJson, "application/json");
	}
);

await app.RunAsync();
