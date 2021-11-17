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

1. Create a FHIR Record. The standard that specifies this format can be found [here](https://build.fhir.org/ig/HL7/vrdr/branches/Sep_2021_Connectathon/). There are also two public library implementations available to assist in the creation of FHIR Records, [VRDR-dotnet](https://github.com/nightingaleproject/vrdr-dotnet) and [VRDR_javalib](https://github.com/MortalityReporting/VRDR_javalib)
2. Create a FHIR VRDR Message. The standard that specifies this format can be found [here](http://build.fhir.org/ig/nightingaleproject/vital_records_fhir_messaging_ig/branches/main/index.html). The [VRDR-dotnet Messaging library](https://github.com/nightingaleproject/vrdr-dotnet/blob/master/doc/Messaging.md) also supports creating FHIR Messages from an existing Record. If you wish to generate synthetic messages for testing, the [Canary](https://github.com/nightingaleproject/canary) project has a **Creating FHIR VRDR Messages** option in which will create an appropriate synthetic message for POSTing to the API.
3. Submit the message using a POST request to the `/Bundles` endpoint, here is an example doing so with [curl](https://curl.se/):
```bash
curl --location --request POST 'https://localhost:5001/Bundles' \
--header 'Content-Type: application/json' \
--data "@path/to/file.json"
```
3. This will return a 204 no content HTTP response if everything is functioning correctly.
Example Response:
```
put example response here and include headers
```

### Receiving Messages
1. NCHS returns messages to the jurisdiction by offering a message retrieval interface that can be polled rather than sending messages to a jurisdiction endpoint. The API provides an endpoint to get a bundle of messages from NCHS, they can be retrieved using a GET request to the `/Bundles` endpoint, here is an example doing so with [curl](https://curl.se/):
```bash
curl https://localhost:5001/Bundles
```
2. Time based filtering is also available, and can be done by providing a `lastUpdated` parameter as a filter. The best practice is to use time based filtering whenever retrieving messages. Always keep track of the last time polling was performed and use that timestamp to filter results in order to only retrieve messages that have not previously been processed.
```bash
curl "https://localhost:5001/Bundles?lastUpdated=2021-10-21T17:21:41.492893-04:00"
```
3. These requests return a 200 Response header with a body containing a [FHIR Bundle](https://www.hl7.org/fhir/bundle.html) of type 'searchset' containing a list of FHIR Messages. These messages can be either ACK, Error, or Coding Responses.
Example Response:
```
put example response here and include headers (make it verbose)
```
