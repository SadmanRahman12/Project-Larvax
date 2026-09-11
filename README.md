# LarvaX - Dengue Sentinel Platform

LarvaX is a real-time epidemiological surveillance and healthcare management platform for vector-borne disease outbreaks (Dengue, Chikungunya, Malaria, Zika). It connects citizens, clinicians, laboratories, and health authorities to detect outbreak clusters early, streamline triage and care, and coordinate medical resources.

---

## 🏗 Architecture

Built on **.NET 10** using Clean Architecture:

```
Project-Larvax/
├── LarvaX.Core/           # Domain entities, enums, core abstractions
├── LarvaX.Application/    # Business logic, clinical triage, services
├── LarvaX.Infrastructure/ # EF Core, PostgreSQL migrations, QuestPDF, external APIs
├── LarvaX.Web/            # ASP.NET Core MVC, SignalR hubs, Hangfire jobs
└── LarvaX.Tests/          # xUnit test suite (In-Memory EF Core)
```

---

## 💻 Tech Stack

- **Framework & Runtime:** .NET 10 (`net10.0`), C# 13, ASP.NET Core MVC
- **Database:** PostgreSQL 18+ / Supabase PostgreSQL via EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Authentication:** ASP.NET Core Identity (Role-Based Access Control)
- **Real-time Communication:** SignalR, WebRTC signaling
- **Scheduled Jobs:** Hangfire with PostgreSQL storage
- **Document Generation:** QuestPDF
- **Frontend:** Bootstrap 5, Bootstrap Icons, Leaflet.js
- **Testing:** xUnit, EF Core In-Memory
- **Containerization:** Multi-stage Dockerfile (`aspnet:10.0`)

---

## 📋 Prerequisites

- **[.NET 10.0 SDK](https://dotnet.microsoft.com/download)**
- **[PostgreSQL 18+](https://www.postgresql.org/)** or **[Supabase](https://supabase.com/)**

---

## 🚀 Quickstart

### 1. Restore Tools & Build
```
dotnet tool restore
dotnet build
```

### 2. Configure Database
By default, `appsettings.json` points to local PostgreSQL:
```
Host=localhost;Port=5432;Database=larvax;Username=postgres;Password=postgres
```

**Use Supabase (Optional for Local Dev):**
```
# Set Supabase connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<HOST>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<REF>;Password=<PASSWORD>;SSL Mode=Require;Trust Server Certificate=true" --project LarvaX.Web

# Revert to local PostgreSQL
dotnet user-secrets remove "ConnectionStrings:DefaultConnection" --project LarvaX.Web
```
*(Production uses the `ConnectionStrings__DefaultConnection` environment variable).*

### 3. Apply Migrations
*(Pending migrations also apply automatically on startup).*
```
dotnet ef database update --project LarvaX.Infrastructure --startup-project LarvaX.Web
```

---

## 🏃 Running the Application Locally

```
# Standard run
dotnet run --project LarvaX.Web

# Or run with Hot Reload
dotnet watch --project LarvaX.Web
```

- **HTTPS:** [https://localhost:7017](https://localhost:7017)
- **HTTP:** [http://localhost:5079](http://localhost:5079)

---

## 🔑 Default Admin Account

Seeded on first startup via `RoleSeeder.cs`:

- **Email:** `admin@larvax.gov.bd`
- **Password:** `Admin@123456`
- **Role:** `Administrator`

**System Roles:** `Citizen`, `Doctor`, `HealthWorker`, `LabStaff`, `Administrator`, `GovernmentAuthority`

---

## 🧪 Testing

```
dotnet test
```