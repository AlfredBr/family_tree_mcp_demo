using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace FamilyTreeApp;

[TestClass]
public class FamilyServiceTests
{
    public required FamilyService _familyService;

    [TestInitialize]
    public void Setup()
    {
        // Initialize the FamilyService with a mock or test logger if needed
        // For simplicity, we are using a real instance here. In a real test, you might want to mock the logger.
        var logger = new Mock<ILogger<FamilyService>>();
        _familyService = new FamilyService(logger.Object);
    }

    [TestMethod]
    public async Task T01_GetFamily_ShouldReturnListOfPeople()
    {
        // Arrange
        var expectedCount = 40; // Adjust this based on your test data

        // Act
        var result = await _familyService.GetFamily();        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count >= expectedCount, $"Expected at least {expectedCount} people, but got {result.Count}");
    }

    [TestMethod]
    public async Task T02_GetPerson_ShouldReturnPerson_WhenIdExists()
    {
        // Arrange
        var expectedId = "p1"; // Adjust this based on your test data

        // Act
        var result = await _familyService.GetPerson(expectedId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedId, result?.Id);
    }

    [TestMethod]
    public async Task T03_GetPerson_ShouldReturnNull_WhenIdDoesNotExist()
    {
        // Arrange
        var nonExistentId = "999"; // Adjust this based on your test data

        // Act
        var result = await _familyService.GetPerson(nonExistentId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
	public async Task T04_AddPerson_ShouldAddAndReturnPerson()
	{
		var testId = "test_addperson";
		var person = new Person
		{
			Id = testId,
			Name = "Test Add Person",
			Gender = "male", // Required field
			YearOfBirth = 1990, // Required field
			Parents = new List<string>(), // Required field
			Spouses = new List<string>(), // Required field
			Children = new List<string>() // Required field
		};

		await _familyService.AddPerson(person);
		var fetched = await _familyService.GetPerson(testId);
		Assert.IsNotNull(fetched);
		Assert.AreEqual(testId, fetched?.Id);
        Assert.AreEqual("Test Add Person", fetched?.Name);
        Assert.AreEqual(1990, fetched?.YearOfBirth);
        Assert.AreEqual("male", fetched?.Gender, true);
	}

    [TestMethod]
    public async Task T05_UpdatePerson()
    {
        var testId = "test_addperson";
		var person = new Person
		{
			Id = testId,
			Name = "Test Add Person",
			Gender = "female", // Required field
			YearOfBirth = 1990, // Required field
			Parents = new List<string>(), // Required field
			Spouses = new List<string>(), // Required field
			Children = new List<string>() // Required field
		};
		await _familyService.UpdatePerson(testId, person);
        var fetched = await _familyService.GetPerson(testId);
        Assert.IsNotNull(fetched);
		Assert.AreEqual(testId, fetched?.Id);
		Assert.AreEqual("Test Add Person", fetched?.Name);
		Assert.AreEqual(1990, fetched?.YearOfBirth);
		Assert.AreEqual("female", fetched?.Gender, true);
	}

	[TestMethod]
    public async Task T06_DeletePerson_ShouldRemovePerson()
    {
        var testId = "test_addperson";
        await _familyService.DeletePerson(testId);
        var fetched = await _familyService.GetPerson(testId);
        Assert.IsNull(fetched);
    }

    [TestMethod]
    public async Task T07_AddPerson_ShouldThrowException_WhenPersonWithSameIdExists()
    {
        // Arrange
        var testId = "duplicate_test_person";
        var person1 = new Person
        {
            Id = testId,
            Name = "First Person",
            Gender = "male",
            YearOfBirth = 1990,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        var person2 = new Person
        {
            Id = testId, // Same ID
            Name = "Second Person",
            Gender = "female",
            YearOfBirth = 1995,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        // Act & Assert
        await _familyService.AddPerson(person1);
        await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
        {
            await _familyService.AddPerson(person2);
        });

        // Cleanup
        await _familyService.DeletePerson(testId);
    }

    [TestMethod]
    public async Task T08_UpdatePerson_ShouldThrowException_WhenPersonDoesNotExist()
    {
        // Arrange
        var nonExistentId = "non_existent_update_test";
        var person = new Person
        {
            Id = nonExistentId,
            Name = "Non Existent",
            Gender = "other",
            YearOfBirth = 2000,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
        {
            await _familyService.UpdatePerson(nonExistentId, person);
        });
    }

    [TestMethod]
    public async Task T09_DeletePerson_ShouldThrowException_WhenPersonDoesNotExist()
    {
        // Arrange
        var nonExistentId = "non_existent_delete_test";

        // Act & Assert
        await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
        {
            await _familyService.DeletePerson(nonExistentId);
        });
    }    [TestMethod]
    public async Task T10_AddPerson_WithComplexRelationships()
    {
        var testId = Guid.NewGuid().ToString("N")[..8];
        var parentId = $"test_parent_complex_{testId}";
        var childId = $"test_child_complex_{testId}";
        var spouseId = $"test_spouse_complex_{testId}";
          // Create parent
        var parent = new Person
        {
            Id = parentId,
            Name = "Test Parent",
            Gender = "male",
            YearOfBirth = 1970,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        // Create spouse
        var spouse = new Person
        {
            Id = spouseId,
            Name = "Test Spouse",
            Gender = "female",
            YearOfBirth = 1972,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        // Create child
        var child = new Person
        {
            Id = childId,
            Name = "Test Child",
            Gender = "other",
            YearOfBirth = 2000,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };        // Act - Add people first without relationships
        await _familyService.AddPerson(parent);
        await _familyService.AddPerson(spouse);
        await _familyService.AddPerson(child);

        // Then update them with relationships by updating their objects
        parent.Spouses.Add(spouseId);
        parent.Children.Add(childId);
        spouse.Spouses.Add(parentId);
        spouse.Children.Add(childId);
        child.Parents.Add(parentId);
        child.Parents.Add(spouseId);

        await _familyService.UpdatePerson(parentId, parent);
        await _familyService.UpdatePerson(spouseId, spouse);
        await _familyService.UpdatePerson(childId, child);// Assert - Check people were created and relationships established
        var fetchedParent = await _familyService.GetPerson(parentId);
        var fetchedSpouse = await _familyService.GetPerson(spouseId);
        var fetchedChild = await _familyService.GetPerson(childId);

        Assert.IsNotNull(fetchedParent);
        Assert.IsNotNull(fetchedSpouse);
        Assert.IsNotNull(fetchedChild);

        // Check relationships
        Assert.IsTrue(fetchedParent.Spouses.Contains(spouseId));
        Assert.IsTrue(fetchedParent.Children.Contains(childId));
        Assert.IsTrue(fetchedSpouse.Spouses.Contains(parentId));
        Assert.IsTrue(fetchedSpouse.Children.Contains(childId));
        Assert.IsTrue(fetchedChild.Parents.Contains(parentId));
        Assert.IsTrue(fetchedChild.Parents.Contains(spouseId));
        Assert.IsTrue(fetchedParent.Children.Contains(childId));
        Assert.IsTrue(fetchedSpouse.Spouses.Contains(parentId));
        Assert.IsTrue(fetchedChild.Parents.Contains(parentId));
        Assert.IsTrue(fetchedChild.Parents.Contains(spouseId));

        // Cleanup
        await _familyService.DeletePerson(childId);
        await _familyService.DeletePerson(spouseId);
        await _familyService.DeletePerson(parentId);
    }

    [TestMethod]
    public async Task T11_GetPerson_CaseInsensitiveId()
    {
        // Arrange
        var testId = "test_case_insensitive";
        var person = new Person
        {
            Id = testId,
            Name = "Case Test Person",
            Gender = "male",
            YearOfBirth = 1985,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        await _familyService.AddPerson(person);

        // Act - Test different case variations
        var result1 = await _familyService.GetPerson(testId.ToUpper());
        var result2 = await _familyService.GetPerson(testId.ToLower());
        var result3 = await _familyService.GetPerson("TEST_case_INSENSITIVE");

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsNotNull(result3);
        Assert.AreEqual(testId, result1?.Id);
        Assert.AreEqual(testId, result2?.Id);
        Assert.AreEqual(testId, result3?.Id);

        // Cleanup
        await _familyService.DeletePerson(testId);
    }

    [TestMethod]    public async Task T12_UpdatePerson_ChangeAllProperties()
    {
        // Arrange
        var testId = $"test_update_all_props_{Guid.NewGuid().ToString("N")[..8]}";
        var originalPerson = new Person
        {
            Id = testId,
            Name = "Original Name",
            Gender = "male",
            YearOfBirth = 1990,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        await _familyService.AddPerson(originalPerson);        var updatedPerson = new Person
        {
            Id = testId,
            Name = "Updated Name",
            Gender = "female",
            YearOfBirth = 1992,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        // Act
        var result = await _familyService.UpdatePerson(testId, updatedPerson);        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Updated Name", result.Name);
        Assert.AreEqual("female", result.Gender);
        Assert.AreEqual(1992, result.YearOfBirth);
        Assert.AreEqual(0, result.Parents.Count);
        Assert.AreEqual(0, result.Spouses.Count);
        Assert.AreEqual(0, result.Children.Count);

        // Cleanup
        await _familyService.DeletePerson(testId);
    }

    [TestMethod]
    public async Task T13_GetFamily_EmptyAndReload()
    {
        // This test checks that GetFamily works consistently
        // and returns the expected structure

        // Act
        var family1 = await _familyService.GetFamily();
        var family2 = await _familyService.GetFamily();

        // Assert
        Assert.IsNotNull(family1);
        Assert.IsNotNull(family2);
        Assert.AreEqual(family1.Count, family2.Count);

        // Verify that each person has required properties
        foreach (var person in family1)
        {
            Assert.IsNotNull(person.Id);
            Assert.IsNotNull(person.Name);
            Assert.IsNotNull(person.Gender);
            Assert.IsTrue(person.YearOfBirth > 0);
            Assert.IsNotNull(person.Parents);
            Assert.IsNotNull(person.Spouses);
            Assert.IsNotNull(person.Children);
        }
    }

    [TestMethod]
    public async Task T14_AddPerson_WithEmptyStringProperties()
    {
        // Arrange
        var testId = "test_empty_strings";
        var person = new Person
        {
            Id = testId,
            Name = "", // Empty name
            Gender = "other",
            YearOfBirth = 1995,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        // Act
        var result = await _familyService.AddPerson(person);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testId, result.Id);
        Assert.AreEqual("", result.Name);

        // Cleanup
        await _familyService.DeletePerson(testId);
    }    [TestMethod]
    public async Task T15_AddPerson_WithSpecialCharacters()
    {
        // Arrange
        var testId = $"test_special_chars_{Guid.NewGuid().ToString("N")[..8]}";
        var person = new Person
        {
            Id = testId,
            Name = "Test Name with Special Chars: àáâãäåæçèéêë",
            Gender = "non-binary",
            YearOfBirth = 1995,
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        // Act
        var result = await _familyService.AddPerson(person);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testId, result.Id);
        Assert.AreEqual("Test Name with Special Chars: àáâãäåæçèéêë", result.Name);

        // Cleanup
        await _familyService.DeletePerson(testId);
    }

    [TestMethod]
    public async Task T16_AddPerson_WithExtremeYearOfBirth()
    {
        // Arrange
        var testId = "test_extreme_year";
        var person = new Person
        {
            Id = testId,
            Name = "Ancient Person",
            Gender = "male",
            YearOfBirth = 1800, // Very old
            Parents = new List<string>(),
            Spouses = new List<string>(),
            Children = new List<string>()
        };

        // Act
        var result = await _familyService.AddPerson(person);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1800, result.YearOfBirth);

        // Cleanup
        await _familyService.DeletePerson(testId);
    }

    [TestMethod]
    public async Task T17_GetFamily_PerformanceTest()
    {
        // This is a simple performance test to ensure GetFamily responds in reasonable time
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var family = await _familyService.GetFamily();

        stopwatch.Stop();

        // Assert
        Assert.IsNotNull(family);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, $"GetFamily took too long: {stopwatch.ElapsedMilliseconds}ms");
        Assert.IsTrue(family.Count > 0, "Family should contain at least one person");
    }
}
