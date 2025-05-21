# Family Tree MCP Service

This project provides a Model Context Protocol (MCP) server for exploring and querying a family tree. It is implemented in C# using .NET, and exposes tools for retrieving family members and their relationships.

## Features
- List all people in the family tree
- Query individual family members by ID
- Analyze relationships (parents, children, spouses)
- Easily extendable with new tools

## Project Structure
- `mcp/` — Main service code
  - `FamilyService.cs` — Loads and serves family data
  - `FamilyTools.cs` — MCP tools for querying the family tree
  - `Person.cs` — Data model for a person
  - `people.json` — Family data
- `mcp_tests/` — Unit tests for the service and tools
- `.vscode/mcp.json` — MCP server configuration

## Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

### Running the MCP Server

1. Clone the repository:
   ```sh
   git clone <your-repo-url>
   cd mcp
   ```
2. Run the server:
   ```sh
   dotnet run --project mcp/mcp.csproj
   ```

The server will start and listen for MCP requests as configured in `.vscode/mcp.json`.

### Testing

Run all tests with:
```sh
cd mcp_tests
 dotnet test
```

## Extending
- Add new tools to `FamilyTools.cs` using the `[McpServerTool]` attribute.
- Update `people.json` to add or modify family members.

## Sample Questions

Here are some example questions you can ask this MCP server:

- List all people in the family tree.
- Who are the parents of Elizabeth Carter?
- What are the names of all children of Margaret Carter?
- List all last names in the family tree.
- Who is the youngest male in the family?
- How old was Linda Carter when her child was born?
- Get the details of the person with ID `p5`.
- Who are the spouses of Susan Smith?
- List all people born after 1980.
- What is the relationship between Emily Smith and William Carter?

## License
MIT License

---

*This project is intended for demonstration and educational purposes.*
