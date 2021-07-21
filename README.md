# Steps to start the API

1. Run MSSQL server: `docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=yourStrong(!)Password' -p 1433:1433 -d mcr.microsoft.com/mssql/server`
2. Migrate your local database to match the current migration: `dotnet run database update`
3. Run the server using `dotnet run`
