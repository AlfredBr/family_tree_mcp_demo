#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.ComponentModel;
using FamilyTreeApp;
using Microsoft.SemanticKernel;
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
}
