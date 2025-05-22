using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace FamilyTreeApp;

public class FamilyService
{
    private List<Person> _people = new();

    public async Task<List<Person>> GetFamily()
    {
        if (_people?.Count > 0)
        {
            return _people;
        }

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
        _people = JsonSerializer.Deserialize<List<Person>>(peopleJson, options) ?? new List<Person>();

        if (_people.Count == 0)
        {
            throw new Exception("No people found in the JSON file.");
        }
        return _people;
    }

    public async Task<Person?> GetPerson(string id)
    {
        var people = await GetFamily();
        return people.FirstOrDefault(m =>
            m.Id?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);
    }
}

[JsonSerializable(typeof(List<Person>))]
internal sealed partial class PersonContext : JsonSerializerContext { }
