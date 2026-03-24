# Database Deployment Instructions

This folder contains the `DataProvisioning_Schema.sql` script representing the complete structural schema for the Data Provisioning Engine.

Depending on your organization's deployment pipelines and DBA requirements, you can build and apply this database using any of the following methods:

## Method 1: Entity Framework Core (Automated / Recommended)

If your development team prefers managing database changes natively through code (Code-First), use Entity Framework Core. EF will automatically connect to the server defined in your `appsettings.json` under `ConnectionStrings:DefaultConnection`.

1. Ensure the application's `appsettings.json` is correctly pointing to the target SQL Server.
2. Open a terminal in the root `DataProvisioning.Net` directory.
3. Run the following command to automatically generate the database and apply the schema:
   ```bash
   dotnet ef database update -p DataProvisioning.Infrastructure/DataProvisioning.Infrastructure.csproj -s DataProvisioning.WebUI/DataProvisioning.WebUI.csproj --context ApplicationDbContext
   ```

## Method 2: Manual Execution via SSMS (DBA Approach)

If your database administrators prefer to execute SQL queries directly:

1. Connect to the target database instance using **SQL Server Management Studio (SSMS)**.
2. Create an empty database (e.g., `datamarketplace`).
3. Open the `DataProvisioning_Schema.sql` file.
4. Copy its entire contents and execute it directly against the newly created database.

## Method 3: Visual Studio Database Project (.dacpac)

If your organization utilizes strict automated delivery using DACPAC files:

1. Open **Visual Studio 2022**.
2. Create a new **SQL Server Database Project**.
3. Move or drag the `DataProvisioning_Schema.sql` script into the project.
4. Right-click the Database Project and select **Build**.
5. Visual Studio will produce a compiled `.dacpac` file in the `bin/Debug` directory, ready to be deployed through Azure DevOps, Octopus Deploy, or any other formal pipeline.
