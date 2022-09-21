This subdirectory is used for creating a capability statement for the reference NCHS API using sushi, which is a bit easier than maintaining the JSON directly.  The capability statement is pretty simple, so this may be overkill.

The capability statement is generated in fsh-generated/resources/CapabilityStatement-NVSS-API-CS.json
The input FHIR Shorthand file is in input/fsh/NVSSAPI_CapStmt.fsh

To generate the json from the fsh:
* install sushi:   npm install -g sushi
* run sushi:    sushi .

The capability statement has one quirk.  The implementation URL is essentially a template of what it should be with XX as the placeholder for the juridsiction:   https://apigw.cdc.gov/OSELS/NCHS/NVSSFHIRAPI/XX/CapabilityStatement