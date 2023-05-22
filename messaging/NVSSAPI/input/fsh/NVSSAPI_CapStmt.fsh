Instance: NVSS-API-CS
InstanceOf: CapabilityStatement
Usage: #definition
* version = "v1.1.0-preview15"
* name = "NVSS_API"
* title = "NVSS API Server Capability Statement"
* status = #draft
* date = "2023-02-22"
* publisher = "NCHS"
* kind = #instance
* implementation.description = "NVSS API Server"
* implementation.url = "https://apigw.cdc.gov/OSELS/NCHS/NVSSFHIRAPI/XX/metadata"
* fhirVersion = #4.0.1
* format = #json
* rest.mode = #server
* rest.resource[+].type = #Bundle
* rest.resource[=].interaction[0].code = #search-type
* rest.resource[=].searchParam[0].name = "_since"
* rest.resource[=].searchParam[=].type = #date
* rest.resource[=].interaction[+].code = #create
