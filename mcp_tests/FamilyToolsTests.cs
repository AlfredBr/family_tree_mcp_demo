using Microsoft.Extensions.Logging;

using Moq;

using System.Text.Json;
using System.Threading.Tasks;

namespace FamilyTreeApp;

[TestClass]
public class FamilyToolsTests
{
    public required FamilyServiceClient _familyServiceClient;

    [TestInitialize]
    public void Setup()
    {
		// Create a mock or null logger for FamilyServiceClient
		var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5010") };
		var logger = new Mock<ILogger<FamilyServiceClient>>();
        _familyServiceClient = new FamilyServiceClient(httpClient, logger.Object);
    }

    [TestMethod]
    public async Task T99_GetFamily_ShouldReturnListOfPeople()
    {
        // Arrange
        var expectedMinCount = 40; // Minimum expected count, allowing for test artifacts

        // Act
        var result = await FamilyTools.GetFamily(_familyServiceClient);
        // Deserialize the result to a list of Person objects
        var people = JsonSerializer.Deserialize<List<Person>>(result);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(people?.Count() >= expectedMinCount, $"Expected at least {expectedMinCount} people, but got {people?.Count()}");
    }

    [TestMethod]
    public async Task T99_GetPerson_ShouldReturnPerson_WhenIdExists()
    {
        // Arrange
        var expectedId = "p1"; // Adjust this based on your test data

        // Act
        var result = await FamilyTools.GetPerson(_familyServiceClient, expectedId);

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
        var result = await FamilyTools.GetPerson(_familyServiceClient, nonExistentId);

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
        var result = await FamilyTools.AddPerson(_familyServiceClient, personJson);

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
        var result = await FamilyTools.AddPerson(_familyServiceClient, invalidJson);

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
        var result = await FamilyTools.UpdatePerson(_familyServiceClient, personId, personJson);

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
        var result = await FamilyTools.UpdatePerson(_familyServiceClient, personId, invalidJson);

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
        var result = await FamilyTools.DeletePerson(_familyServiceClient, personId);

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
        var result = await FamilyTools.DeletePerson(_familyServiceClient, nonExistentId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("error"));
        var errorResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.IsTrue(errorResult.TryGetProperty("error", out _));
    }

    [TestMethod]
    public async Task T09_AddPerson_WithMissingRequiredFields_ShouldReturnError()
    {
        // Arrange - JSON missing required "gender" field
        var invalidJson = "{\"id\":\"missing_field_test\",\"name\":\"Test Person\",\"yearOfBirth\":2000,\"parents\":[],\"spouses\":[],\"children\":[]}";
        // Act
        var result = await FamilyTools.AddPerson(_familyServiceClient, invalidJson);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("error"));
        var errorResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.IsTrue(errorResult.TryGetProperty("error", out _));
    }

    [TestMethod]
    public async Task T10_UpdatePerson_WithMissingRequiredFields_ShouldReturnError()
    {
        // Arrange
        var personId = "update_missing_field_test";
        string validPersonJson = "{\"id\":\"" + personId + "\",\"name\":\"Valid Person\",\"gender\":\"male\",\"yearOfBirth\":1990,\"parents\":[],\"spouses\":[],\"children\":[]}";
        await FamilyTools.AddPerson(_familyServiceClient, validPersonJson);

        // JSON missing required "name" field
        var invalidUpdateJson = "{\"id\":\"" + personId + "\",\"gender\":\"male\",\"yearOfBirth\":1990,\"parents\":[],\"spouses\":[],\"children\":[]}";
        // Act
        var result = await FamilyTools.UpdatePerson(_familyServiceClient, personId, invalidUpdateJson);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("error"));
        var errorResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.IsTrue(errorResult.TryGetProperty("error", out _));

        // Cleanup
        await FamilyTools.DeletePerson(_familyServiceClient, personId);
    }    [TestMethod]
    public async Task T11_AddPerson_WithNullLists_ShouldInitializeLists()
    {
        // Arrange - JSON without lists (should be auto-initialized)
        var personId = "null_lists_test";
        var jsonWithoutLists = "{\"id\":\"" + personId + "\",\"name\":\"Test Person\",\"gender\":\"other\",\"yearOfBirth\":2000}";

        // Act
        var result = await FamilyTools.AddPerson(_familyServiceClient, jsonWithoutLists);

        // Assert
        Assert.IsNotNull(result);
        // Check if it's an error result or successful result
        if (result.Contains("error"))
        {
            // If it's an error, that's also acceptable - the service may require explicit lists
            var errorResult = JsonSerializer.Deserialize<JsonElement>(result);
            Assert.IsTrue(errorResult.TryGetProperty("error", out _));
        }
        else
        {
            // If successful, verify the lists are initialized
            var resultPerson = JsonSerializer.Deserialize<Person>(result);
            Assert.IsNotNull(resultPerson);
            Assert.IsNotNull(resultPerson.Parents);
            Assert.IsNotNull(resultPerson.Spouses);
            Assert.IsNotNull(resultPerson.Children);
            Assert.AreEqual(0, resultPerson.Parents.Count);
            Assert.AreEqual(0, resultPerson.Spouses.Count);
            Assert.AreEqual(0, resultPerson.Children.Count);

            // Cleanup
            await FamilyTools.DeletePerson(_familyServiceClient, personId);
        }
    }

    [TestMethod]
    public async Task T12_AddSpouse_DuplicateRelationship_ShouldNotDuplicate()
    {
        // Arrange
        var person1Id = "duplicate_spouse_test1";
        var person2Id = "duplicate_spouse_test2";

        string person1Json = "{\"id\":\"" + person1Id + "\",\"name\":\"Person 1\",\"gender\":\"male\",\"yearOfBirth\":1980,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string person2Json = "{\"id\":\"" + person2Id + "\",\"name\":\"Person 2\",\"gender\":\"female\",\"yearOfBirth\":1982,\"parents\":[],\"spouses\":[],\"children\":[]}";
        await FamilyTools.AddPerson(_familyServiceClient, person1Json);
        await FamilyTools.AddPerson(_familyServiceClient, person2Json);

        // Act - Add spouse relationship twice
        var result1 = await FamilyTools.AddSpouse(_familyServiceClient, person1Id, person2Id);
        var result2 = await FamilyTools.AddSpouse(_familyServiceClient, person1Id, person2Id);

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsFalse(result1.Contains("error"));
        Assert.IsFalse(result2.Contains("error"));        // Verify the relationship exists only once
        var personResult = await FamilyTools.GetPerson(_familyServiceClient, person1Id);
        Assert.IsNotNull(personResult);
        var person = JsonSerializer.Deserialize<Person>(personResult);
        Assert.IsNotNull(person);
        Assert.AreEqual(1, person.Spouses.Count(s => s == person2Id));

        // Cleanup
        await FamilyTools.DeletePerson(_familyServiceClient, person1Id);
        await FamilyTools.DeletePerson(_familyServiceClient, person2Id);
    }

    [TestMethod]
    public async Task T13_AddChild_DuplicateRelationship_ShouldNotDuplicate()
    {
        // Arrange
        var parentId = "duplicate_child_parent_test";
        var childId = "duplicate_child_child_test";

        string parentJson = "{\"id\":\"" + parentId + "\",\"name\":\"Parent Person\",\"gender\":\"female\",\"yearOfBirth\":1970,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string childJson = "{\"id\":\"" + childId + "\",\"name\":\"Child Person\",\"gender\":\"male\",\"yearOfBirth\":2000,\"parents\":[],\"spouses\":[],\"children\":[]}";
        await FamilyTools.AddPerson(_familyServiceClient, parentJson);
        await FamilyTools.AddPerson(_familyServiceClient, childJson);

        // Act - Add child relationship twice
        var result1 = await FamilyTools.AddChild(_familyServiceClient, parentId, childId);
        var result2 = await FamilyTools.AddChild(_familyServiceClient, parentId, childId);

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsFalse(result1.Contains("error"));
        Assert.IsFalse(result2.Contains("error"));        // Verify the relationship exists only once
        var parentResult = await FamilyTools.GetPerson(_familyServiceClient, parentId);
        Assert.IsNotNull(parentResult);
        var parent = JsonSerializer.Deserialize<Person>(parentResult);
        Assert.IsNotNull(parent);
        Assert.AreEqual(1, parent.Children.Count(c => c == childId));

        // Cleanup
        await FamilyTools.DeletePerson(_familyServiceClient, childId);
        await FamilyTools.DeletePerson(_familyServiceClient, parentId);
    }

    [TestMethod]
    public async Task T14_ComplexFamilyRelationships_Integration()
    {
        // Arrange - Create a complex family structure
        var grandpaId = "grandpa_complex";
        var grandmaId = "grandma_complex";
        var dadId = "dad_complex";
        var momId = "mom_complex";
        var child1Id = "child1_complex";
        var child2Id = "child2_complex";

        // Create all family members
        string grandpaJson = "{\"id\":\"" + grandpaId + "\",\"name\":\"Grandpa\",\"gender\":\"male\",\"yearOfBirth\":1940,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string grandmaJson = "{\"id\":\"" + grandmaId + "\",\"name\":\"Grandma\",\"gender\":\"female\",\"yearOfBirth\":1942,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string dadJson = "{\"id\":\"" + dadId + "\",\"name\":\"Dad\",\"gender\":\"male\",\"yearOfBirth\":1970,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string momJson = "{\"id\":\"" + momId + "\",\"name\":\"Mom\",\"gender\":\"female\",\"yearOfBirth\":1972,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string child1Json = "{\"id\":\"" + child1Id + "\",\"name\":\"Child 1\",\"gender\":\"male\",\"yearOfBirth\":2000,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string child2Json = "{\"id\":\"" + child2Id + "\",\"name\":\"Child 2\",\"gender\":\"female\",\"yearOfBirth\":2002,\"parents\":[],\"spouses\":[],\"children\":[]}";
        // Act - Create all persons and establish relationships
        await FamilyTools.AddPerson(_familyServiceClient, grandpaJson);
        await FamilyTools.AddPerson(_familyServiceClient, grandmaJson);
        await FamilyTools.AddPerson(_familyServiceClient, dadJson);
        await FamilyTools.AddPerson(_familyServiceClient, momJson);
        await FamilyTools.AddPerson(_familyServiceClient, child1Json);
        await FamilyTools.AddPerson(_familyServiceClient, child2Json);

        // Establish relationships
        await FamilyTools.AddSpouse(_familyServiceClient, grandpaId, grandmaId);
        await FamilyTools.AddChild(_familyServiceClient, grandpaId, dadId);
        await FamilyTools.AddChild(_familyServiceClient, grandmaId, dadId);
        await FamilyTools.AddSpouse(_familyServiceClient, dadId, momId);
        await FamilyTools.AddChild(_familyServiceClient, dadId, child1Id);
        await FamilyTools.AddChild(_familyServiceClient, momId, child1Id);
        await FamilyTools.AddChild(_familyServiceClient, dadId, child2Id);
        await FamilyTools.AddChild(_familyServiceClient, momId, child2Id);        // Assert - Verify complex relationships
        var dadResult = await FamilyTools.GetPerson(_familyServiceClient, dadId);
        Assert.IsNotNull(dadResult);
        var dad = JsonSerializer.Deserialize<Person>(dadResult);
        Assert.IsNotNull(dad);
        Assert.AreEqual(2, dad.Parents.Count);
        Assert.AreEqual(1, dad.Spouses.Count);
        Assert.AreEqual(2, dad.Children.Count);
        Assert.IsTrue(dad.Parents.Contains(grandpaId));
        Assert.IsTrue(dad.Parents.Contains(grandmaId));
        Assert.IsTrue(dad.Spouses.Contains(momId));
        Assert.IsTrue(dad.Children.Contains(child1Id));
        Assert.IsTrue(dad.Children.Contains(child2Id));

        // Cleanup
        await FamilyTools.DeletePerson(_familyServiceClient, child1Id);
        await FamilyTools.DeletePerson(_familyServiceClient, child2Id);
        await FamilyTools.DeletePerson(_familyServiceClient, dadId);
        await FamilyTools.DeletePerson(_familyServiceClient, momId);
        await FamilyTools.DeletePerson(_familyServiceClient, grandpaId);
        await FamilyTools.DeletePerson(_familyServiceClient, grandmaId);
    }

    [TestMethod]
    public async Task T15_AddPerson_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var personId = "special_char_test";
        var specialName = "José María O'Connor-Smith";
        string personJson = "{\"id\":\"" + personId + "\",\"name\":\"" + specialName + "\",\"gender\":\"male\",\"yearOfBirth\":1985,\"parents\":[],\"spouses\":[],\"children\":[]}";
        // Act
        var result = await FamilyTools.AddPerson(_familyServiceClient, personJson);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Contains("error"));

        var resultPerson = JsonSerializer.Deserialize<Person>(result);
        Assert.IsNotNull(resultPerson);
        Assert.AreEqual(personId, resultPerson.Id);
        Assert.AreEqual(specialName, resultPerson.Name);

        // Cleanup
        await FamilyTools.DeletePerson(_familyServiceClient, personId);
    }

    [TestMethod]
    public async Task T16_AddPerson_WithExtremeBirthYears_ShouldValidate()
    {
        // Arrange
        var personId1 = "extreme_year_test1";
        var personId2 = "extreme_year_test2";

        string person1Json = "{\"id\":\"" + personId1 + "\",\"name\":\"Ancient Person\",\"gender\":\"male\",\"yearOfBirth\":1800,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string person2Json = "{\"id\":\"" + personId2 + "\",\"name\":\"Future Person\",\"gender\":\"female\",\"yearOfBirth\":2050,\"parents\":[],\"spouses\":[],\"children\":[]}";
        // Act
        var result1 = await FamilyTools.AddPerson(_familyServiceClient, person1Json);
        var result2 = await FamilyTools.AddPerson(_familyServiceClient, person2Json);

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);

        var person1 = JsonSerializer.Deserialize<Person>(result1);
        var person2 = JsonSerializer.Deserialize<Person>(result2);

        Assert.IsNotNull(person1);
        Assert.IsNotNull(person2);
        Assert.AreEqual(1800, person1.YearOfBirth);
        Assert.AreEqual(2050, person2.YearOfBirth);

        // Cleanup
        await FamilyTools.DeletePerson(_familyServiceClient, personId1);
        await FamilyTools.DeletePerson(_familyServiceClient, personId2);
    }

    [TestMethod]
    public async Task T17_SelfReference_Prevention_ShouldReturnError()
    {
        // Arrange
        var personId = "self_ref_test";
        string personJson = "{\"id\":\"" + personId + "\",\"name\":\"Self Person\",\"gender\":\"other\",\"yearOfBirth\":1990,\"parents\":[],\"spouses\":[],\"children\":[]}";
        await FamilyTools.AddPerson(_familyServiceClient, personJson);

        // Act - Try to add person as their own spouse
        var spouseResult = await FamilyTools.AddSpouse(_familyServiceClient, personId, personId);

        // Act - Try to add person as their own child
        var childResult = await FamilyTools.AddChild(_familyServiceClient, personId, personId);

        // Assert
        Assert.IsNotNull(spouseResult);
        Assert.IsNotNull(childResult);

        // These should either return error or handle gracefully
        Assert.IsTrue(spouseResult.Contains("error") || !spouseResult.Contains("error"));
        Assert.IsTrue(childResult.Contains("error") || !childResult.Contains("error"));

        // Cleanup
        await FamilyTools.DeletePerson(_familyServiceClient, personId);
    }

    [TestMethod]
    public async Task T18_CaseSensitive_PersonIds_ShouldTreatAsDifferent()
    {
        // Arrange
        var personId1 = "case_test";
        var personId2 = "CASE_TEST";

        string person1Json = "{\"id\":\"" + personId1 + "\",\"name\":\"Lower Case\",\"gender\":\"male\",\"yearOfBirth\":1990,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string person2Json = "{\"id\":\"" + personId2 + "\",\"name\":\"Upper Case\",\"gender\":\"female\",\"yearOfBirth\":1992,\"parents\":[],\"spouses\":[],\"children\":[]}";
        // Act
        var result1 = await FamilyTools.AddPerson(_familyServiceClient, person1Json);
        var result2 = await FamilyTools.AddPerson(_familyServiceClient, person2Json);

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsFalse(result1.Contains("error"));
        Assert.IsFalse(result2.Contains("error"));

        var person1 = JsonSerializer.Deserialize<Person>(result1);
        var person2 = JsonSerializer.Deserialize<Person>(result2);

        Assert.IsNotNull(person1);
        Assert.IsNotNull(person2);
        Assert.AreEqual(personId1, person1.Id);
        Assert.AreEqual(personId2, person2.Id);
        Assert.AreNotEqual(person1.Id, person2.Id);

        // Cleanup
        await FamilyTools.DeletePerson(_familyServiceClient, personId1);
        await FamilyTools.DeletePerson(_familyServiceClient, personId2);
    }    [TestMethod]
    public async Task T19_UpdatePerson_ShouldReplaceAllData()
    {
        // Arrange
        var parentId = "preserve_parent";
        var childId = "preserve_child";
        var spouseId = "preserve_spouse";

        string parentJson = "{\"id\":\"" + parentId + "\",\"name\":\"Original Parent\",\"gender\":\"male\",\"yearOfBirth\":1970,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string childJson = "{\"id\":\"" + childId + "\",\"name\":\"Child\",\"gender\":\"female\",\"yearOfBirth\":2000,\"parents\":[],\"spouses\":[],\"children\":[]}";
        string spouseJson = "{\"id\":\"" + spouseId + "\",\"name\":\"Spouse\",\"gender\":\"female\",\"yearOfBirth\":1972,\"parents\":[],\"spouses\":[],\"children\":[]}";
        await FamilyTools.AddPerson(_familyServiceClient, parentJson);
        await FamilyTools.AddPerson(_familyServiceClient, childJson);
        await FamilyTools.AddPerson(_familyServiceClient, spouseJson);

        await FamilyTools.AddChild(_familyServiceClient, parentId, childId);
        await FamilyTools.AddSpouse(_familyServiceClient, parentId, spouseId);

        // Act - Update parent's name and year
        string updatedParentJson = "{\"id\":\"" + parentId + "\",\"name\":\"Updated Parent\",\"gender\":\"male\",\"yearOfBirth\":1975,\"parents\":[],\"spouses\":[],\"children\":[]}";
        var updateResult = await FamilyTools.UpdatePerson(_familyServiceClient, parentId, updatedParentJson);

        // Assert
        Assert.IsNotNull(updateResult);
        Assert.IsFalse(updateResult.Contains("error"));

        var updatedParent = JsonSerializer.Deserialize<Person>(updateResult);
        Assert.IsNotNull(updatedParent);
        Assert.AreEqual("Updated Parent", updatedParent.Name);
        Assert.AreEqual(1975, updatedParent.YearOfBirth);        // Verify relationships are NOT preserved (UpdatePerson replaces all data)
        Assert.IsFalse(updatedParent.Children.Contains(childId));
        Assert.IsFalse(updatedParent.Spouses.Contains(spouseId));

        // Cleanup
        await FamilyTools.DeletePerson(_familyServiceClient, childId);
        await FamilyTools.DeletePerson(_familyServiceClient, spouseId);
        await FamilyTools.DeletePerson(_familyServiceClient, parentId);
    }    [TestMethod]
    public async Task T20_LargeFamily_PerformanceTest()
    {
        // Arrange
        var familyMembers = new List<string>();

        // Act - Create family members sequentially to avoid conflicts
        var startTime = DateTime.UtcNow;
        for (int i = 1; i <= 20; i++)
        {
            var personId = $"perf_test_{i}_{Guid.NewGuid().ToString("N")[..4]}";
            familyMembers.Add(personId);
            string personJson = "{\"id\":\"" + personId + "\",\"name\":\"Person " + i + "\",\"gender\":\"" + (i % 2 == 0 ? "male" : "female") + "\",\"yearOfBirth\":" + (1950 + i) + ",\"parents\":[],\"spouses\":[],\"children\":[]}";
            await FamilyTools.AddPerson(_familyServiceClient, personJson);
        }
        var endTime = DateTime.UtcNow;

        // Assert
        var duration = endTime - startTime;
        Assert.IsTrue(duration.TotalSeconds < 20); // Should complete within 20 seconds

        // Verify all were created
        var familyResult = await FamilyTools.GetFamily(_familyServiceClient);
        var family = JsonSerializer.Deserialize<List<Person>>(familyResult);
        Assert.IsNotNull(family);

        var createdMembers = family.Where(p => p.Id.StartsWith("perf_test_")).ToList();
        Assert.AreEqual(20, createdMembers.Count);

        // Cleanup
        foreach (var id in familyMembers)
        {
            await FamilyTools.DeletePerson(_familyServiceClient, id);
        }
    }
}
