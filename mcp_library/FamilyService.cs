using Microsoft.Extensions.Logging;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace FamilyTreeApp;

#pragma warning disable

public class FamilyService
{
    private readonly ILogger<FamilyService> _logger;

    public FamilyService(ILogger<FamilyService> logger)
    {
        _logger = logger;
    }

    public async Task<List<Person>> GetFamily()
    {
        _logger.LogInformation("Fetching family data from web service...");

        // Fetch people.json from the web service
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("http://localhost:5010/people");
        response.EnsureSuccessStatusCode();
        var peopleJson = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        var people = JsonSerializer.Deserialize<List<Person>>(peopleJson, options) ?? new List<Person>();

        if (people.Count == 0)
        {
            throw new Exception("No people found in the JSON file.");
        }
        return people;
    }

    public async Task<Person?> GetPerson(string id)
    {
        _logger.LogInformation("Fetching person with ID {Id} from web service...", id);

        var people = await GetFamily();
        return people.FirstOrDefault(m =>
            m.Id?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);
    }
    
    public async Task<Person> AddPerson(Person person)
    {
        _logger.LogInformation("Adding new person with ID {Id} to web service...", person.Id);
        
        using var httpClient = new HttpClient();
        var content = new StringContent(
            JsonSerializer.Serialize(person),
            System.Text.Encoding.UTF8,
            "application/json");
            
        var response = await httpClient.PostAsync("http://localhost:5010/person", content);
        response.EnsureSuccessStatusCode();
        
        var resultJson = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
        
        var result = JsonSerializer.Deserialize<Person>(resultJson, options);
        if (result == null)
        {
            throw new Exception("Failed to deserialize the created person.");
        }
        
        return result;
    }
    
    public async Task<Person> UpdatePerson(string id, Person person)
    {
        _logger.LogInformation("Updating person with ID {Id} in web service...", id);
        
        using var httpClient = new HttpClient();
        var content = new StringContent(
            JsonSerializer.Serialize(person),
            System.Text.Encoding.UTF8,
            "application/json");
            
        var response = await httpClient.PutAsync($"http://localhost:5010/person/{id}", content);
        response.EnsureSuccessStatusCode();
        
        var resultJson = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
        
        var result = JsonSerializer.Deserialize<Person>(resultJson, options);
        if (result == null)
        {
            throw new Exception("Failed to deserialize the updated person.");
        }
        
        return result;
    }
    
    public async Task DeletePerson(string id)
    {
        _logger.LogInformation("Deleting person with ID {Id} from web service...", id);
        
        using var httpClient = new HttpClient();
        var response = await httpClient.DeleteAsync($"http://localhost:5010/person/{id}");
        response.EnsureSuccessStatusCode();
    }
}

[JsonSerializable(typeof(List<Person>))]
internal sealed partial class PersonContext : JsonSerializerContext { }
