@set configuration=debug
dotnet build --configuration %configuration%
dotnet run --configuration %configuration% --urls=http://localhost:5001/
