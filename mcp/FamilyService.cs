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
        // check if the people list is already populated
        if (_people?.Count > 0)
        {
            return _people;
        }

        // check if the file exists
        if (!File.Exists("people.json"))
        {
            throw new FileNotFoundException("people.json file not found.");
        }
        // read the people.json file from the filesystem
        var peopleJson = await File.ReadAllTextAsync("people.json");
        // deserialize the json into a list of Person objects
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
        // use the JsonSerializerContext to deserialize the json
        // into a list of Person objects
        _people =
            JsonSerializer.Deserialize<List<Person>>(peopleJson, options) ?? new List<Person>();
        // check if the list is empty
        if (_people.Count == 0)
        {
            throw new Exception("No people found in the JSON file.");
        }
        // check if the list is null
        if (_people == null)
        {
            throw new Exception("No people found in the JSON file.");
        }
        // check if the list is empty
        if (_people.Count == 0)
        {
            throw new Exception("No people found in the JSON file.");
        }
        // return the list of people
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
