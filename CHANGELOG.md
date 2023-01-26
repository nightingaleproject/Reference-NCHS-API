## Changelog

### v1.1.0-preview9 - 2023-01-26
* Improve performance when handling large numbers of records

### v1.1.0-preview8 - 2023-01-17
* Catches missing required fields in the message header and returns 400
* Returns 400 if the message event type is not a submission, update, void, alias, or acknowledgement message 

### v1.1.0-preview7 - 2022-12-28
* Update the library version to v4.0.0-preview16

### v1.1.0-preview6 - 2022-12-02
* Created Swagger GitHub Page and Auto Swagger API Documentation
* Improve batch upload response
* Add response codes to swagger and README

### v1.1.0-preview5 - 2022-09-30
* Validate the jurisdiction id in GET and POST requests
* Add Alias message types to the list of MOR message types
* Add deployment instruction documentation
* Add documentation for the bulk upload feature
* Add a capability statement endpoint to the API
* Update the library version to v4.0.0-preview11
* Implement pagination for GET requests

### v1.1.0-preview4 - 2022-07-25
* Update library to v4.0.0-preview6 

### v1.1.0-preview3 - 2022-07-14
* Added logging in the Bundles Controller for debugging
* Made use of HTTP 400/500 responses more accurate
* Implemented bulk processing to handle receiving multiple FHIR messages sent in one request
* Configured logs to write to file for improved debugging

### v1.1.0-preview1 - 2022-06-22
* Add endpoints for STEVE POST and GET requests
* Added a new column to the DB to track message retrieval through the STEVE channel
* Track the source of incoming messages (STEVE vs SAMS) in the database

### v1.0.0-preview1 - 2022-06-01
* Add a retrievedAt column to the db and only return messages that have not been retrieved yet
* Update the API to use the vrdr-dotnet v4.0.0-preview4 version, aligns with IG v1.3 
* Update tests to permit empty messages for testing purposes

### v0.1.0-alpha - 2022-06-01
* Fix time zone bug by updating database to store all timestamps in UTC 
* Change the `lastUpdated` paramater to `_since` to align with proper FHIR conventions

### v0.0.1-alpha - 2022-03-11

* Initial release used in the January 2022 testing event
