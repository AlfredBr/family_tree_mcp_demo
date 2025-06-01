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

app.MapGet("/family",
        async (FamilyService familyService) =>
        {
            var people = await familyService.GetFamily();
            return Results.Ok(people);
        }
    )
    .WithName("GetFamily")
    .WithOpenApi();

app.MapGet("/family/{id}",
        async (FamilyService familyService, string id) =>
        {
            var person = await familyService.GetPerson(id);
            return person is not null ? Results.Ok(person) : Results.NotFound();
        }
    )
    .WithName("GetPerson")
    .WithOpenApi();

app.MapPost("/family",
        async (FamilyService familyService, Person person) =>
        {
            try
            {
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
        async (FamilyService familyService, string id, Person person) =>
        {
            try
            {
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
        async (FamilyService familyService, string id) =>
        {
            try
            {
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

await app.RunAsync();
