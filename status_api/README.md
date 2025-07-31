# NVSS Status UI (Frontend and Backend Status API)

## Dependencies
 - .NET Core 6.100
 - Docker with MSSQL Server

## Quick Start

1. This `status_api` project uses the models, migrations, and database from the NVSS FHIR API `messaging` project. If `messaging` is not already setup, see [the top-level GettingStarted.md](../GettingStarted.md). You can use [Canary](https://canary.fhir.nvss.cdc.gov/) to generate sample messages or run our [exercise_local_api.rb testing tool](../testing_tools/README.md) to populate the database.

2. Optional: the frontend is compiled into `status_api/StatusUI` and checked into git from its source at `status_ui`. If you want to rebuild the frontend follow its [README](../status_ui/README.md).

3. Launch `status_api`, which serves both the backend and frontend; from `status_api/` folder run:

```shell
dotnet run
```

4. Visit <https://127.0.0.1:5003/StatusUI/index.html> to view the StatusUI.

## Endpoints

|          Path             |       Purpose                             |
|:-------------------------:|:------------------------------------------|
| /StatusUI/index.html      | Frontend dashboard                        |
| /api/v1/status            | Backend API endpoint                      |
| /swagger                  | Open API Documentation (development only) |
| /profiler/results-index   | Profiling (development only)              | 

## Testing

1. Create the test database for the first run and anytime there is a new migration. From `messaging/` run:

```shell
dotnet ef database update -- --environment Test
```

2. Execute tests. From `status_api/` run:

```shell
dotnet test ../status_api.tests
```

