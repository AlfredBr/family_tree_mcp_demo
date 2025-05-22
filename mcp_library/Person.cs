using System.Collections.Generic;

namespace FamilyTreeApp;

public class Person
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Gender { get; set; }
    public required int YearOfBirth { get; set; }
    public required List<string> Parents { get; set; }
    public required List<string> Spouses { get; set; }
    public required List<string> Children { get; set; }
}