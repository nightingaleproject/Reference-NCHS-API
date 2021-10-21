This is a C# implementation of the NCHS Messaging Infrastructure described in section A.2 of the [FHIR Messaging for NVSS](https://github.com/nightingaleproject/vital_records_fhir_messaging/releases/download/v3.1.0/fhir_messaging_for_nvss.pdf) document. It leverages the [VRDR Messaging](https://www.nuget.org/packages/VRDR.Messaging) library for parsing and constructing messages.

# Features

 - Background message unpacking and conversion to IJE.
 - Sending of ACK messages to jurisdictions on successful message receive.
 - Writes IJE messages to SQL server.

# Steps to start the API In Development

1. Run MSSQL server: In development `docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=yourStrong(!)Password' -p 1433:1433 -d mcr.microsoft.com/mssql/server`, or in production configure a proper MSSQL instance to your organizational requirements.
2. Ensure the `NVSSMessagingDatabase` contains the proper MSSQL connection string for your environment in `messaging/appsettings.Development.json`
3. Migrate your local database to match the current migration: `dotnet run --project messaging database update`
4. Run the server using `dotnet run --project messaging`

# Deploying in Production

1. Setup a proper MSSQL instance meeting your organizational requirements, or have the credentials to an existing instance available.
2. Rename `appsettings.json.sample` to `appsettings.json` inside of the `messaging` folder.
3. Ensure the `NVSSMessagingDatabase` contains the proper MSSQL connection string for your environment in `messaging/appsettings.json` are set correctly.
4. Migrate your local database to match the current migration: `dotnet run --project messaging database update`
5. Run the server using `dotnet run --project messaging`

# Interacting with the API

### Sending Messages

1. Create a FHIR VRDR Message. This can be done using the **Creating FHIR VRDR Messages** option in [Canary](https://github.com/nightingaleproject/canary) if you do not have an existing tool to create FHIR VRDR Messages available.
2. Submit the message using a POST request to the `/Bundles` endpoint, here is an example doing so with [curl](https://curl.se/):
```bash
curl --location --request POST 'https://localhost:5001/Bundles' \
--header 'Content-Type: application/json' \
--data "@path/to/file.json"
```
3. This will return a 204 no content HTTP response if everything is functioning correctly.

### Receiving Messages
1. The API provides an endpoint to get a bundle of ACK messages, they can be retrieved using the following command:
```bash
curl https://localhost:5001/Bundles
```
2. Time based filtering is also available, and can be done by providing a `lastUpdated` parameter as a filter:
```bash
curl "https://localhost:5001/Bundles?lastUpdated=2021-10-21T17:21:41.492893-04:00"
```
