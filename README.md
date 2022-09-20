This repository provides a description of the NVSS API exchange of mortality data between
NCHS and vital records jurisdictions, along with a reference implementation. This implementation and
documentation describes the server side of the API. For the client side see the
[Reference NVSS Client API](https://github.com/nightingaleproject/Reference-Client-API).

# Overview

NCHS is working to modernize the national collection and exchange of mortality data by developing
and deploying new Application Programming Interfaces (APIs) for data exchange, implementing modern
standards health like HL7's Fast Healthcare Interoperability Resources (FHIR), and improving overall
systems and processes. This repository provides a reference implementation and documentation
describing the NVSS API, which supports the exchange of mortality data between NCHS and vital
records jurisdictions.

This reference implementation is developed for .NET using C# and implements the NCHS Messaging
Infrastructure section of the
[FHIR Messaging for NVSS](http://build.fhir.org/ig/nightingaleproject/vital_records_fhir_messaging_ig/branches/main/appendix.html#nchs-fhir-messaging-infrastructure)
documentation. It leverages the
[VRDR Messaging](https://www.nuget.org/packages/VRDR.Messaging)
library for parsing and constructing messages.

This implementation describes the NVSS API hosted by NCHS and the compatible implementation hosted
via the STEVE 2.0 system.

# The NVSS API

The NVSS API can be accessed by vital records jurisdictions in order to submit mortality data to
NCHS and receive acknowledgments, errors, and coded data in response. An API is a set of rules that
describe how two systems can communicate with each other. The NVSS API allows vital records
jurisdiction mortality data systems to automate communication with NCHS in a robust and repeatable
way. Automation improves timeliness of data exchange and reduces burden on vital records
stakeholders.

The NVSS API uses a RESTful approach. REST, or
[Representational State Transfer](http://www.ics.uci.edu/~fielding/pubs/dissertation/rest_arch_style.htm),
is an architectural style that is typically implemented using internet technologies like the
[Hypertext Transfer Protocol (HTTP)](https://datatracker.ietf.org/doc/html/rfc2616) and
[JavaScript Object Notation (JSON)](https://datatracker.ietf.org/doc/html/rfc8259).
REST offers a simple stateless request-response pattern that makes building applications straightforward.

The NVSS API is built using the [FHIR](htatp://hl7.org/fhir/) standard.  FHIR is a RESTful standard
for the electronic exchange of health information. FHIR's focus on health data and its basis on
internet standards make it a good fit for exchanging mortality data.  The fundamental building block
for organizing data in FHIR is the [Resource](https://www.hl7.org/fhir/resource.html). A FHIR
Resource is just a well-specified way to represent a single concept, like a Patient or a
Condition. The NVSS API uses the [Bundle](https://www.hl7.org/fhir/bundle.html) resource to
represent and share information about mortality data in the form of
[FHIR Messages](https://www.hl7.org/fhir/messaging.html). Jurisdictions send mortality records to
NCHS and NCHS responds using
[Vital Records Death Reporting FHIR Messages](http://build.fhir.org/ig/nightingaleproject/vital_records_fhir_messaging_ig/branches/main/index.html).

## Endpoints
### Submit Death Records
```
POST https://localhost:5001/<jurisdiction-id>/Bundles
```
The API supports several types of POST interaction:

* POSTing a submission message containing a new death record, which represents the information collected on a death certificate
* POSTing an update message containing an updated version of an existing death record, suitable for amending previous submissions
* POSTing a void message voiding one or more existing death records, used to indicate that a previously submitted record is no longer valid
* POSTing an alias message providing decedent alias information for an existing death record, used to provide additional identifying information about a decedent
* POSTing an acknowledgment message acknowledging receipt of a coding response message from NCHS
* POSTing a batch submission, used to submit multiple messages at once, each message is processed independent of other messages in the batch, returns a batch-response with status codes for each message submitted via the original bundle, [FHIR batch documentation](http://www.hl7.org/fhir/http.html#transaction) 

### Receive Responses
```
GET https://localhost:5001/<jurisdiction-id>/Bundles
```
which returns any message response that has not been retrieved yet
or
```
GET https://localhost:5001/<jurisdiction-id>/Bundles/_since=yyyy-MM-ddTHH:mm:ss.fffffff
```
which returns any message created after the datetime provided in the _since parameter

The API supports GET requests to retrieve responses from NCHS, including:

 * Acknowledgment messages acknowledging jurisdiction-submitted submission, update, and void messages
 * Error messages describing problems with jurisdiction-submitted messages
 * Coding response messages coding jurisdiction-submitted data such as cause of death, race, and ethnicity

The API supports a `_since` parameter that will limit the messages returned to only message responses created since the provided timestamp.

Messages flow from NCHS back to jurisdictions by jurisdiction systems polling the API looking for
new responses. This approach of pulling responses rather than NCHS pushing responses to
jurisdictions allows return messages without requiring jurisdictions to set up a listening endpoint.

## STEVE
Jurisdictions can send their POST and GET requests through STEVE or direct to NCHS. 

### POST expected behavior
If a duplicate message is POST'd through STEVE and direct to NCHS, the second message received will be ignored. 

### GET expected behavior
If a jurisdiction places a GET request through STEVE and a GET request direct to NCHS, they will recieve all messages that have not been retrieved through that channel yet. Therefore, the two requests may have duplicate messages. Clients are expected to typically only use one channel. If the client needs to make requests through both channels, the client is responsible for ignoring duplicate messages that come through both channels.

### Authenticate
```
POST https://<OAuthHost>/auth/oauth/v2/token
```

Secure access to the NVSS API is provided using

1. encryption of all connections using the HTTPS protocol.
2. access control of connections using the [OAuth (Open Authorization) 2.0](https://oauth.net/2/) standard.

OAuth 2.0 is an open standard for authorization and access delegation. OAuth is used in the
following manner:

* A vital records jurisdiction representative requests an account
* Identity proofing is conducted on the jurisdiction representative's request
* The jurisdiction representative's account is approved and system credentials are issued
* Jurisdiction vital records system uses credentials to retrieve an access token
* Jurisdiction vital records system includes the appropriate access token for subsequent API requests

# Open API Documentation
To view the swagger generated Open API documentation, run the service using the instructions in [Steps to start the API In Development](https://github.com/nightingaleproject/Reference-NCHS-API#steps-to-start-the-api-in-development) below and navigate to https://localhost:5001/swagger/index.html 

# Interacting with the API

## Requesting an OAuth Token

Before interacting with the API an OAuth token must be retrieved for use in subsequent requests using the
[OAuth Password Grant](https://www.oauth.com/oauth2-servers/access-tokens/password-grant/)
as demonstrated by the following requests using [curl](https://curl.se/):

```bash
# Request a token using password grant
curl --request POST --url 'https://<OAuthHost>/auth/oauth/v2/token' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data grant_type=password \
--data client_id='<OAuthClientID>' \
--data client_secret='<OAuthClientSecret>' \
--data username='<Username>' \
--data password='<Password>'

# Send a request using the retrieved token
curl --header 'Authorization: Bearer <OAuthToken>' https://localhost:5001/MA/Bundles
```

## Sending Messages

1. Create a FHIR Record. The standard that specifies this format can be found [here](https://build.fhir.org/ig/HL7/vrdr/branches/Sep_2021_Connectathon/). There are also two public library implementations available to assist in the creation of FHIR Records, [VRDR-dotnet](https://github.com/nightingaleproject/vrdr-dotnet) and [VRDR_javalib](https://github.com/MortalityReporting/VRDR_javalib).
2. Create a FHIR VRDR Message to act as an envelope for the FHIR Record created above. The standard that specifies this format can be found [here](http://build.fhir.org/ig/nightingaleproject/vital_records_fhir_messaging_ig/branches/main/index.html). The [VRDR-dotnet Messaging library](https://github.com/nightingaleproject/vrdr-dotnet/blob/master/doc/Messaging.md) also supports creating FHIR Messages from an existing Record. If you wish to generate synthetic messages for testing, the [Canary](https://github.com/nightingaleproject/canary) project has a **Creating FHIR VRDR Messages** option in which will create an appropriate synthetic message for POSTing to the API.
3. Submit the message using a POST request to the `/<JurisdictionID>/Bundles` endpoint; the following example demonstrates the request format using [curl](https://curl.se/):
```bash
curl --location --request POST 'https://localhost:5001/MA/Bundles' \
--header 'Content-Type: application/json' \
--header 'Authorization: Bearer <OAuthToken>' \
--data "@path/to/file.json"
```
3. The API will return a 204 No Content HTTP response if everything is functioning correctly.
Example Response:
```
> POST /MA/Bundles HTTP/1.1
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

### Sending Bulk Messages

Multiple messages can be sent using a single connection. When possible multiple messages should be
sent per connection to increase efficiency of API use. Sending multiple messages is similar to
sending a single message; the set of message is simply wrapped in a "batch" FHIR Bundle. Note that
the concept of "batch" used here is different than the batch processing used with the IJE standard
in that this is simply a way of sending multiple messages at once; the messages in a bulk upload
will not necessarily be processed or acknowledged together. To bulk upload messages,

1. Create one or more FHIR Records, as described in the "Sending Messages" section above.
2. Create FHIR VRDR Messages to act as envelopes for the FHIR Records created above, as described in the "Sending Messages" section above.
3. Wrap the messages in a "batch" Bundle as described [in the FHIR specification](https://www.hl7.org/fhir/http.html#transaction); refer to the [example "batch" Bundle](./messaging.tests/fixtures/json/BatchMessages.json) for details.
4. Submit the "batch" Bundle of messages using a POST request to the `/<JurisdictionID>/Bundles` endpoint in the same way a single message can be submitted; the following example demonstrates the request format using [curl](https://curl.se/):
```bash
curl --location --request POST 'https://localhost:5001/MA/Bundles' \
--header 'Content-Type: application/json' \
--header 'Authorization: Bearer <OAuthToken>' \
--data "@path/to/file.json"
```
4. The API will return a 200 OK HTTP response if everything is functioning correctly.

## Receiving Messages
1. NCHS returns messages to the jurisdiction by offering a message retrieval interface that can be polled rather than sending messages to a jurisdiction endpoint. The API provides an endpoint to retrieve a bundle of messages from NCHS: response messages can be retrieved using a GET request to the `/<JurisdictionID>/Bundles` endpoint. The following example demonstrates the request format using [curl](https://curl.se/):
```bash
curl --header 'Authorization: Bearer <OAuthToken>' https://localhost:5001/MA/Bundles
```
2. Time based filtering is also available, and can be used by providing the [FHIR parameter](https://www.hl7.org/fhir/http.html) `_since` as a filter. The best practice is to use time based filtering whenever retrieving messages. Always keep track of the last time polling was performed and use that timestamp to filter results in order to only retrieve messages that have not previously been processed.
```bash
curl --header 'Authorization: Bearer <OAuthToken>' "https://localhost:5001/MA/Bundles?_since=2021-10-21T17:21:41.492893-04:00"
```
3. These requests return a 200 Response header with a body containing a [FHIR Bundle](https://www.hl7.org/fhir/bundle.html) of type 'searchset' containing a list of FHIR Messages. These messages can be either ACK, Error, or Coding Responses.
Example Response:
```
> GET /MA/Bundles HTTP/1.1
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

# API Developer Documentation

This section documents information useful for developers of the API itself and is not relevant to users of the API or developers of systems that use the API.

## Steps to start the API In Development

1. Run MSSQL server: In development `docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=yourStrong(!)Password' -p 1433:1433 -d mcr.microsoft.com/mssql/server`, or in production configure a proper MSSQL instance to your organizational requirements.
2. Ensure the `NVSSMessagingDatabase` contains the proper MSSQL connection string for your environment in `messaging/appsettings.Development.json`
3. Migrate your local database to match the current migration: `dotnet ef --project messaging database update`
4. Run the server using `dotnet run --project messaging`

## Deploying in Production

1. Setup a proper MSSQL instance meeting your organizational requirements, or have the credentials to an existing instance available.
2. Rename `appsettings.json.sample` to `appsettings.json` inside of the `messaging` folder.
3. Ensure the `NVSSMessagingDatabase` contains the proper MSSQL connection string for your environment in `messaging/appsettings.json` are set correctly.
4. Migrate your local database to match the current migration: `dotnet ef --project messaging database update`
5. Run the server using `dotnet run --project messaging`

## Logging
The application uses Serilog as the third party log provider. Serilog replaces .NET standard Logging and can be configured in the appsettings.json file. 

### Logging Sinks
Serilog supports a variety of sinks to write your logs to. The default configuration writes the logs to the console and to a file. A new file is created each day and files are deleted after 31 days by default. These default configurations can be overwritten in the appsettings.json file. Serilog's provided sinks are listed [here](https://github.com/serilog/serilog/wiki/Provided-Sinks). Splunk, S3, and DBs are among the many options provided by serilog. To change the sink configuration, update the "Using" and "WriteTo" configuration fields in the example below.
```
  "Serilog": {
    "Using": [
      "Serilog.Sinks.ApplicationInsights", "Serilog.Sinks.File"
    ],
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "Logs/log.txt", "rollingInterval": "Day"} }
    ],
    ...
  }
```
### Turn Off Logging
To turn off logging to a sink, remove the `WriteTo` configuration for the sink you wish to remove. Ex. below will only write to the console.
```
  "Serilog": {
    "Using": [
      "Serilog.Sinks.ApplicationInsights"
    ],
    "WriteTo": [
      { "Name": "Console" }
    ],
    ...
  }
```

### Debug Logging
To turn on debug logging, update the log level from `Information` to `Debug` in appsettings.json, see example below
```
  "Serilog": {
    ...
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Information",
        "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware": "Debug"
      }
    }
  },
```


### Logging to File
To save logs to a file, uncomment the line below in Startup.cs
```
loggerFactory.AddFile("logs/nvssmessaging-{Date}.txt");
```
and this package reference in messaging.csproj
```
<PackageReference Include="Serilog.Extensions.Logging.File" Version="2.0.0"/>
```

# License

Copyright 2021 The MITRE Corporation

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

```
http://www.apache.org/licenses/LICENSE-2.0
```

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
