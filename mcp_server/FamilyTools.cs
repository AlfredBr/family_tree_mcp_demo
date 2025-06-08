using ModelContextProtocol.Server;

using System.ComponentModel;
using System.Text.Json;

namespace FamilyTreeApp;

[McpServerToolType]
public static class FamilyTools
{
    [McpServerTool, Description("Get a list of all of the people in a family.")]
    public static async Task<string> GetFamily(FamilyServiceClient familyServiceClient)
    {
        var family = await familyServiceClient.GetFamily();
        return JsonSerializer.Serialize(family);
    }

    [McpServerTool, Description("Get a particular member of the family by id.  Use this tool to retrieve specific family member information.")]
    public static async Task<string?> GetPerson(FamilyServiceClient familyServiceClient,
        [Description("The id of the person in the family")] string id)
    {
        var person = await familyServiceClient.GetPerson(id);
        return person is null ? null : JsonSerializer.Serialize(person);
    }

    [McpServerTool, Description("Add a new person to the family. This creates a new family member record.")]
    public static async Task<string> AddPerson(FamilyServiceClient familyServiceClient,
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

            var result = await familyServiceClient.AddPerson(person);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Update an existing person in the family. This modifies the details of a family member.")]
    public static async Task<string> UpdatePerson(FamilyServiceClient familyServiceClient,
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

            var result = await familyServiceClient.UpdatePerson(id, person);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Delete a person from the family. This removes a family member record completely.")]
    public static async Task<string> DeletePerson(FamilyServiceClient familyServiceClient,
        [Description("The id of the person to delete")] string id)
    {
        try
        {
            await familyServiceClient.DeletePerson(id);
            return JsonSerializer.Serialize(new { success = true, message = $"Person with id {id} was deleted successfully" });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Add a spouse relationship between two family members.")]
    public static async Task<string> AddSpouse(FamilyServiceClient familyServiceClient,
        [Description("The id of the primary person")] string id,
        [Description("The id of the person to add as a spouse")] string spouseId)
    {
        try
        {
            // Retrieve both persons
            var person = await familyServiceClient.GetPerson(id);
            if (person is null)
            {
                throw new ArgumentException($"Person with id {id} not found.");
            }

            var spouse = await familyServiceClient.GetPerson(spouseId);
            if (spouse is null)
            {
                throw new ArgumentException($"Person with id {spouseId} not found.");
            }

            // Update spouse relationships if not already set
            if (!person.Spouses.Contains(spouseId))
            {
                person.Spouses.Add(spouseId);
            }

            if (!spouse.Spouses.Contains(id))
            {
                spouse.Spouses.Add(id);
            }

            // Update both persons via FamilyService
            var updatedPerson = await familyServiceClient.UpdatePerson(id, person);
            var updatedSpouse = await familyServiceClient.UpdatePerson(spouseId, spouse);

            return JsonSerializer.Serialize(new { person = updatedPerson, spouse = updatedSpouse });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool, Description("Add a child relationship to a parent family member.")]
    public static async Task<string> AddChild(FamilyServiceClient familyServiceClient,
        [Description("The id of the parent person")] string parentId,
        [Description("The id of the person to add as a child")] string childId)
    {
        try
        {
            // Retrieve both persons
            var parent = await familyServiceClient.GetPerson(parentId);
            if (parent is null)
            {
                throw new ArgumentException($"Parent with id {parentId} not found.");
            }

            var child = await familyServiceClient.GetPerson(childId);
            if (child is null)
            {
                throw new ArgumentException($"Child with id {childId} not found.");
            }

            // Update child relationships if not already set
            if (!parent.Children.Contains(childId))
            {
                parent.Children.Add(childId);
            }

            if (!child.Parents.Contains(parentId))
            {
                child.Parents.Add(parentId);
            }

            // Update both persons via FamilyService
            var updatedParent = await familyServiceClient.UpdatePerson(parentId, parent);
            var updatedChild = await familyServiceClient.UpdatePerson(childId, child);

            return JsonSerializer.Serialize(new { parent = updatedParent, child = updatedChild });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
