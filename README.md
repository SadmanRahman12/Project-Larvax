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
├── LarvaX.Infrastructure/ # EF Core DbContext, Migrations, QuestPDF, External Services
├── LarvaX.Web/            # ASP.NET Core MVC Presentation, SignalR Hubs, Hangfire Jobs
└── LarvaX.Tests/          # xUnit Test Suite (Unit tests with In-Memory EF Core)
```

---

## 💻 Tech Stack

* **Framework & Runtime**: .NET 10 (`net10.0`), C# 13, ASP.NET Core MVC
* **Database & ORM**: Microsoft SQL Server (LocalDB / Express), Entity Framework Core 10.0
* **Authentication & Authorization**: ASP.NET Core Identity with Role-Based Access Control
* **Real-time Communication**: ASP.NET Core SignalR + WebRTC signaling
* **Scheduled Jobs**: Hangfire (hourly risk recalculation and alert dispatching)
* **Document Generation**: QuestPDF
* **Frontend**: Bootstrap 5, Bootstrap Icons, Leaflet.js
* **Testing**: xUnit, EF Core In-Memory Database

---

## 📋 Prerequisites

Before setting up the project locally, ensure the following software is installed on your machine:

| Software / Tool | Required Version | Purpose / Notes |
|---|---|---|
| **[.NET SDK](https://dotnet.microsoft.com/download)** | **.NET 10.0 SDK** | Builds and runs all projects (`net10.0`). |
| **Microsoft SQL Server LocalDB** | 2019 or later | Included automatically with Visual Studio (under *.NET desktop development* or *ASP.NET and web development* workload), or via [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads). |
| **[Git](https://git-scm.com/)** | Latest | Version control. |
| **IDE / Code Editor** *(Any)* | | **Visual Studio 2026 / 2022** (recommended on Windows), **Visual Studio Code** (with C# Dev Kit extension), or **JetBrains Rider**. |
| **[SSMS](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms)** *(Optional)* | v19 / v20 | Optional GUI tool for directly viewing and querying SQL Server tables. |

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

### 3. Apply Database Migrations
Create and configure the local SQL Server database (`LarvaXDb`) by applying existing migrations:
```powershell
dotnet ef database update --project LarvaX.Infrastructure --startup-project LarvaX.Web
```

---

## 🏃 Running the Application

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