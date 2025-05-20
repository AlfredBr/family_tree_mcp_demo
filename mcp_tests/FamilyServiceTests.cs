using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FamilyTreeApp.Services;
using FamilyTreeApp.Models;

namespace mcp_tests;

[TestClass]
public class FamilyServiceTests
{
    private FamilyService _familyService;

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
