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
        var result = await _familyService.GetFamily();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedCount, result.Count);
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
}
