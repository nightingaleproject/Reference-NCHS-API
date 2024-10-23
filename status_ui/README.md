# NVSS API Status UI

This subdirectory contains a simple single page Status UI, built with React, that displays information pulled from the NVSS API Status API.

For deployment simplicity the results of building the React app are just checked into source control as part of the API and the API serves those generated files.

If changes are needed to the UI, follow the following steps:

1. Make updates to the React code in the src subdirectory

2. Run `npm run build` to generate the static HTML, JS, and CSS files for the Status IP; these files will be placed in the StatusUI subdirectory of the API implementation (`../messaging/StatusUI`)

3. Check both the updates to the UI source code and the generated static files into source control; this may require a `git add` of `../messaging/StatusUI`

4. As needed, update the names of the generated files in the EmbeddedResource references in the messaging project file (`../messaging/messaging.csproj`)
