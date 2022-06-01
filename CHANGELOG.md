## Changelog

### v1.0.0-preview1 - 2022-06-01
* Add a retrievedAt column to the db and only return messages that have not been retrieved yet
* Update the API to use the vrdr-dotnet v4.0.0-preview4 version, aligns with IG v1.3 
* Update tests to permit empty messages for testing purposes

### v0.1.0-alpha - 2022-06-01
* Fix time zone bug by updating database to store all timestamps in UTC 
* Change the `lastUpdated` paramater to `_since` to align with proper FHIR conventions

### v0.0.1-alpha - 2022-03-11

* Initial release used in the January 2022 testing event