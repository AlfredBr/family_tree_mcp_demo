Push-Location .\family_webapi
dotnet build
Pop-Location

push-Location .\mcp_client_http
dotnet build
Pop-Location

push-Location .\mcp_client_meai
dotnet build
Pop-Location

push-Location .\mcp_client_openai
dotnet build
Pop-Location

push-Location .\mcp_client_sk
dotnet build
Pop-Location

Push-Location .\mcp_library
dotnet build
Pop-Location

Push-Location .\mcp_tests
dotnet build
Pop-Location

Push-Location .\mcp_webapi
dotnet build
Pop-Location

#Start-Process -NoNewWindow -FilePath dotnet -ArgumentList 'run --project .\family_webapi\family_webapi.csproj'
#Start-Process -NoNewWindow -FilePath dotnet -ArgumentList 'run --project .\mcp_webapi\mcp_webapi.csproj'