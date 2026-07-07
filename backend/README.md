# Hotel & Service Management System - Backend

This is the initial backend project structure for the **Hotel and Service Management System** Web API, developed using **ASP.NET Core Web API** and **.NET 10**.

## Project Architecture

The solution follows a Clean Layered Architecture with 4 distinct projects:

1. **HotelServiceManagement.Domain**: Contains all domain entities, enums, and common entities (no external dependencies).
2. **HotelServiceManagement.Application**: Contains DTOs, service interfaces, validation logic, and application services.
3. **HotelServiceManagement.Infrastructure**: Implements database context (`HotelDbContext`), Fluent API configurations, security/password hashing services, identity services (JWT token generator), and data seed configurations.
4. **HotelServiceManagement.Api**: The entry point Web API that handles routing, controller requests, JWT authorization, and Dependency Injection registration.

The data access flow is designed to be simple and direct:
`Controller` ➔ `Service` ➔ `DbContext` ➔ `SQL Server`

---

## Prerequisites

- **.NET 10 SDK** (v10.0.x or higher)
- **SQL Server LocalDB** (installed by default with Visual Studio)
- **EF Core CLI Tools** (install globally using: `dotnet tool install --global dotnet-ef`)

---

## Connection String

The default connection string is configured in [appsettings.json](file:///d:/Project_PRN212/HotelServiceManagement/backend/HotelServiceManagement.Api/appsettings.json) to use SQL Server LocalDB:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\\\mssqllocaldb;Database=HotelServiceManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

---

## Database Migrations

Use the following commands from the `backend/` directory to create and apply database migrations.

### 1. Create Initial Migration
Run the following command to generate the migration files:

```bash
dotnet ef migrations add InitialCreate --project HotelServiceManagement.Infrastructure --startup-project HotelServiceManagement.Api --output-dir Migrations
```

### 2. Apply Migration (Create/Update Database)
Run the following command to apply the migration and create the SQL Server database with initial seed data:

```bash
dotnet ef database update --project HotelServiceManagement.Infrastructure --startup-project HotelServiceManagement.Api
```

---

## Run and Test Backend

### Run via .NET CLI
To run the Web API, execute this command from the `backend/` directory:

```bash
dotnet run --project HotelServiceManagement.Api
```

Once running, you can access the interactive Swagger UI documentation at:
`https://localhost:7198/swagger/index.html` (port may vary, check console output).

---

## Seed Data Included
The database is pre-configured to seed the following values:

1. **Roles**: `Admin`, `Manager`, `Receptionist`, `ServiceStaff`.
2. **Users** (Passwords are hashed using `BCrypt`):
   - `admin@hotel.com` (Password: `Admin123!`)
   - `manager@hotel.com` (Password: `Manager123!`)
   - `receptionist@hotel.com` (Password: `Receptionist123!`)
   - `service@hotel.com` (Password: `Service123!`)
3. **Room Types**: `Standard`, `Deluxe`, `Suite`, `Family Room`.
4. **Service Categories**: `Restaurant`, `Laundry`.
5. **Service Items**:
   - `Breakfast Set` ($15.00)
   - `Dinner Set` ($25.00)
   - `Bottled Water` ($2.00)
   - `Shirt Washing` ($5.00)
   - `Pants Washing` ($5.00)
   - `Ironing Service` ($3.00)
