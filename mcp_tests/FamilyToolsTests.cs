using System.Text.Json;
using System.Threading.Tasks;

namespace FamilyTreeApp;

[TestClass]
public class FamilyToolsTests
{
    public required FamilyService _familyService;

    [TestInitialize]
    public void Setup()
    {
        _familyService = new FamilyService();
    }

    [TestMethod]
    public async Task GetFamily_ShouldReturnListOfPeople()
    {
        // Arrange
        var expectedCount = 19; // Adjust this based on your test data

        // Act
        var result = await FamilyTools.GetFamily(_familyService);
        // Deserialize the result to a list of Person objects
        var people = JsonSerializer.Deserialize<List<Person>>(result);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedCount, people?.Count());
    }

    [TestMethod]
    public async Task GetPerson_ShouldReturnPerson_WhenIdExists()
    {
        // Arrange
        var expectedId = "p1"; // Adjust this based on your test data

        // Act
        var result = await FamilyTools.GetPerson(_familyService, expectedId);

        // Assert
        Assert.IsNotNull(result);
        // Deserialize the result to a Person object
        var person = JsonSerializer.Deserialize<Person>(result);
        Assert.AreEqual(expectedId, person?.Id);
    }

    [TestMethod]
    public async Task GetPerson_ShouldReturnNull_WhenIdDoesNotExist()
    {
        // Arrange
        var nonExistentId = "999"; // Adjust this based on your test data

        // Act
        var result = await FamilyTools.GetPerson(_familyService, nonExistentId);

        // Assert
        Assert.IsNull(result);
    }

}
