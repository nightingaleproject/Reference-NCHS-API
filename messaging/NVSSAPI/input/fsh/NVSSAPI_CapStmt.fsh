Instance: NVSS-API-CS
InstanceOf: CapabilityStatement
Usage: #definition
* version = "v1.1.0-preview5"
* name = "NVSS_API"
* title = "NVSS API Server Capability Statement"
* status = #draft
* date = "2022-09-15"
* publisher = "NCHS"
* kind = #instance
* implementation.description = "NVSS API Server"
* implementation.url = "https://apigw.cdc.gov/OSELS/NCHS/NVSSFHIRAPI/XX/CapabilityStatement"
* fhirVersion = #4.0.1
* format = #json
* rest.mode = #server
* rest.resource[+].type = #Bundle
* rest.resource[=].interaction[0].code = #search-type
* rest.resource[=].searchParam[0].name = "_since"
* rest.resource[=].searchParam[=].type = #date
* rest.resource[=].interaction[+].code = #create