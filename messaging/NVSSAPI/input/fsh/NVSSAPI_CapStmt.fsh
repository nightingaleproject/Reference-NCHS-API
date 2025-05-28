Instance: NVSS-API-CS
InstanceOf: CapabilityStatement
Usage: #definition
* version = "1.6.1"
* name = "NVSS_API"
* title = "NVSS API Server Capability Statement"
* status = #draft
* date = "2025-05-14"
* publisher = "NCHS"
* kind = #instance
* implementation.description = "NVSS API Server"
* implementation.url = "https://apigw.cdc.gov/OSELS/NCHS/NVSSFHIRAPI/XX/metadata"
* fhirVersion = #4.0.1
* format = #json
* rest.mode = #server
* rest.resource[+]
  * type = #Bundle
  * interaction[+].code = #search-type
  * interaction[+].code = #create
  * searchParam[+]
    * name = "_since"
    * type = #date
  * searchParam[+]
    * name = "certificateNumber"
    * type = #string
  * searchParam[+]
    * name = "deathYear"
    * type = #string
