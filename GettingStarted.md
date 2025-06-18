# NVSS FHIR API Getting Started for Development

This guide walks through first time for local development of this FHIR server. It includes setting up a native .NET environment via CLI and running the Microsoft SQL server through Docker.

1. Install [.NET Core 6.0.100](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) by going to the .NET Core 6.0 download page, scrolling down to 6.0.0, and collapsing the dropdown to find 6.0.100 SDK. You can also find [an alternative install on GitHub](https://github.com/dotnet/core/blob/main/release-notes/6.0/6.0.0/6.0.0.md?WT.mc_id=dotnet-35129-website).

2. Install [Docker Desktop](https://www.docker.com/) to enable running [containerized applications](https://www.docker.com/resources/what-container/).

3. In a command line interface, download the MSSQL Docker image with the command:
```
docker pull mcr.microsoft.com/mssql/server:2022-latest
```
  - Optional: increase Docker Desktop RAM to 4GB for smoother performance. [2GB minimum is required](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver16&tabs=cli&pivots=cs1-bash#prerequisites).

4. Install .NET EF Core for managing database migrations and schema:
```
dotnet tool install --global dotnet-ef --version 7
```

5. Clone the [Reference Server](https://github.com/nightingaleproject/Reference-NCHS-API) and make it the working directory.

6. Pick a secure random password, which the remainder of this guide will refer to as `yourStrong(!)Password`. **Never commit this password to GitHub.**

7. Open the file `messaging/appsettings.Development.json` and modify the `NVSSMessagingDatabase` field to use the Password `yourStrong(!)Password`. **Never commit this password to GitHub.**

8. Launch MSSQL database server in the background via Docker:
```
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1433:1433 -d mcr.microsoft.com/mssql/server
```
 - Optional: run `docker ps` to check working Docker services.
 - Note: On an Apple silicon Mac you may need to add `--platform=linux/amd64` to the docker run command above.
 - Warning: Based on your shell special characters may need to be escaped or avoided.

9. Load the database migrations:
```
dotnet ef --project messaging database update
```

 - Note: To reset the database you can use `dotnet ef --project messaging database drop` followed by `dotnet ef --project messaging database update`

10. Run the server with the command below. It may ask you for a system password when generating a developer certificate.
```
dotnet run --project messaging
```

If it worked successfully you should see something like this:
```
Building...
[15:46:53 INF] Starting the NVSS FHIR API
[15:46:53 INF] Now listening on: https://localhost:5001
[15:46:53 INF] Now listening on: http://localhost:5000
[15:46:53 INF] Background job processing has started

[15:46:53 INF] Application started. Press Ctrl+C to shut down.
[15:46:53 INF] Hosting environment: Development
[15:46:53 INF] Content root path: /Users/nightingaleproject/nvss/Reference-NCHS-API/messaging
```

11. If the system keeps prompting you for a password, you can have the system trust your new developer cert by running the command:
```
dotnet dev-certs https --trust
```

12. To confirm the server is running properly, visit <https://127.0.0.1:5001/swagger/v1/swagger.json>, where you should get a JSON file and see `StatusCode: 200` in the server logs. Authentication and authorization are disabled in development mode. You can view the [REST API documentation](https://nightingaleproject.github.io/Reference-NCHS-API/#/) on GitHub Pages or <http://localhost:5001/swagger/index.html>.


## Run Messaging Tests

From `messaging` directory with MSSQL database already running, run:

```shell
dotnet test
```

## Next Steps

In any order:

 - Use the [Canary Testing Framework](https://canary.fhir.nvss.cdc.gov/) to generate sample FHIR messages.
 - Try the [Postman Collections](https://github.com/nightingaleproject/Reference-NCHS-API/tree/main/examples/README.md) provided, but disregard authentication and use `https://127.0.0.1:5001/` as the base URL.
 - Launch the [StatusUI](status_api/README.md) to get a NVSS FHIR messages status dashboard.
 - Checkout our [Testing Tools](testing_tools/README.md) for probing the NVSS FHIR API.
