#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace FamilyTreeApp;

// Family Tools Plugin class to wrap the static FamilyTools methods
public class FamilyToolsPlugin
{
    private readonly FamilyService _familyService;

    public FamilyToolsPlugin(FamilyService familyService)
    {
        _familyService = familyService;
    }

    [KernelFunction, Description("Get a list of all of the people in a family.")]
    public async Task<string> GetFamily()
    {
        return await FamilyTools.GetFamily(_familyService);
    }

    [KernelFunction, Description("Get a particular member of the family by id.")]
    public async Task<string?> GetPerson(
        [Description("The id of the person in the family")] string id
    )
    {
        return await FamilyTools.GetPerson(_familyService, id);
    }

    [KernelFunction, Description("Add a new person to the family. This creates a new family member record.")]
    public async Task<string> AddPerson(
        [Description("The JSON representation of the person to add.")] string personJson
    )
    {
        return await FamilyTools.AddPerson(_familyService, personJson);
    }

    [KernelFunction, Description("Update an existing person in the family. This modifies the details of a family member.")]
    public async Task<string> UpdatePerson(
        [Description("The id of the person to update")] string id,
        [Description("The JSON representation of the updated person.")] string personJson
    )
    {
        return await FamilyTools.UpdatePerson(_familyService, id, personJson);
    }

    [KernelFunction, Description("Delete a person from the family. This removes a family member record completely.")]
    public async Task<string> DeletePerson(
        [Description("The id of the person to delete")] string id
    )
    {
        return await FamilyTools.DeletePerson(_familyService, id);
    }

    [KernelFunction, Description("Add a spouse relationship between two family members.")]
    public async Task<string> AddSpouse(
        [Description("The id of the primary person")] string id,
        [Description("The id of the person to add as a spouse")] string spouseId
    )
    {
        return await FamilyTools.AddSpouse(_familyService, id, spouseId);
    }

    [KernelFunction, Description("Add a child relationship to a parent family member.")]
    public async Task<string> AddChild(
        [Description("The id of the parent person")] string parentId,
        [Description("The id of the person to add as a child")] string childId
    )
    {
        return await FamilyTools.AddChild(_familyService, parentId, childId);
    }
}
