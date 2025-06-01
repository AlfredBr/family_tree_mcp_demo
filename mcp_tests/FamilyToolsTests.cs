using Microsoft.Extensions.Logging;

using Moq;

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
        // Create a mock or null logger for FamilyService
        var logger = new Mock<ILogger<FamilyService>>();
        _familyService = new FamilyService(logger.Object);
    }

    [TestMethod]
    public async Task T99_GetFamily_ShouldReturnListOfPeople()
    {
        // Arrange
        var expectedCount = 40; // Adjust this based on your test data

        // Act
        var result = await FamilyTools.GetFamily(_familyService);
        // Deserialize the result to a list of Person objects
        var people = JsonSerializer.Deserialize<List<Person>>(result);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedCount, people?.Count());
    }

    [TestMethod]
    public async Task T99_GetPerson_ShouldReturnPerson_WhenIdExists()
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
    public async Task T99_GetPerson_ShouldReturnNull_WhenIdDoesNotExist()
    {
        // Arrange
        var nonExistentId = "999"; // Adjust this based on your test data

        // Act
        var result = await FamilyTools.GetPerson(_familyService, nonExistentId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task T01_AddPerson_ShouldReturnCreatedPerson_WhenDataIsValid()
    {
        // Arrange
        var personId = "test_person";
        // Create a simple JSON string directly
        string personJson = "{\"id\":\"" + personId + "\",\"name\":\"Test Person\",\"gender\":\"Other\",\"yearOfBirth\":2000,\"parents\":[],\"spouses\":[],\"children\":[]}";

        // Act
        var result = await FamilyTools.AddPerson(_familyService, personJson);

        // Assert
        Assert.IsNotNull(result);
        var resultPerson = JsonSerializer.Deserialize<Person>(result);
        Assert.IsNotNull(resultPerson);
        Assert.AreEqual(personId, resultPerson?.Id);
        Assert.AreEqual("Test Person", resultPerson?.Name);
        Assert.AreEqual(2000, resultPerson?.YearOfBirth);
    }

    [TestMethod]
    public async Task T99_AddPerson_ShouldReturnError_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = await FamilyTools.AddPerson(_familyService, invalidJson);

        // Assert
        Assert.IsNotNull(result);
        // The result should contain an error message
        Assert.IsTrue(result.Contains("error"));
        var errorResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.IsTrue(errorResult.TryGetProperty("error", out _));
    }

    [TestMethod]
    public async Task T02_UpdatePerson_ShouldReturnUpdatedPerson_WhenDataIsValid()
    {
        // Arrange
        var personId = "test_person"; // Use an existing ID from your test data
        var updatedName = "Updated Name";

        // Create a simple JSON string directly
        string personJson = "{\"id\":\"" + personId + "\",\"name\":\"" + updatedName + "\",\"gender\":\"Male\",\"yearOfBirth\":1980,\"parents\":[],\"spouses\":[],\"children\":[]}";

        // Act
        var result = await FamilyTools.UpdatePerson(_familyService, personId, personJson);

        // Assert
        Assert.IsNotNull(result);
        var resultPerson = JsonSerializer.Deserialize<Person>(result);
        Assert.IsNotNull(resultPerson);
        Assert.AreEqual(personId, resultPerson?.Id);
        Assert.AreEqual(updatedName, resultPerson?.Name);
    }

    [TestMethod]
    public async Task T99_UpdatePerson_ShouldReturnError_WhenJsonIsInvalid()
    {
        // Arrange
        var personId = "p1"; // Use an existing ID from your test data
        var invalidJson = "{ invalid json }";

        // Act
        var result = await FamilyTools.UpdatePerson(_familyService, personId, invalidJson);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("error"));
        var errorResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.IsTrue(errorResult.TryGetProperty("error", out _));
    }

    [TestMethod]
    public async Task T03_DeletePerson_ShouldReturnSuccess_WhenIdExists()
    {
        // Arrange
        var personId = "test_person"; // Use an ID that can be deleted in your test data

        // Act
        var result = await FamilyTools.DeletePerson(_familyService, personId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("success"));
        var successResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.IsTrue(successResult.TryGetProperty("success", out var successValue));
        Assert.IsTrue(successValue.GetBoolean());
    }

    [TestMethod]
    public async Task T99_DeletePerson_ShouldReturnError_WhenIdDoesNotExist()
    {
        // Arrange
        var nonExistentId = "non_existent_id";

        // Act
        var result = await FamilyTools.DeletePerson(_familyService, nonExistentId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("error"));
        var errorResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.IsTrue(errorResult.TryGetProperty("error", out _));
    }
}
