using FamilyTreeApp;

using Microsoft.Extensions.Logging.Console;

using Throw;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSimpleConsoleLogging();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<FamilyServiceClient>(client =>
{
	// This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
	// Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.

    var isAspireHosted = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("services__raw-webapi__https__0"));
    var baseAddress = isAspireHosted
        ? "https+http://raw-webapi"
        : builder.Configuration["FamilyApi:BaseAddress"];
	client.BaseAddress = new(baseAddress.ThrowIfNull().IfEmpty().Value);
});

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

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting MCP Web API...");

app.MapGet("/family",
        async (FamilyServiceClient familyService) =>
        {
            logger.LogInformation("Fetching family data from web service...");
			var people = await familyService.GetFamily();
            return Results.Ok(people);
        }
    )
    .WithName("GetFamily")
    .WithOpenApi();

app.MapGet("/family/{id}",
        async (FamilyServiceClient familyService, string id) =>
        {
            logger.LogInformation("Fetching person with ID {Id} from web service...", id);
			var person = await familyService.GetPerson(id);
            return person is not null ? Results.Ok(person) : Results.NotFound();
        }
    )
    .WithName("GetPerson")
    .WithOpenApi();

app.MapPost("/family",
        async (FamilyServiceClient familyService, Person person) =>
        {
            try
            {
                logger.LogInformation("Adding new person with ID {Id} to web service...", person.Id);
				var result = await familyService.AddPerson(person);
                return Results.Created($"/family/{result.Id}", result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode != null)
            {
                return ex.StatusCode switch
                {
                    System.Net.HttpStatusCode.Conflict => Results.Conflict($"Person with ID {person.Id} already exists."),
                    System.Net.HttpStatusCode.BadRequest => Results.BadRequest("Invalid person data."),
                    _ => Results.StatusCode((int)ex.StatusCode)
                };
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }
    )
    .WithName("AddPerson")
    .WithOpenApi();

app.MapPut("/family/{id}",
        async (FamilyServiceClient familyService, string id, Person person) =>
        {
            try
            {
                logger.LogInformation("Updating person with ID {Id} in web service...", id);
				var result = await familyService.UpdatePerson(id, person);
                return Results.Ok(result);
            }
            catch (HttpRequestException ex) when (ex.StatusCode != null)
            {
                return ex.StatusCode switch
                {
                    System.Net.HttpStatusCode.NotFound => Results.NotFound($"Person with ID {id} not found."),
                    System.Net.HttpStatusCode.BadRequest => Results.BadRequest("Invalid person data."),
                    _ => Results.StatusCode((int)ex.StatusCode)
                };
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }
    )
    .WithName("UpdatePerson")
    .WithOpenApi();

app.MapDelete("/family/{id}",
        async (FamilyServiceClient familyService, string id) =>
        {
            try
            {
                logger.LogInformation("Attempting to delete person with ID {Id} from web service...", id);
                await familyService.DeletePerson(id);
                return Results.NoContent();
            }
            catch (HttpRequestException ex) when (ex.StatusCode != null)
            {
                return ex.StatusCode switch
                {
                    System.Net.HttpStatusCode.NotFound => Results.NotFound($"Person with ID {id} not found."),
                    System.Net.HttpStatusCode.BadRequest => Results.BadRequest($"Cannot delete person with ID {id}."),
                    _ => Results.StatusCode((int)ex.StatusCode)
                };
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }
    )
    .WithName("DeletePerson")
    .WithOpenApi();

app.MapDefaultEndpoints();

await app.RunAsync();
