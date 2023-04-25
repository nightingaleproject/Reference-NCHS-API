# NVSS FHIR API Deployment Steps
These instructions walk through the steps of installing the FHIR API on a Windows Server. The setup includes intalling a SQL server, running the application using IIS, setting up a domain name, and certificates.

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
   6. Create and update the appsettings and web.config files in the project folder on the new server
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
6. Run the IIS application
   1. Once the app is configured and the SQL database is running, go to the IIS application and hit "start" in the right side menu
7. Testing
   1. Once the API is up and running, test posting a request to the endpoint
      1. `curl -X POST https://<server-host-name>/MA/Bundle -- header "Content-Type: application/json" --data "@path\to\test\MessageSubmission.json"` 
8. SQL Authentication
   1. In the appsettings.json file, add `Integrated Security=SSPI;`
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

## Update Deployment Steps:

1. Pull the latest code from https://github.com/nightingaleproject/Reference-NCHS-API 
2. Checkout the tagged verion you want to deploy
3. Run `dotnet ef migrations list` to get a list of past migrations
4. If there are new migrations to apply run `dotnet ef --project messaging migrations script <name-of-last-applied-migration>` to get the SQL script for all migrations after the last applied migration
5. Run `dotnet publish —configuration release` on your local machine
6. Copy the files in messaging > bin > release > net6.0
7. Connect to the remote server's filesystem (\\ASTV-NVSS-API for test and dev, \\ASPV-NVSS-API for prod)and copy the files to a folder
8. Copy appsettings and web.config from the `NVSS-API-TEST` folder and paste them in the new folder
9.  If you are applying migrations, connect to the dev server database in visual studio code using server explorer
   1. Select Microsoft SQL Server
   2. server name: dstv-infc-1900.cdc.gov, dsdv-infc-1900.cdc.gov
   3. database name: `NVSSMESSAGING`
   4. Paste the SQL code from step 2 in a new query and run to apply the migration
10. Sign into remote server
11. Run the IIS application, Internet Information Services
    1.  Select ASTV-NVSS-API
    2.  Expand sites
    3.  Click on NVSS-FHIR-API-TEST and on the right hand side under Manage Website select "Stop" to stop the server
12. In the remote desktop, open file explorer and navigate to the d drive
    1.  Once the copy from step 6 has complete, update the folders in the d drive
        1.  delete `NVSS-API-TEST-last-release`
        2.  change the `NVSS-API-TEST` to `NVSS-API-TEST-last-release`
        3.  change `NVSS-API-TEST-new` to `NVSS-API-TEST`

Updating the Identity Credentials
1. RDP into the server, dev and test are on ASTV-NVSS-API.cdc.gov
2. Open Internet Information Services
3. Go to the Application pool and click advanced settings
4. Click Identity and enter the new credentials in the Custom Identity field

Log viewing:
To write logs to the log folder on the server:
1. uncomment the logging line at the bottom of StartUp.cs
2. uncomment the package in messaging.csproj to enable logging
3. rebuild the release and deploy the code

Potential issues:
Might get error for dotnet 6 extension when trying to access the server with the new version update. To resolve the issue, install the extension on the pbi server.


IIS resources:
1. Install .NET Core Hosting Bundle Installer to add dotnet support to IIS https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-6.0 
2. Configure the site in IIS https://docs.microsoft.com/en-us/aspnet/core/tutorials/publish-to-iis?view=aspnetcore-6.0&tabs=visual-studio 
