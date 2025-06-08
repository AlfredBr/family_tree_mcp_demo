using Microsoft.Extensions.Logging;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace FamilyTreeApp;

#pragma warning disable

public class FamilyServiceClient(HttpClient httpClient, ILogger<FamilyServiceClient> logger)
{
    public async Task LogAsync(string message)
    {
        logger.LogInformation(message);
        await Task.CompletedTask; // Simulate async logging
	}

	public async Task<List<Person>> GetFamily()
    {
        logger.LogInformation("Fetching family data from web service...");

        // Fetch people.json from the web service
        var response = await httpClient.GetAsync("/people");
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
        logger.LogInformation("Fetching person with ID {Id} from web service...", id);

        var people = await GetFamily();
        return people.FirstOrDefault(m =>
            m.Id?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);
    }

    public async Task<Person> AddPerson(Person person)
    {
        logger.LogInformation("Adding new person with ID {Id} to web service...", person.Id);

        var content = new StringContent(
            JsonSerializer.Serialize(person),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await httpClient.PostAsync("/person", content);
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
        logger.LogInformation("Updating person with ID {Id} in web service...", id);

        var content = new StringContent(
            JsonSerializer.Serialize(person),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await httpClient.PutAsync($"/person/{id}", content);
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
        logger.LogInformation("Deleting person with ID {Id} from web service...", id);

        var response = await httpClient.DeleteAsync($"/person/{id}");
        response.EnsureSuccessStatusCode();
    }
}

[JsonSerializable(typeof(List<Person>))]
internal sealed partial class PersonContext : JsonSerializerContext { }
