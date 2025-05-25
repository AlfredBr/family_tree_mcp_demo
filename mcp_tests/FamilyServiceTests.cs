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
    public async Task GetFamily_ShouldReturnListOfPeople()
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
    public async Task GetPerson_ShouldReturnPerson_WhenIdExists()
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
    public async Task GetPerson_ShouldReturnNull_WhenIdDoesNotExist()
    {
        // Arrange
        var nonExistentId = "999"; // Adjust this based on your test data

        // Act
        var result = await _familyService.GetPerson(nonExistentId);

        // Assert
        Assert.IsNull(result);
    }
}
