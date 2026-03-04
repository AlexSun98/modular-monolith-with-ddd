# Modular Monolith with DDD - Template

This solution was created from the **Modular Monolith with DDD** template.

## 🚀 Quick Start

### 1. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or SQL Server in Docker)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (optional, for containerized SQL Server)

### 2. Database Setup

#### Option A: Docker (Recommended)
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password1" \
  -p 1433:1433 --name sql-server -d mcr.microsoft.com/mssql/server:2022-latest
```

#### Option B: Local SQL Server
Update connection strings in `appsettings.json` files.

### 3. Run Database Migrations

```bash
cd src/Database/DatabaseMigrator
dotnet run
```

Or use the build script:
```bash
./build.ps1 MigrateDatabase
```

### 4. Run the Application

```bash
cd src/API/CompanyName.MyMeetings.API
dotnet run
```

### 5. Access the API

- Swagger UI: `https://localhost:5001/swagger`
- API Base URL: `https://localhost:5001/api`

---

## 📁 Solution Structure

```
src/
├── API/                          # ASP.NET Core Web API
├── BuildingBlocks/               # Shared infrastructure & domain primitives
│   ├── Application/              # CQRS base classes, decorators
│   ├── Domain/                   # Entity, ValueObject, DomainEvent bases
│   └── Infrastructure/           # EF Core, Dapper, EventBus, Email
├── Modules/
│   ├── Administration/           # Admin module (user management)
│   ├── Meetings/                 # Sample business module
│   ├── Payments/                 # Event-sourced payments module
│   ├── Registrations/            # User registration module
│   └── UserAccess/               # Authentication & authorization
├── Database/
│   ├── CompanyName.MyMeetings.Database/   # SSDT project (schema)
│   └── DatabaseMigrator/                   # DbUp migrations
└── Tests/
    ├── ArchTests/                # Architecture tests
    ├── IntegrationTests/         # Cross-module integration tests
    └── SUT/                      # System Under Test helpers
```

---

## 🔧 Post-Creation Customization

### Rename Namespaces (if not done via template parameters)

If you need to rename namespaces after creation:

```powershell
# PowerShell - Replace namespaces in all files
$oldName = "CompanyName.MyMeetings"
$newName = "YourCompany.YourApp"

Get-ChildItem -Recurse -Include *.cs,*.csproj,*.sln,*.json,*.props,*.targets |
  ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match $oldName) {
      $content -replace $oldName, $newName | Set-Content $_.FullName -NoNewline
      Write-Host "Updated: $($_.FullName)"
    }
  }

# Rename files and directories
Get-ChildItem -Recurse -Directory | Where-Object { $_.Name -match $oldName } |
  Rename-Item -NewName { $_.Name -replace $oldName, $newName }

Get-ChildItem -Recurse -File | Where-Object { $_.Name -match $oldName } |
  Rename-Item -NewName { $_.Name -replace $oldName, $newName }
```

---

## 📚 Key Patterns & Guides

| Pattern | Location | Description |
|---------|----------|-------------|
| **CQRS** | `BuildingBlocks/Application/` | Command/Query separation with MediatR |
| **DDD Aggregates** | `Modules/*/Domain/` | Rich domain models with business rules |
| **Integration Events** | `Modules/*/IntegrationEvents/` | Cross-module async communication |
| **Outbox Pattern** | `BuildingBlocks/Infrastructure/` | Reliable event publishing |
| **Event Sourcing** | `Modules/Payments/` | Event-sourced aggregate example |

### Detailed Documentation

See `docs/copilot-instructions/` for:
- `01-NEW-FEATURE-GUIDE.md` - Adding new features
- `02-NEW-MODULE-GUIDE.md` - Creating new modules
- `03-TESTING-GUIDELINES.md` - Testing patterns
- `04-DATABASE-CHANGES.md` - Migration strategies
- `07-DOMAIN-MODEL-GUIDE.md` - DDD tactical patterns

---

## 🧪 Running Tests

```bash
# All tests
dotnet test

# Specific test category
dotnet test --filter "Category=UnitTests"
dotnet test --filter "Category=IntegrationTests"
dotnet test --filter "Category=ArchTests"

# With coverage
dotnet-coverage collect -f cobertura -o coverage.xml dotnet test
```

---

## 🔨 Build Scripts

```bash
# Full build
./build.ps1 Compile

# Run integration tests
./build.ps1 IntegrationTests

# Deploy database
./build.ps1 MigrateDatabase

# Create SUT (System Under Test) data
./build.ps1 CreateSUT
```

---

## ➕ Adding a New Module

1. Create the module structure:
```
Modules/YourModule/
├── Application/
├── Domain/
├── Infrastructure/
└── IntegrationEvents/
```

2. Follow the guide in `docs/copilot-instructions/02-NEW-MODULE-GUIDE.md`

3. Register the module in `API/Startup.cs`

4. Add database schema in `Database/CompanyName.MyMeetings.Database/`

---

## 📝 License

[Your License Here]

---

## 🤝 Contributing

[Your Contributing Guidelines Here]
