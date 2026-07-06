<div align="center">

# WMS Pro â€” Warehouse Management System

![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Clean_Architecture-6C63FF?style=for-the-badge)
![DDD](https://img.shields.io/badge/Domain_Driven_Design-6C63FF?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![CI](https://github.com/mohamed68909/Warehouse-Management-System/actions/workflows/ci.yml/badge.svg)

A professional enterprise-grade Warehouse Management System built with **Clean Architecture**, **Domain-Driven Design (DDD)**, and **ASP.NET Core MVC + REST API**.

</div>

---

## Overview

WMS Pro is a full-featured warehouse management solution designed for real-world business operations. It follows industry-standard architectural patterns to ensure scalability, maintainability, and testability.

## Architecture

```
Warehouse-Management-System/
â”œâ”€â”€ Domain/              # Entities, Value Objects, Domain Events (DDD)
â”œâ”€â”€ Application/         # Use Cases, DTOs, Interfaces (Clean Architecture)
â”œâ”€â”€ Infrastructure/      # EF Core, SQL Server, Repositories
â””â”€â”€ Presentation/        # ASP.NET Core MVC + REST API Controllers
```

### Design Principles
- **Clean Architecture** â€” Separation of concerns across layers
- **Domain-Driven Design (DDD)** â€” Rich domain model with aggregates
- **Repository Pattern** â€” Abstracted data access layer
- **SOLID Principles** â€” Throughout the codebase

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 |
| Database | SQL Server |
| Architecture | Clean Architecture + DDD |
| API Style | RESTful API + MVC |
| Language | C# 12 |

---

## Key Features

- **Inventory Management** â€” Track stock levels, movements, and locations
- **Warehouse Operations** â€” Inbound, outbound, and transfer management
- **Reporting** â€” Real-time inventory reports and analytics
- **REST API** â€” Full API for integration with external systems
- **MVC Interface** â€” Web-based management dashboard
- **Audit Logging** â€” Track all changes with timestamps

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server 2019+
- Visual Studio 2022 or VS Code

### Installation

```bash
# 1. Clone the repository
git clone https://github.com/mohamed68909/Warehouse-Management-System.git
cd Warehouse-Management-System

# 2. Restore dependencies
dotnet restore

# 3. Update database connection string in appsettings.json
# ConnectionStrings:Default -> your SQL Server connection

# 4. Apply database migrations
dotnet ef database update

# 5. Run the application
dotnet run
```

### Configuration

Create `appsettings.Development.json` (excluded from git):
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=WarehouseDB;Trusted_Connection=true;"
  }
}
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/products | Get all products |
| GET | /api/products/{id} | Get product by ID |
| POST | /api/products | Create new product |
| PUT | /api/products/{id} | Update product |
| DELETE | /api/products/{id} | Delete product |
| GET | /api/inventory | Get inventory status |
| POST | /api/inventory/transfer | Transfer stock |
| GET | /api/reports/stock | Stock level report |

---

## CI/CD

This project uses **GitHub Actions** for continuous integration:
- Automatic build on every push to `main` and `develop`
- Unit test execution
- Security vulnerability scan for NuGet packages

---

## License

This project is licensed under the **MIT License** â€” see the [LICENSE](LICENSE) file for details.

---

<div align="center">
  Made with passion by <a href="https://github.com/mohamed68909">Mohamed Ashraf</a>
</div>