# LarvaX - Dengue Sentinel Platform

**LarvaX** is a full-stack epidemiological surveillance and healthcare management platform designed for rapid vector-borne disease outbreak response (specifically Dengue, Chikungunya, Malaria, and Zika). 

The platform connects citizens, healthcare providers, laboratory personnel, and government authorities in real time to detect hazard clusters early, guide patient clinical care, and coordinate medical resources.

---

## 🏗 Architecture & Solution Structure

The project is built on **.NET 10** following **Clean / Onion Architecture** principles:

```
Project-Larvax/
├── LarvaX.Core/           # Domain Entities, Enums, and Core Abstractions
├── LarvaX.Application/    # Business Logic, Decision Support, Services & Interfaces
├── LarvaX.Infrastructure/ # EF Core DbContext, PostgreSQL Migrations, QuestPDF, External Services
├── LarvaX.Web/            # ASP.NET Core MVC Presentation, SignalR Hubs, Hangfire Jobs
└── LarvaX.Tests/          # xUnit Test Suite (Unit tests with In-Memory EF Core)
```

---

## 💻 Tech Stack

* **Framework & Runtime**: .NET 10 (`net10.0`), C# 13, ASP.NET Core MVC
* **Database & ORM**: PostgreSQL 15+ / Supabase PostgreSQL, Npgsql Entity Framework Core Provider 10.0
* **Authentication & Authorization**: ASP.NET Core Identity with Role-Based Access Control
* **Real-time Communication**: ASP.NET Core SignalR + WebRTC signaling
* **Scheduled Jobs**: Hangfire (with PostgreSQL storage via `Hangfire.PostgreSql`)
* **Document Generation**: QuestPDF
* **Containerization**: Multi-stage Dockerfile (targets `mcr.microsoft.com/dotnet/aspnet:10.0`)
* **Frontend**: Bootstrap 5, Bootstrap Icons, Leaflet.js
* **Testing**: xUnit, EF Core In-Memory Database

---

## 📋 Prerequisites

Before setting up the project locally, ensure the following software is installed on your machine:

| Software / Tool | Required Version | Purpose / Notes |
|---|---|---|
| **[.NET SDK](https://dotnet.microsoft.com/download)** | **.NET 10.0 SDK** | Builds and runs all projects (`net10.0`). |
| **PostgreSQL** *(or [Supabase](https://supabase.com))* | v15 or later | Relational database. You can use a free cloud PostgreSQL instance on Supabase or run a local PostgreSQL service. |
| **[Git](https://git-scm.com/)** | Latest | Version control. |
| **IDE / Code Editor** *(Any)* | | **Visual Studio 2026 / 2022**, **Visual Studio Code** (with C# Dev Kit), or **JetBrains Rider**. |
| **Database GUI** *(Optional)* | | **Supabase Table Editor** (web), **[pgAdmin 4](https://www.pgadmin.org/)**, or **[DBeaver](https://dbeaver.io/)**. |

---

## 🚀 First-Time Machine Setup

Open your terminal (PowerShell, Command Prompt, or IDE terminal) in the repository root directory:

### 1. Restore Local .NET Tools
The repository includes a local tool manifest (`dotnet-tools.json`). Restore it locally without requiring global installation:
```powershell
dotnet tool restore
```

### 2. Restore Dependencies & Build
```powershell
dotnet restore
dotnet build
```

### 3. Configure Database Connection
Configure your PostgreSQL connection string for local development without committing sensitive passwords to Git.

**Option A (Recommended): Use .NET User Secrets**
```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<YOUR_HOST>;Port=5432;Database=postgres;Username=postgres;Password=<YOUR_PASSWORD>" --project LarvaX.Web
```

**Option B: Local PostgreSQL in `appsettings.Development.json`**
Edit `LarvaX.Web/appsettings.Development.json` (or use local PostgreSQL defaults):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=larvax;Username=postgres;Password=postgres"
  }
}
```

### 4. Apply Database Migrations
Create and configure the database schema by applying the clean PostgreSQL migration:
```powershell
dotnet ef database update --project LarvaX.Infrastructure --startup-project LarvaX.Web
```
*(Note: The application is also configured to automatically apply pending migrations on startup).*

---

## 🏃 Running the Application Locally

### Start the Web Server
```powershell
dotnet run --project LarvaX.Web
```

### Start with Hot Reload (Recommended for Development)
```powershell
dotnet watch --project LarvaX.Web
```

### Application URLs
Once started, navigate to:
* **HTTPS**: [https://localhost:7017](https://localhost:7017)
* **HTTP**: [http://localhost:5079](http://localhost:5079)

---

## 🔑 Default Administrator Credentials

On first run, database roles and a default administrator account are automatically seeded via `RoleSeeder.cs`:

* **Email**: `admin@larvax.gov.bd`
* **Password**: `Admin@123456`
* **Role**: `Administrator`

*Available Roles*: `Citizen`, `Doctor`, `HealthWorker`, `LabStaff`, `Administrator`, `GovernmentAuthority`.

---

## 🧪 Running Automated Tests

Run the complete xUnit test suite across the solution:
```powershell
dotnet test
```