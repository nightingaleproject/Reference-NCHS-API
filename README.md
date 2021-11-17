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
4. Migrate your local database to match the current migration: `dotnet ef --project messaging database update`
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
> POST /Bundles HTTP/1.1
> Host: localhost:5001
> User-Agent: curl/7.64.1
> Accept: */*
> Content-Type: application/json
> Content-Length: 46643
> Expect: 100-continue
>
< HTTP/1.1 100 Continue
* We are completely uploaded and fine
< HTTP/1.1 204 No Content
< Date: Wed, 17 Nov 2021 21:56:03 GMT
< Server: Kestrel
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
> GET /Bundles HTTP/1.1
> Host: localhost:5001
> User-Agent: curl/7.64.1
> Accept: */*
>
< HTTP/1.1 200 OK
< Date: Wed, 17 Nov 2021 21:58:21 GMT
< Content-Type: application/json; charset=utf-8
< Server: Kestrel
< Content-Length: 2213

{
  "resourceType": "Bundle",
  "type": "searchset",
  "timestamp": "2021-11-17T16:58:22.091838-05:00",
  "entry": [{
    "fullUrl": "urn:uuid:d4d597b3-2634-412f-b41d-0017ad4cfb15",
    "resource": {
      "resourceType": "Bundle",
      "id": "e6752e31-799c-4732-82e7-85ba967a4779",
      "type": "message",
      "timestamp": "2021-11-17T16:55:55.090601-05:00",
      "entry": [{
        "fullUrl": "urn:uuid:d4d597b3-2634-412f-b41d-0017ad4cfb15",
        "resource": {
          "resourceType": "MessageHeader",
          "id": "d4d597b3-2634-412f-b41d-0017ad4cfb15",
          "eventUri": "http://nchs.cdc.gov/vrdr_acknowledgement",
          "destination": [{
            "endpoint": "https://example.com/jurisdiction/message/endpoint"
          }],
          "source": {
            "endpoint": "http://nchs.cdc.gov/vrdr_submission"
          },
          "response": {
            "identifier": "1eba0dc0-9d5a-48ec-9536-67e56d7fa130",
            "code": "ok"
          },
          "focus": [{
            "reference": "urn:uuid:f753e87e-5058-47a4-83f7-a1d8375f5e44"
          }]
        }
      }, {
        "fullUrl": "urn:uuid:f753e87e-5058-47a4-83f7-a1d8375f5e44",
        "resource": {
          "resourceType": "Parameters",
          "id": "f753e87e-5058-47a4-83f7-a1d8375f5e44",
          "parameter": [{
            "name": "cert_no",
            "valueUnsignedInt": 365483
          }, {
            "name": "state_auxiliary_id",
            "valueString": "650014"
          }, {
            "name": "jurisdiction_id",
            "valueString": "MA"
          }, {
            "name": "death_year",
            "valueUnsignedInt": 2021
          }]
        }
      }]
    }
  }, {
    "fullUrl": "urn:uuid:9e553c61-8510-416f-984b-f5b70d9ce4fd",
    "resource": {
      "resourceType": "Bundle",
      "id": "85a7e61d-578a-4e4a-ae84-6b6f402f9048",
      "type": "message",
      "timestamp": "2021-11-17T16:56:03.736701-05:00",
      "entry": [{
        "fullUrl": "urn:uuid:9e553c61-8510-416f-984b-f5b70d9ce4fd",
        "resource": {
          "resourceType": "MessageHeader",
          "id": "9e553c61-8510-416f-984b-f5b70d9ce4fd",
          "eventUri": "http://nchs.cdc.gov/vrdr_acknowledgement",
          "destination": [{
            "endpoint": "https://example.com/jurisdiction/message/endpoint"
          }],
          "source": {
            "endpoint": "http://nchs.cdc.gov/vrdr_submission"
          },
          "response": {
            "identifier": "1eba0dc0-9d5a-48ec-9536-67e56d7fa130",
            "code": "ok"
          },
          "focus": [{
            "reference": "urn:uuid:03890087-4891-4802-a2cf-68c1d895f762"
          }]
        }
      }, {
        "fullUrl": "urn:uuid:03890087-4891-4802-a2cf-68c1d895f762",
        "resource": {
          "resourceType": "Parameters",
          "id": "03890087-4891-4802-a2cf-68c1d895f762",
          "parameter": [{
            "name": "cert_no",
            "valueUnsignedInt": 365483
          }, {
            "name": "state_auxiliary_id",
            "valueString": "650014"
          }, {
            "name": "jurisdiction_id",
            "valueString": "MA"
          }, {
            "name": "death_year",
            "valueUnsignedInt": 2021
          }]
        }
      }]
    }
  }]
}
```
