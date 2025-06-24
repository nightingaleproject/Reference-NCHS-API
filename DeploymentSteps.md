# NVSS FHIR API Deployment Steps
These instructions walk through the steps of installing the FHIR API on a Windows Server. The setup includes intalling a SQL server, running the application using IIS, setting up a domain name, and certificates.

[Initial Install Instructions](#initial-install-on-windows-server)  
[Update Instructions](#update-deployment-steps)  
[Update Identity Credentials](#updating-the-identity-credentials)  
[Log Viewing](#log-viewing)  
[Potential Issues](#potential-issues)  
[IIS Resources](#iis-resources)  


## Initial Install On Windows Server 

1. Setting up Windows Server
   1. Install Internet Information Services (IIS) on the Windows server
   2. Install the .NET module for IIS https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-6.0 
   3. Make sure you have a way to access the Windows filesystem from your local machine
2. Build the project
   1. On your local machine, clone the git repo https://github.com/nightingaleproject/Reference-NCHS-API
   2. Checkout the tagged version you want to install
   3. From the project root directory, run `dotnet publish —configuration release` on your local machine
   4. Copy all files in messaging > bin > release > net6.0
   5. Paste the files to a folder on the new Windows Server
   6. Create and update all three appsettings and web.config files in the project folder on the new server
      1. appsettings (update the database server name)
   ```
   {
      "Logging": {
         "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Hosting.Lifetime": "Information",
            "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware": "Information"
         }
      },
      "ConnectionStrings": {
         "NVSSMessagingDatabase": "Server=<db-server-name-here>.cdc.gov;Database=nvssmessaging;Integrated Security=SSPI;"
      }
   }
   ``` 
      2. web.config
   ```
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <location path="." inheritInChildApplications="false">
       <system.webServer>
         <handlers>
           <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
         </handlers>
         <aspNetCore processPath="dotnet" arguments=".\messaging.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
           <environmentVariables>
            <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="<environment>" />
           </environmentVariables>
       </system.webServer>
     </location>
   </configuration>      
   ```
3. Install certificates
   1. If you plan to run multiple apps on the same machine (ex. Test and Dev) request DNS names for each
   2. Request an external certificate
      1. Make sure the new cert is installed on the machine
4. Create the IIS application
   1. Configure each new site in IIS https://docs.microsoft.com/en-us/aspnet/core/tutorials/publish-to-iis?view=aspnetcore-6.0&tabs=visual-studio 
   2. Point the app to the folder created in Step 2 that holds all the release files on the server 
   3. Configure HTTPS and point to the cert installed in step 3
   4. Do not start the server until the SQL database is up and running
5. Set up SQL server
   1. Create a new sql server
   2. On your local machine in the project directory, run  `dotnet ef --project messaging migrations script` to get the full sql script including all migrations
   3. Execute the sql script on the new sql server to initialize the db 
6. Application Authentication
   1. In the appsettings.json file, ensure you have `Integrated Security=SSPI;`
   2. In IIS, set the Site authentication to `Anonymous Authentication=Enabled` and `ASP.NET Impersonation=Disabled`
   3. Create a service account with Read/Write permissions for the database, ex `Domain\NVSSMsgAPI_SvcAcct`
   4. Set up impersonation at the Application Identity Pool level 
      1. Go to the pool's advanced settings
      2. Click the Identity field
      3. Select custom identity
      4. Enter the credentials for the account from step 1 into the form
   5. Add the account from step 1 to the `batch logon` permissions group
   6. Add the account from step 1 to the `IIS_IUSRS` group
   7. Add the account from step 1 to the `Log on as a service` group
   8. Give the account from step 8.3 permissions to access the application folder
   9. Set up HSTS 
7. Run the IIS application
   1. Once the app is configured and the SQL database is running, go to the IIS application and hit "start" in the right side menu
8. Testing
   1. Once the API is up and running, test posting a request to the endpoint
      1. `curl -X POST https://<server-host-name>/MA/Bundle -- header "Content-Type: application/json" --data "@path\to\test\MessageSubmission.json"` 

## Update Deployment Steps

1. In gitbash, or your preferred command line tool, navigate to the Reference-NCHS-API root directory
2. Use `git fetch` to pull the latest code from https://github.com/nightingaleproject/Reference-NCHS-API 
3. Use `git checkout <vx.x.x>` to checkout the newest tagged verion on your local machine, see the list of tags [here](https://github.com/nightingaleproject/Reference-NCHS-API/tags)
4. If there are migrations to apply for this verions, run `dotnet ef migrations list` to get a list of past migrations. Otherwise go to step 5.
   1. If there are new migrations to apply for this version run `dotnet ef --project messaging migrations script <name-of-last-applied-migration>` to get the SQL script for all migrations after the last applied migration
5. From the project root directory, run `dotnet publish —-configuration release` on your local machine.
   1. If you are deploying a debug version, run `dotnet publish --configuration debug` instead. This will enable the debug logging.
6. Copy the all files in `messaging > bin > release > net6.0`
   1. If you are deploying a debug version, copy all the files in `messaging > bin > debug > net6.0 instead`
7. Use your file explorer to connect to the remote server's filesystem (`\\ASTV-NVSS-API` for test and dev, `\\ASPV-NVSS-API` for prod) and paste all the files from step 6 to a new folder under `Users > Public`. Name the new folder using the version number of the software, ex. `NVSS-FHIR-API-v1.2.0`. The file transfer will take a few mintues.
8. Connect to the server via rdp at https://pvwach.cdc.gov
9. On the remote server, open the file explorer and copy the folder created in step 7. Paste the contents into a new folder under `D:/WebApps/`. If you are deploying dev and test, you will need to create two new folders, `D:/WebApps/NVSS-API-TEST-vx.x.x` and `D:/WebApps/NVSS-API-DEV-vx.x.x` and paste the contents into both folders.
10. Copy all three `appsettings.json`, `appsettings.Test.json`, and `appsettings.Development.json` files and the `web.config` file from the `NVSS-API-TEST` folder and paste them in the new `NVSS-API-TEST-vx.x.x` folder. Do the same for `NVSS-API-DEV` and `NVSS-API-DEV-vx.x.x`. The config files are different for dev, test, and prod so make sure you copy from the correct folder.
11. If you are applying migrations, complete the following steps. Otherwise skip to step 12.
    1. From your local machine, connect to the dev server database in visual studio code using server explorer
       1. Select Microsoft SQL Server
       2. server name: `dstv-infc-1900.cdc.gov`, `dsdv-infc-1900.cdc.gov`
       3. database name: `NVSSMESSAGING`
       4. Paste the SQL code from step 4.1 in a new query and run to apply the migration
12. Before you update the API, double check what the current version is by navigating to the metadata endpoint, ex. for test is `https://test.astv-nvss-api.cdc.gov/MA/metadata`. The API version will be specified in the json response.
13. On the remote server, run the IIS application (Internet Information Services)
    1.  Select ASTV-NVSS-API
    2.  Expand sites
    3.  Click on application you are updating, ex. `NVSS-FHIR-API-TEST` and on the right hand side under Manage Website select "Stop" to stop the API.
14. In the remote desktop, open the file explorer and navigate to `D:/WebApps/`
    1.  Update the folders in the D drive so IIS will pickup the latest version
        1.  Rename `NVSS-API-TEST` to `NVSS-API-TEST-vx.x.x` where `vx.x.x` matches the version from step 12.
        2.  Rename the new version `NVSS-API-TEST-vx.x.x` to `NVSS-API-TEST`
        3.  Optional, clean up any old API folder versions in `D:/WebApps/` that are no longer needed.
15. In the remote desktop, go to IIS and select "Start" to restart the API.
16. Test the API is up and running
    1.  Check the metadata endpoint, ex. for test it is `https://test.astv-nvss-api.cdc.gov/MA/metadata`, to confirm the API is up and running.
    2. FOR DEV AND TEST ONLY, execute POST and GET test messages using either curl or Postmann to confirm the API is working as expected.

## Update Identity Credentials
1. RDP into the server, dev and test are on ASTV-NVSS-API.cdc.gov
2. Open Internet Information Services
3. Go to the Application pool and click advanced settings
4. Click Identity and enter the new credentials in the Custom Identity field

## Log viewing
To write logs to the log folder on the server:
1. uncomment the logging line at the bottom of StartUp.cs
2. uncomment the package in messaging.csproj to enable logging
3. rebuild the release and deploy the code

## Potential issues
Might get error for dotnet 6 extension when trying to access the server with the new version update. To resolve the issue, install the extension on the pbi server.


## IIS resources
1. Install .NET Core Hosting Bundle Installer to add dotnet support to IIS https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-6.0 
2. Configure the site in IIS https://docs.microsoft.com/en-us/aspnet/core/tutorials/publish-to-iis?view=aspnetcore-6.0&tabs=visual-studio 


# Status UI Deployment Steps

The Status UI is a separate .NET web API backend and ReactJS frontend that may be deployed alongside the
NVSS FHIR API for messaging. The Status UI and NVSS FHIR API must connect to the same Microsoft SQL Server
database, and should be deployed as separate entities on the IIS with separate FQDNs and separate access
controls. The Status UI can only be deployed if the NVSS FHIR API is already deployed.

To deploy the Status UI, follow the [NVSS FHIR API Deployment Steps](#NVSS-FHIR-API-Deployment-Steps),
except replace steps 2 (Build), 5 (Setup the SQL Server), 7 (Test), and 8 (SQL Authentication) with their
counterparts below:

2. Build the project

   1. Ensure you have a clone the git repo https://github.com/nightingaleproject/Reference-NCHS-API on your local machine
   2. Checkout the tagged version you want to install
   3. From the `status_ui/` directory, run `npm ci && npm run build` on your local machine
   3. From the `status_api/` directory, run `dotnet publish —configuration release` on your local machine
   4. Copy all files in status_api > bin > release > net6.0
   5. Paste the files to a **separate** folder on Windows Server
   6. Create and update all three appsettings and web.config files in the **separate** project folder on Windows Server
      1. appsettings (update the database server name)
   ```
   {
      "Logging": {
         "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Hosting.Lifetime": "Information",
            "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware": "Information"
         }
      },
      "ConnectionStrings": {
         "NVSSMessagingDatabase": "Server=<db-server-name-here>.cdc.gov;Database=nvssmessaging;Integrated Security=SSPI;"
      }
   }
   ```

**The database ConnectionString is intentionally the same between the Status UI and NVSS FHIR API.**

      2. web.config
   ```
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <location path="." inheritInChildApplications="false">
       <system.webServer>
         <handlers>
           <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
         </handlers>
         <aspNetCore processPath="dotnet" arguments=".\status_api.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
           <environmentVariables>
            <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="<environment>" />
            <environmentVariable name="DOTNET_ENVIRONMENT" value="<environment>" />
           </environmentVariables>
       </system.webServer>
     </location>
   </configuration>
   ```

Replace `<environment>` with `Development`, `Test`, or `Production` depending on your use case. **Never deploy `Development` or `Test` on an open network.**

5. Setup the SQL Server
   1. There are no setup steps for the Status UI. The migrations and schema are all defined by the messaging project.
6. Application Authentication
   1. In IIS, set the Site authentication to `Anonymous Authentication=Enabled` and `ASP.NET Impersonation=Disabled`
   2. Create a service account with Read/Write permissions for the database, ex `Domain\StatusUI_SvcAcct`
   3. Set up impersonation at the Application Identity Pool level
      1. Go to the pool's advanced settings
      2. Click the Identity field
      3. Select custom identity
      4. Enter the credentials for the account from step 1 into the form
   4. Add the account from step 1 to the `IIS_IUSRS` group
   5. Add the account from step 1 to the `Log on as a service` group
   6. Give the account from step 8.3 permissions to access the application folder
   7. Set up HSTS
8. Testing the app
   1. Ensure there is already FHIR messaging data on the SQL Server, use the NVSS FHIR API to post messages if there is no data.
   2. Navigate to `<fqdn>/StatusUI/index.html`, where `<fqdn>` is the full domain where the application was deployed.

