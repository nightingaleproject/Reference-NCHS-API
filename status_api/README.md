# NVSS API Status App (API and UI Build)

## Dependencies
 - .NET Core 6.100
 - Docker with 

## Quick Start

1. The status_api uses models, migrations, and database from `messaging`. Follow its [README](../README.md) to get started
and run a few API calls to populate its database.

2. Go to the `status_ui/` folder and build the frontend:

```shell
npm install
npm run build
```
3. Launch Status App, which serves a status backend and frontend. From `status_api/` folder run:

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

1. Create the test database for the first run and anytime there is a new migration. From `status_api/` run:

```shell
dotnet ef database update -- --environment Test
```

2. Execute tests. From `status_api/` run:

```shell
dotnet test ../status_api.tests
```

