# Prod Update Instructions for v1.7.0
MITRE's contract ends on July 31, 2025. At that time:
  - The PROD API is on v1.6.1
  - The TEST API is on v1.6.1
  - The DEV API is on v1.7.0

This md goes over the update process for when TEST (and eventually PROD) updates to v1.7.0. The v1.7.0 changes include:
  - changes to support multiple IGs in the future
  - new endpoint urls for Birth, Fetal Death, and VRDR STU 3.0 support
  - backwards compatibility for VRDR STU 2.2 
  - stored procedures for all database interactions
  - a privacy policy endpoint

Since v1.7.0 updates the urls, this folder also includes an updated draft Birth and Fetal Death Test Plans for when v1.7.0 is used at an NCHS Hosted Test Event.

# Instructions
When NCHS updates TEST to v1.7.0 there are three database updates that need to occur in this order. The same steps should be followed when v1.7.0 is deployed to PROD.
1. Work with OCIO to execute the SQL script on the test DB to add the new IGVersion column to the IncomingMessageItems and OutgoingMessageItems tables.
2. Work with OCIO to execute the SQL script on the test DB to add the new stored procedures. 
3. Work with OCIO to add the DB service account configured in the application pool to a DB role with EXECUTE permissions. This will allow the API application to execute the stored procedures. This should already be configured in TEST, but will need to be configured for the PROD db account.

The two SQL scripts for steps 1 and 2 can be found in this folder. They have already been applied and tested on the DEV instance of the API. Note that the SQL script for the stored procedures complains about there being no COMMIT to end the transaction, but the script is valid and successfully creates the procedures. You can verify the stored procedures were created by inspecting the DB stored procedure folder.

When TEST and PROD are updated to v1.7.0, the appsetting config files in also need to be updated to include new settings.
1. In the test or production NVSS API messaging folder, open the appsettings.json file
  a. In TEST, assuming that only BFDR 2.0 and VRDR 2.2 are used, update the AppSettings section so that Birth and Fetal Death are turned ON and VRDR_STU2_2 is the only supported IG Version. It should look like the following:
```
    "AckAndIJEConversion": false,
    "BirthEnabled" : false,
    "FetalDeathEnabled": false,
    "SAMS" : <leave as is>,
    "STEVE" : <leave as is>,
    "PageCount" : <leave as is>,
    "MaxPayloadSize" : <leave as is>,
    "SupportedBFDRIGVersions": [
      "BFDR_STU2_0"
    ],
    "SupportedVRDRIGVersions": [
      "VRDR_STU2_2"
    ],
```
  b. In PROD, assuming only VRDR 2.2 is used, update the AppSettings section so that Birth and Fetal Death are turned OFF and VRDR_STU2_2 is the only supported IG Version. It should look like the following:
```
    "AckAndIJEConversion": false,
    "BirthEnabled" : false,
    "FetalDeathEnabled": false,
    "SAMS" : <leave as is>,
    "STEVE" : <leave as is>,
    "PageCount" : <leave as is>,
    "MaxPayloadSize" : <leave as is>,
    "SupportedBFDRIGVersions": [
    ],
    "SupportedVRDRIGVersions": [
      "VRDR_STU2_2"
    ],
```
Once the appsettings files are updated, restart the TEST or PROD API server. 

Note that if things break and you need to roll back to v1.6.1, you may need to remove the IGVersion column from the IncomingMessageItems and OutgoingMessageItems table to prevent errors. The stored procedures can be removed if you wish, but should not impact v1.6.1 from running.   