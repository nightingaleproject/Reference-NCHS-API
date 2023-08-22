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
POST https://localhost:5001/<jurisdiction-id>/Bundle
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
GET https://localhost:5001/<jurisdiction-id>/Bundle
```
which returns any message response that has not been retrieved yet
or
```
GET https://localhost:5001/<jurisdiction-id>/Bundle?_since=yyyy-MM-ddTHH:mm:ss.fffffff
```
which returns any message created after the datetime provided in the _since parameter
or
```
GET https://localhost:5001/<jurisdiction-id>/Bundle?certificateNumber=xxxx&deathYear=yyyy
```
which returns any message that matches the given business ids: jurisidicion id, certificate number, and death year. Certificate number and death year are optional parameters, any combination of business IDs will further filter the results. When certificate number or death year are provided, it will not filter out previously retrieved messages.

The API supports GET requests to retrieve responses from NCHS, including:

 * Acknowledgment messages acknowledging jurisdiction-submitted submission, update, and void messages
 * Error messages describing problems with jurisdiction-submitted messages
 * Coding response messages coding jurisdiction-submitted data such as cause of death, race, and ethnicity

The API supports a `_since` parameter that will limit the messages returned to only message responses created since the provided timestamp.

Messages flow from NCHS back to jurisdictions by jurisdiction systems polling the API looking for
new responses. This approach of pulling responses rather than NCHS pushing responses to
jurisdictions allows return messages without requiring jurisdictions to set up a listening endpoint.

## Pagination
The API implements pagination. The default page size in production is set to 100. The test environment is set to 25. 

There are 3 optional parameters for pagination.

- `_count` used to specify the number of records per page, default is 100 in prod and 25 in test
- `_since` used to retrieve all response messages created after the `_since` datetime, __for testing only__
- `page` used to specify the page of data you are interested in. This parameter is only used if a `_since` datetime is provided, __for testing only__

If a response contains a `next` link, there is additional data to retrieve. Client side systems should parse out the `next` link and automate the next request to retrieve the next page of data. If a `_since` parameter is not provided, the `next` link will not change.

### Default Pagination
It is recommended to use the default GET request when implementing automated client systems. The default behavior is implemented as a queue. Each default request will return the next page of unretrieved messages.
#### Example responses with links.
1. A default request will return the first 100 responses from the unretrieved messages queue. A default `next` link is provided.  
    Request  
    ```
    GET https://localhost:5001/<jurisdiction-id>/Bundle
    ```
    Response links  
    ```
        "link": [
            {
                "relation": "next",
                "url": " https://localhost:5001/OSELS/NCHS/NVSSFHIRAPI/MA/Bundle?_count=100"
            }
        ]
    ```
2. A default request with a specified page size of 50. This will return the first 50 responses from the unretrieved messages queue. A default `next` link is provided.  
    Request  
    ```
    GET https://localhost:5001/<jurisdiction-id>/Bundle?_count=50
    ```
    Response links  
    ```
        "link": [
            {
                "relation": "next",
                "url": "https://localhost:5001/<jurisdiction-id>/Bundle?_count=50"
            }
        ]
    ```

### Testing with the Since Parameter
When testing, there may be a need to retrieve messages that were already pulled off the queue. The `_since` parameter allows users to request messages based on timestamp versus using a queue. The `_since` parameter is intended for testing and special cases where messages need to be retrieved a second time from NCHS. It should no be used when implementing an automated client side system.

#### Example responses with links.

1. A timestamp based request with a specified page size of 50 records. If there are additional pages to retrieve, there will be a `next` link. Will return the first 50 responses created after the _since datetime parameter and a `first`, `last`, and `next` link if there is more data to retrieve.  
    Request  
    ```
    GET https://localhost:5001/<jurisdiction-id>/Bundle?_since=2022-06-16T10:28:01.000-05:00&_count=50
    ```
    Response links  
    ```
        "link": [
            {
                "relation": "first",
                "url": " https://localhost:5001/<jurisdiction-id>/Bundle?_since=2022-06-16T10:28:01.000-05:00&_count=50&page=1"
            },
            {
                "relation": "last",
                "url": " https://localhost:5001/<jurisdiction-id>/Bundle?_since=2022-06-16T10:28:01.000-05:00&_count=50&page=3"
            },
            {
                "relation": "next",
                "url": " https://localhost:5001/<jurisdiction-id>/Bundle?_since=2022-06-16T10:28:01.000-05:00&_count=50&page=2"
            }
        ],
    ```
2. The 3rd and last page of a timestamp based request with a specified page size of 50 records. If this is the last page, there will not be a `next` link. Will return the third page of 50 responses created after the _since datetime parameter and a `first`, `last` link. There is no `next` link because there is no more data to retrieve.  
    Request  
    ```
    GET https://localhost:5001/<jurisdiction-id>/Bundle?_since=2022-06-16T10:28:01.000-05:00&_count=50&page=3
    ```
    Response links  
    ```
        "link": [
            {
                "relation": "first",
                "url": " https://localhost:5001/<jurisdiction-id>/Bundle?_since=2022-06-16T10:28:01.000-05:00&_count=50&page=1"
            },
            {
                "relation": "last",
                "url": " https://localhost:5001/<jurisdiction-id>/Bundle?_since=2022-06-16T10:28:01.000-05:00&_count=50&page=3"
            }
        ],
    ```

## STEVE

There are two instances of FHIR APIs meeting this specification for jurisdictions to submit
mortality data. One is provided as part of the State and Territorial Exchange of Vital Events
(STEVE) system, and is intended to be the primary means for jurisdictions to both submit data to
NCHS and to exchange FHIR-based vital records data with other jurisdictions. The other is provided
by NCHS and is intended to act as a backup channel to the STEVE FHIR API in the event of a long term
outage. Both APIs implement the same specification, so moving submission to the backup NCHS API if
ever needed should require only a simple configuration change. When performing testing both channels
should be included to insure that the backup channel configuration is functional.

Jurisdiction client implementations in a production context are expected to only use the primary
STEVE FHIR API for data submission to NCHS. If both APIs are used, the following behavior should be
expected:

* **Interjurisdictional Exchange**: Data submitted via the STEVE API will go to NCHS and also be
  used for interjurisdictional exchange as configured by the submitting jurisdiction on STEVE. Data
  submitted via the NCHS API will only go to NCHS and will not go to other jurisdictions.

* **Duplicate Submissions**: If a duplicate message is POST'd both through STEVE and directly to
  NCHS the second message received will be ignored by NCHS. This is based on comparing the message
  ID of the submission, not on the content of the message, and is the same behavior as expected
  when submitting two duplicate copies of a message to the same API.

* **Duplicate Retrievals**: The NVSS FHIR APIs keep track of which messages have been retrieved via
  GET requests. Subsequent GET requests will not retrieve messages that have already been retrieved.
  However, the NCHS and STEVE FHIR APIs keep track of which messages have been retrieved separately
  from each other. If a jurisdiction places a GET request through STEVE and subsequently places a
  GET request directly to NCHS the two requests will contain duplicate messages. If for any reason a
  client implementation needs to make requests through both channels the client is responsible for
  ignoring duplicate messages that come through both channels using the message ID to detect
  duplicates.

## Authentication
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
You can also view an always-available version of the documentation hosted on GitHub at [NVSSMessaging Swagger](https://nightingaleproject.github.io/Reference-NCHS-API/).

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
curl --header 'Authorization: Bearer <OAuthToken>' https://localhost:5001/MA/Bundle
```

## Sending Messages

1. Create a FHIR Record. The standard that specifies this format can be found [here](https://build.fhir.org/ig/HL7/vrdr/branches/Sep_2021_Connectathon/). There are also two public library implementations available to assist in the creation of FHIR Records, [VRDR-dotnet](https://github.com/nightingaleproject/vrdr-dotnet) and [VRDR_javalib](https://github.com/MortalityReporting/VRDR_javalib).
2. Create a FHIR VRDR Message to act as an envelope for the FHIR Record created above. The standard that specifies this format can be found [here](http://build.fhir.org/ig/nightingaleproject/vital_records_fhir_messaging_ig/branches/main/index.html). The [VRDR-dotnet Messaging library](https://github.com/nightingaleproject/vrdr-dotnet/blob/master/doc/Messaging.md) also supports creating FHIR Messages from an existing Record. If you wish to generate synthetic messages for testing, the [Canary](https://github.com/nightingaleproject/canary) project has a **Creating FHIR VRDR Messages** option in which will create an appropriate synthetic message for POSTing to the API.
3. Submit the message using a POST request to the `/<JurisdictionID>/Bundle` endpoint; the following example demonstrates the request format using [curl](https://curl.se/):
```bash
curl --location --request POST 'https://localhost:5001/MA/Bundle' \
--header 'Content-Type: application/json' \
--header 'Authorization: Bearer <OAuthToken>' \
--data "@path/to/file.json"
```
3. The API will return a 204 No Content HTTP response if everything is functioning correctly.
Example Response:
```
> POST /MA/Bundle HTTP/1.1
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
4. Submit the "batch" Bundle of messages using a POST request to the `/<JurisdictionID>/Bundle` endpoint in the same way a single message can be submitted; the following example demonstrates the request format using [curl](https://curl.se/):
```bash
curl --location --request POST 'https://localhost:5001/MA/Bundle' \
--header 'Content-Type: application/json' \
--header 'Authorization: Bearer <OAuthToken>' \
--data "@path/to/file.json"
```
5. The API will return a 200 OK HTTP response if the overall bulk upload was processed correctly; this does not provide information on the status of the individual records with the batch.
6. On a successful submission the HTTP response will contain a payload of a `batch-response` Bundle with response codes for each individual record that was submitted. The response codes appear in the same order that the records were submitted in the bulk upload. The individual response codes should each be checked to ensure that they all have a successful `201` status code:

```
{
  "resourceType": "Bundle",
  "type": "batch-response",
  "timestamp": "2022-12-22T12:14:09.1780469-05:00",
  "entry": [
    {
      "response": {
        "status": "201"
      }
    },
    {
      "response": {
        "status": "201"
      }
    },
    {
      "response": {
        "status": "201"
      }
    }
  ]
}
```

#### Bulk Upload Batch Size

Bulk upload is strongly recommended to increase efficient and performant use of the API. However,
for efficient use of the API batches should also not exceed 10MB in size. Given the size of a
typical record his means that batch sizes from 20 to 100 records should work well.

## Receiving Messages
1. NCHS returns messages to the jurisdiction by offering a message retrieval interface that can be polled rather than sending messages to a jurisdiction endpoint. The API provides an endpoint to retrieve a bundle of messages from NCHS: response messages can be retrieved using a GET request to the `/<JurisdictionID>/Bundle` endpoint. The following example demonstrates the request format using [curl](https://curl.se/):
```bash
curl --header 'Authorization: Bearer <OAuthToken>' https://localhost:5001/MA/Bundle
```
2. Time based filtering is also available, and can be used by providing the [FHIR parameter](https://www.hl7.org/fhir/http.html) `_since` as a filter. The best practice is to use time based filtering whenever retrieving messages. Always keep track of the last time polling was performed and use that timestamp to filter results in order to only retrieve messages that have not previously been processed.
```bash
curl --header 'Authorization: Bearer <OAuthToken>' "https://localhost:5001/MA/Bundle?_since=2021-10-21T17:21:41.492893-04:00"
```
3. These requests return a 200 Response header with a body containing a [FHIR Bundle](https://www.hl7.org/fhir/bundle.html) of type 'searchset' containing a list of FHIR Messages. These messages can be either ACK, Error, or Coding Responses.
Example Response:
```
> GET /MA/Bundle HTTP/1.1
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
# Http Error Responses
|Endpoint     |Http Response Code|Common Fixes|
|-------------|------------------|------------|
|POST /token (SAMS) | 400 - Bad Request| Check all required parameters are present in the token request|
|             | 401 - Unauthorized, invalid_request| Check the username and password credentials are correct|
|             | 401 - Unauthorized, invalid_client| Check the client id and client secret are correct|
|POST /Bundle | 400 - Bad Request| Check the body of the request is valid VRDR Message. If using pagination, check the parameters are valid.|
|             | 500 - Internal Server Error, Authorization Failure (SAMS)| Refresh expired token|
|             | 500 - Internal Server Error| Report issue on Zulip|
|             | 501 - Not implemented| Check the url is correct|
|GET /Bundle  | 500 - Internal Server Error, Authorization Failure (SAMS)| Refresh expired token|



# API Developer Documentation

This section documents information useful for developers of the API itself and is not relevant to users of the API or developers of systems that use the API.

## Version Updates
1. Update the CHANGELOG.md
2. Update `NVSSAPI_CapStmt.fsh` to use the new version number
3. Regenerate the fsh file so CapabilityStatement-NVSS-API-CS.json includes the new version number

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
