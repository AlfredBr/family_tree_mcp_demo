using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace FamilyTreeApp;

[DebuggerDisplay("{Id}")]
public class Person
{
	[JsonPropertyName("id")] public required string Id { get; set; }
	[JsonPropertyName("name")] public required string Name { get; set; }
	[JsonPropertyName("gender")] public required string Gender { get; set; }
	[JsonPropertyName("placeOfBirth")] public required string PlaceOfBirth { get; set; }
	[JsonPropertyName("yearOfBirth")] public required int YearOfBirth { get; set; }
	[JsonPropertyName("parents")] public required List<string> Parents { get; set; }
	[JsonPropertyName("spouses")] public required List<string> Spouses { get; set; }
	[JsonPropertyName("children")] public required List<string> Children { get; set; }
}
