using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Console;
using FamilyTreeApp;
using System.Collections.Concurrent;

#pragma warning disable S125

var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "HH:mm:ss ";
    options.ColorBehavior = LoggerColorBehavior.Enabled;
    options.SingleLine = true;
});

var app = builder.Build();

// Get logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// File path for people.json
const string PeopleFile = "people.json";
// Mutex for concurrency control
var fileLock = new object();

// Helper: Read all people from file
List<Person> ReadPeople()
{
    lock (fileLock)
    {
        try
        {
            if (!File.Exists(PeopleFile))
            {
                throw new FileNotFoundException($"File {PeopleFile} does not exist.");

			}
			var json = File.ReadAllText(PeopleFile);
            var people = JsonSerializer.Deserialize<List<Person>>(json) ?? new List<Person>();
            logger.LogInformation("Read {Count} people from {File}", people.Count, PeopleFile);
            return people;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read {File}", PeopleFile);
            return new List<Person>();
        }
    }
}

// Helper: Write all people to file
void WritePeople(List<Person> people)
{
    lock (fileLock)
    {
        var json = JsonSerializer.Serialize(people, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(PeopleFile, json);
        logger.LogInformation("Wrote {Count} people to {File}", people.Count, PeopleFile);
    }
}

// Helper: Update relationships for add/update
void UpdateRelationships(List<Person> people, Person person, Person? oldPerson = null)
{
    // Remove old relationships if updating
    if (oldPerson != null)
    {
        // Remove this person from old parents' children
        foreach (var parentId in oldPerson.Parents)
        {
            var parent = people.FirstOrDefault(p => p.Id == parentId);
            if (parent != null && parent.Children.Contains(oldPerson.Id))
            {
                parent.Children.Remove(oldPerson.Id);
                logger.LogInformation("Removed child {ChildId} from parent {ParentId}", oldPerson.Id, parentId);
            }
        }
        // Remove this person from old spouses' spouses
        foreach (var spouseId in oldPerson.Spouses)
        {
            var spouse = people.FirstOrDefault(p => p.Id == spouseId);
            if (spouse != null && spouse.Spouses.Contains(oldPerson.Id))
            {
                spouse.Spouses.Remove(oldPerson.Id);
                logger.LogInformation("Removed spouse {SpouseId} from spouse {PersonId}", oldPerson.Id, spouseId);
            }
        }
        // Remove this person from old children's parents
        foreach (var childId in oldPerson.Children)
        {
            var child = people.FirstOrDefault(p => p.Id == childId);
            if (child != null && child.Parents.Contains(oldPerson.Id))
            {
                child.Parents.Remove(oldPerson.Id);
                logger.LogInformation("Removed parent {ParentId} from child {ChildId}", oldPerson.Id, childId);
            }
        }
    }
    // Add new relationships
    foreach (var parentId in person.Parents)
    {
        var parent = people.FirstOrDefault(p => p.Id == parentId);
        if (parent != null && !parent.Children.Contains(person.Id))
        {
            parent.Children.Add(person.Id);
            logger.LogInformation("Added child {ChildId} to parent {ParentId}", person.Id, parentId);
        }
    }
    foreach (var spouseId in person.Spouses)
    {
        var spouse = people.FirstOrDefault(p => p.Id == spouseId);
        if (spouse != null && !spouse.Spouses.Contains(person.Id))
        {
            spouse.Spouses.Add(person.Id);
            logger.LogInformation("Added spouse {SpouseId} to spouse {PersonId}", person.Id, spouseId);
        }
    }
    foreach (var childId in person.Children)
    {
        var child = people.FirstOrDefault(p => p.Id == childId);
        if (child != null && !child.Parents.Contains(person.Id))
        {
            child.Parents.Add(person.Id);
            logger.LogInformation("Added parent {ParentId} to child {ChildId}", person.Id, childId);
        }
    }
}

// GET /people
app.MapGet("/people", () =>
{
    logger.LogInformation("GET /people endpoint accessed at {Time}", DateTime.Now);
    var people = ReadPeople();
    return Results.Json(people);
});

// GET /person/{id}
app.MapGet("/person/{id}", (string id) =>
{
    logger.LogInformation("GET /person/{Id} endpoint accessed at {Time}", id, DateTime.Now);
    var people = ReadPeople();
    var person = people.FirstOrDefault(p => p.Id == id);
    if (person == null)
    {
        logger.LogWarning("Person with id {Id} not found", id);
        return Results.NotFound();
    }
    logger.LogInformation("Found Person with id {Id} -> {Name}", id, person.Name);
    return Results.Json(person);
});

// POST /person
app.MapPost("/person", (Person person) =>
{
    logger.LogInformation("POST /person endpoint accessed at {Time}", DateTime.Now);
    var people = ReadPeople();
    if (people.Any(p => p.Id == person.Id))
    {
        logger.LogWarning("POST rejected: Person with id {Id} already exists", person.Id);
        return Results.Conflict($"Person with id {person.Id} already exists.");
    }
    // Validate referenced IDs
    foreach (var pid in person.Parents.Concat(person.Spouses).Concat(person.Children))
    {
        if (!string.IsNullOrWhiteSpace(pid) && !people.Any(p => p.Id == pid))
        {
            logger.LogWarning("POST rejected: Referenced id {RefId} does not exist", pid);
            return Results.BadRequest($"Referenced id {pid} does not exist.");
        }
    }
    people.Add(person);
    UpdateRelationships(people, person);
    WritePeople(people);
    logger.LogInformation("Added person {Id}", person.Id);
    return Results.Created($"/person/{person.Id}", person);
});

// PUT /person/{id}
app.MapPut("/person/{id}", (string id, Person person) =>
{
    logger.LogInformation("PUT /person/{Id} endpoint accessed at {Time}", id, DateTime.Now);
    var people = ReadPeople();
    var existing = people.FirstOrDefault(p => p.Id == id);
    if (existing == null)
    {
        logger.LogWarning("PUT rejected: Person with id {Id} not found", id);
        return Results.NotFound();
    }
    // Validate referenced IDs
    foreach (var pid in person.Parents.Concat(person.Spouses).Concat(person.Children))
    {
        if (!string.IsNullOrWhiteSpace(pid) && !people.Any(p => p.Id == pid))
        {
            logger.LogWarning("PUT rejected: Referenced id {RefId} does not exist", pid);
            return Results.BadRequest($"Referenced id {pid} does not exist.");
        }
    }
    // Remove old relationships, update with new
    UpdateRelationships(people, person, existing);
    // Replace the person
    var idx = people.FindIndex(p => p.Id == id);
    people[idx] = person;
    WritePeople(people);
    logger.LogInformation("Updated person {Id}", id);
    return Results.Ok(person);
});

await app.RunAsync();
