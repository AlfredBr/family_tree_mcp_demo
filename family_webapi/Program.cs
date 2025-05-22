using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet(
    "/people",
    () =>
    {
        var json = File.ReadAllText("people.json");
        var doc = JsonDocument.Parse(json);
        return Results.Json(doc.RootElement);
    }
);

app.Run();
