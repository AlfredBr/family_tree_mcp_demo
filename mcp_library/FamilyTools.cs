using ModelContextProtocol.Server;

using System.ComponentModel;
using System.Text.Json;

namespace FamilyTreeApp;

[McpServerToolType]
public static class FamilyTools
{
    [McpServerTool, Description("Get a list of all of the people in a family.")]
    public static async Task<string> GetFamily(FamilyService familyService)
    {
        var family = await familyService.GetFamily();
        return JsonSerializer.Serialize(family);
    }

    [McpServerTool, Description("Get a particular member of the family by id.  Use this tool to retrieve specific family member information.")]
    public static async Task<string?> GetPerson(FamilyService familyService,
        [Description("The id of the person in the family")] string id)
    {
        var person = await familyService.GetPerson(id);
        return person is null ? null : JsonSerializer.Serialize(person);
    }

    [McpServerTool, Description("Add a new person to the family. This creates a new family member record.")]
    public static async Task<string> AddPerson(FamilyService familyService,
        [Description("The JSON representation of the person to add.")] string personJson)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var person = JsonSerializer.Deserialize<Person>(personJson, options);

            if (person == null)
            {
                throw new ArgumentException("Invalid person data");
            }

            // Ensure lists are initialized
            person.Parents ??= new List<string>();
            person.Spouses ??= new List<string>();
            person.Children ??= new List<string>();

            var result = await familyService.AddPerson(person);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Update an existing person in the family. This modifies the details of a family member.")]
    public static async Task<string> UpdatePerson(FamilyService familyService,
        [Description("The id of the person to update")] string id,
        [Description("The JSON representation of the updated person.")] string personJson)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var person = JsonSerializer.Deserialize<Person>(personJson, options);

            if (person == null)
            {
                throw new ArgumentException("Invalid person data");
            }

            // Ensure lists are initialized
            person.Parents ??= new List<string>();
            person.Spouses ??= new List<string>();
            person.Children ??= new List<string>();

            var result = await familyService.UpdatePerson(id, person);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Delete a person from the family. This removes a family member record completely.")]
    public static async Task<string> DeletePerson(FamilyService familyService,
        [Description("The id of the person to delete")] string id)
    {
        try
        {
            await familyService.DeletePerson(id);
            return JsonSerializer.Serialize(new { success = true, message = $"Person with id {id} was deleted successfully" });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
