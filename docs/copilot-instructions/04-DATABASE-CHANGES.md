# Database Change Management

## 1. Overview
*   **Tool**: DbUp (for migrations) and SSDT (for schema versioning).
*   **Structure**: Database scripts are located in `src/Database/CompanyName.MyMeetings.Database/`.
*   **Schemas**: Each module has its own schema (e.g., `meetings`, `administration`, `payments`).

## 2. Creating a Migration
1.  Navigate to the appropriate module folder in `src/Database/CompanyName.MyMeetings.Database/[schema]/`.
2.  Create a new `.sql` file.
3.  Naming convention: `[Order]_[Description].sql` (e.g., `0001_CreateMeetingTable.sql`).
4.  Write the SQL script (CREATE TABLE, ALTER TABLE, etc.).
5.  Ensure the script is idempotent if possible (check for existence).

## 3. Applying Migrations
*   Run the `MigrateDatabase` task via the build script:
    ```shell
    .\build MigrateDatabase --DatabaseConnectionString "..."
    ```
*   Or run the application (if configured to migrate on startup - check `Program.cs` or `Startup.cs`).

## 4. SSDT Project
*   The `src/Database/CompanyName.MyMeetings.Database/` folder is also a Visual Studio Database Project.
*   Ensure the `.sql` files are included in the project file (`.sqlproj` or `.csproj` if using MSBuild.Sdk.SqlProj).

## 5. Best Practices
*   **Never** modify an existing migration script after it has been merged/deployed. Create a new script to fix or change it.
*   Keep scripts small and focused.
*   Use the correct schema for the module.
