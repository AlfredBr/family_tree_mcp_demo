using System.Text.Json;
using FamilyTreeApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<FamilyService>();

var app = builder.Build();

// Enable Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.ConfigObject.AdditionalItems["tryItOutEnabled"] = true;
    });
}

app.UseHttpsRedirection();

app.MapGet(
        "/family",
        async (FamilyService familyService) =>
        {
            var people = await familyService.GetFamily();
            return Results.Ok(people);
        }
    )
    .WithName("GetFamily")
    .WithOpenApi();

app.MapGet(
        "/family/{id}",
        async (FamilyService familyService, string id) =>
        {
            var person = await familyService.GetPerson(id);
            return person is not null ? Results.Ok(person) : Results.NotFound();
        }
    )
    .WithName("GetPerson")
    .WithOpenApi();

app.Run();
