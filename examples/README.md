This folder contains example submissions for testing submitting and retrieving messages from the FHIR API.

# Postman Collections
Postman is an API platform for building and using APIs. It is a useful tool for testing your ability to authenticate to the API server and make POST and GET requests. Postman can be downloaded [here](https://www.postman.com/). 

To use the examples in the `postman_collection` folder follow these steps.
1. Download the collection and import it into the Postman tool
2. Before you can make requests to the API, you will need an authentication token. Update the parameters in the get token request to retrieve your authentication token. If you don't have a client_id, client_secret, username, or password for SAMS contact nvssmodernization@cdc.gov
and for STEVE contact support@steve2.org

3. For all other GET and POST requests in the collection, update the URL to use your jurisdiction ID (the default is MI)
5. If you are sending a POST, update the jurisdiction in the body to use your jurisdiction ID (the default is MI), if you don't complete this step you will get an extraction error for mismatched jurisdiction ids.
6. Update the token under the Authorization tab to the token you retrieved from the auth server.  
7. Save your changes in Postman
