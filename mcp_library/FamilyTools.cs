using System;
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

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

    [McpServerTool, Description("Get a particular member of the family by id.")]
    public static async Task<string?> GetPerson(
        FamilyService familyService,
        [Description("The id of the person in the family")] string id
    )
    {
        var person = await familyService.GetPerson(id);
        return person is null ? null : JsonSerializer.Serialize(person);
    }
}
