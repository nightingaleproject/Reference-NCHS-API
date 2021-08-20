This is a C# implementation of the NCHS Messaging Infrastructure described in section A.2 of the [FHIR Messaging for NVSS](https://github.com/nightingaleproject/vital_records_fhir_messaging/releases/download/v3.1.0/fhir_messaging_for_nvss.pdf) document. It leverages the [VRDR Messaging](https://www.nuget.org/packages/VRDR.Messaging) library for parsing and constructing messages.

# Features

 - Background message unpacking and conversion to IJE.
 - Sending of ACK messages to jurisdictions on successful message receive.
 - Writes IJE messages to SQL server.

# Steps to start the API

1. Run MSSQL server: `docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=yourStrong(!)Password' -p 1433:1433 -d mcr.microsoft.com/mssql/server`
2. Migrate your local database to match the current migration: `dotnet run database update`
3. Run the server using `dotnet run`
