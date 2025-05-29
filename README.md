# Family Tree MCP Demo

This project provides a Model Context Protocol (MCP) server for exploring and querying a family tree. It is implemented in C# using .NET, and exposes tools for retrieving family members and their relationships.

## Features
- List all people in the family tree
- Query individual family members by ID
- Analyze relationships (parents, children, spouses)
- Easily extendable with new tools

## Project Structure
- `family_tree_mcp_demo/`
   - `family_webapi/` -- a super simple webservice that returns JSON data
   - `mcp_client_meai/` -- a MCP client that uses Microsoft.Extensions.AI
   - `mcp_client_sk/` -- a MCP client that uses Semantic Kernel
   - `mcp_library/` -- the shared MCP code library
   - `mcp_tests/` -- Unit tests for the service and tools
   - `mcp_webapi/` -- not used in the MCP server, but useful for demos.
   - `mcp_server/` -- a console based (stdio) server to use with MCP clients like VSCode/Claude.

Create a `.vscode/mcp.json` MCP server configuration when you use VSCode as your client.  (Can be adapted to Claude as well.)

## Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

### Running the MCP Client

1. Clone the repository:
   ```sh
   git clone https://github.com/AlfredBr/family_tree_mcp_demo.git
   ```
2. Run the web server that provides the json data:
   ```sh
   dotnet run --project family_tree_mcp_demo/family_webapi/family_webapi.csproj
   ```

3. Run the __Semantic Kernel__ based client:
   ```sh
   dotnet run --project family_tree_mcp_demo/mcp_client_sk/mcp_client_sk.csproj
   ```

   or the __Microsoft.Extensions.AI__ based client:
   ```sh
   dotnet run --project family_tree_mcp_demo/mcp_client_meai/mcp_client_meai.csproj
   ```

4. Or you can just run the server and use VSCode as your client:
   ```
   Open 'mcp.json' in VSCode and select 'Start' to start the MCP server
   ```
   The server will start and listen for MCP requests as configured in `.vscode/mcp.json`.

### Testing

Run all tests with:
```sh
cd mcp_tests
dotnet test
```

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
