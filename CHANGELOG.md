## Changelog

### v1.6.0 - 2025-04-08
* Add support for multiple IG Versions (BFDR v2.0, VRDR v2.2 & v3.0) at the new /vitalType/igVersion endpoint
* Add support for VRDR library v5.0.0 and VR Messaging Common Messages while maintaining backwards compatibilty

### v1.5.0 - 2025-03-21
* Add support for FetalDeath messaging under the /BFDR-FETALDEATH url
* Update BFDR to v1.0.0-preview.11 and VitalRecordMessaging to v1.0.0-preview.6
* Changed Birth Messaging url to /BFDR-BIRTH

### v1.4.1 - 2024-12-10
* Update VRDR to v4.4.2

### v1.4.0 - 2024-11-07
* Update BFDR to v1.0.0-preview.9 and VRDR to v4.4.1
* Add validation for maximum payload size
* Add required web config headers for security 
* Clean up logs when parsing bfdr and vrdr messages

### v1.3.0 - 2024-08-29
* Add support for Natality Messaging
* Add configuration to enable or disable Natality Messaging
* Update BFDR to v1.0.0-preview.7 and VRDR to v4.3.0
* Use message header validation from vrdr-dotnet library
* Add POST and GET route handling for vital record type and IG version number, ex. MA/Bundle/BFDR/v2.0
  * Validate the record aligns with the url vital record type on POST
  * Return record submissions based on record type on GET

### v1.2.2 - 2024-08-27
* Update library v4.2.2

### v1.2.1 - 2024-04-25
* Update library v4.1.9

### v1.2.0 - 2024-04-01
* Validate url jurisdiction and message header jurisdiction match
* Add feature to query by business ids; death year, certificate number, and jurisdiction id
* Update library v4.1.8

### v1.1.2 - 2024-01-26
* Fix Newtonsoft configuration to prevent deserializer from changing time zone

### v1.1.1 - 2023-11-07
* Make message header destination validation case insensitive

### v1.1.0-preview19 - 2023-08-15
* Update library to v4.1.4

### v1.1.0-preview18 - 2023-08-14
* Change double queue for STEVE and the backup endpoint to a single queue

### v1.1.0-preview17 - 2023-08-11
* Update library to v4.1.1

### v1.1.0-preview16 - 2023-07-14
* Update library to v4.0.9

### v1.1.0-preview15 - 2023-05-19
* Update library to v4.0.1

### v1.1.0-preview14 - 2023-05-04
* Readme updates
* Remove swagger for prod

### v1.1.0-preview13 - 2023-04-24
* Update swagger docs
* Validate certificate number length
* Validate NCHS is included in the message header destination
* Update library to v4.0.0-preview21

### v1.1.0-preview12 - 2023-03-28
* Add logging for caught bundle parsing errors
* Add error response messages on parameter validation

### v1.1.0-preview11 - 2023-03-10
* Catch null certificate numbers in the message header and return 400
* Added documentation for all validation checks at the API level

### v1.1.0-preview10 - 2023-02-22
* Update the library version to v4.0.0-preview19

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
