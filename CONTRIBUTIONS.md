# Project Contribution Report: LarvaX

**Project:** LarvaX – Dengue Sentinel Platform  
**Architecture:** Clean Architecture (.NET 10 / C# 13, ASP.NET Core MVC, PostgreSQL / Supabase, SignalR, WebRTC, Hangfire, QuestPDF, ML.NET)  
**Timeline:** August 28, 2026 – September 26, 2026  
**Total Tracked Codebase:** 126,825 lines across 295+ source files  
**Total Commits:** 24 on `main` (55 across all feature branches and pull requests)

---

## 1. Executive Summary & Contributor Overview

Project LarvaX was engineered by a 3-member development team with distinct ownership across system architecture, clinical intelligence, enterprise QA/testing, telemedicine, database infrastructure, DevOps, and cloud deployment.

| Contributor | Git Identities / Emails | Primary Specialization | Main Commits | Active Tracked Lines (HEAD) | Code Ownership % |
| :--- | :--- | :--- | :---: | :---: | :---: |
| **Sadman Rahman Arnab** | `sadmanrahman438@gmail.com`<br>`138679165+SadmanRahman12` | **Lead Architect & Full-Stack Core Engineer**<br>Core architecture, Identity/RBAC, AI Chatbot, ML.NET Classifier, Admin Dashboard, Monetization | 17 (70.8%) | 122,330 | **96.5%** |
| **Nafis Fuad** | `nafisfuadisc@gmail.com`<br>`165877137+fuad023` | **Telemedicine, Database, Cloud Deployment, DevOps & QA Lead**<br>Telemedicine (WebRTC/SignalR), 4-Stage QA Test Suite, PostgreSQL Migration, Render/Supabase Deployment, Docker, GIS Maps, Docs | 6 (25.0%) | 3,780 | **3.0%** |
| **Md. Adnan** | `madnan4980@gmail.com`<br>`madnan4980-dot` | **Clinical Fluid Management Specialist**<br>Dengue IV Fluid Calculator, Scientific Formula References, Patient Education | 1 (4.2%) | 708 | **0.6%** |

---

## 2. Architecture & Code Ownership Breakdown

### Codebase Ownership by Clean Architecture Layer

```
Project-Larvax/
├── LarvaX.Core/           # Domain entities, enums, core abstractions
├── LarvaX.Application/    # Business logic, clinical triage, services
├── LarvaX.Infrastructure/ # EF Core, PostgreSQL migrations, QuestPDF, external APIs
├── LarvaX.Web/            # ASP.NET Core MVC, SignalR hubs, Hangfire jobs
└── LarvaX.Tests/          # xUnit test suite (In-Memory EF Core, 4-stage QA matrix)
```

| Layer / Subsystem | Total Active Lines | Sadman Rahman Arnab | Nafis Fuad | Md. Adnan |
| :--- | :---: | :---: | :---: | :---: |
| [LarvaX.Web](LarvaX.Web) | **104,775** | 103,736 (99.0%) | 331 (0.3%) | 708 (0.7%) |
| [LarvaX.Infrastructure](LarvaX.Infrastructure) | **15,126** | 14,001 (92.6%) | 1,125 (7.4%) | 0 (0.0%) |
| [LarvaX.Tests](LarvaX.Tests) | **2,845** | 1,534 (53.9%) | 1,311 (46.1%) | 0 (0.0%) |
| [LarvaX.Application](LarvaX.Application) | **1,493** | 1,493 (100.0%) | 0 (0.0%) | 0 (0.0%) |
| [LarvaX.Core](LarvaX.Core) | **1,102** | 1,064 (96.6%) | 38 (3.4%) | 0 (0.0%) |
| **Root / Docs / Configs** | **1,484** | 502 (33.8%) | 975 (65.7%) | 0 (0.0%) |

### Code Ownership by File Extension

| File Type | Description | Total Lines | Sadman Rahman Arnab | Nafis Fuad | Md. Adnan |
| :--- | :--- | :---: | :---: | :---: | :---: |
| `.cs` | C# Source (Domain, Services, Controllers, Hubs, Tests) | **26,131** | 23,413 (89.6%) | 2,603 (10.0%) | 115 (0.4%) |
| `.cshtml` | Razor Views & ViewComponents | **15,775** | 14,983 (95.0%) | 199 (1.3%) | 593 (3.8%) |
| `.css` | Styling & Themes | **44,359** | 44,359 (100.0%) | 0 (0.0%) | 0 (0.0%) |
| `.js` | Frontend Client Logic (Maps, WebRTC, Charts) | **38,322** | 38,322 (100.0%) | 0 (0.0%) | 0 (0.0%) |
| `.md` | Documentation (`README.md`, `TESTS.md`, `TEST_REPORT.md`) | **957** | 22 (2.3%) | 928 (97.0%) | 0 (0.0%) |
| Config / Docker | `Dockerfile`, `.dockerignore`, `.csproj`, `.slnx` | **660** | 610 (92.4%) | 50 (7.6%) | 0 (0.0%) |

---

## 3. Individual Contributor Profiles

---

### 1. Nafis Fuad (`fuad023` / `fuad`)
**Role:** Telemedicine Architect, Database & Cloud Deployment Lead, QA Test Suite Lead  
**Activity Period:** Aug 30, 2026 – Sep 26, 2026  
**Metrics:** 6 Commits on `main` (23 Branch Commits) | +9,762 Additions / -10,775 Deletions | 3,780 Active Lines (HEAD)  

#### Key Deliverables & Modules Implemented:
1. **Telemedicine & Video Consultation Suite ([PR #2](LarvaX.Web/Controllers/TelemedicineController.cs)):**
   - **Original Author & Lead:** Developed the entire Telemedicine and Virtual Consultation module (Commit `b7507c1e` and PR #2 `55e0c846`).
   - Built the real-time [VideoConsultHub.cs](LarvaX.Web/Hubs/VideoConsultHub.cs) using SignalR and WebRTC signaling for peer-to-peer browser video/audio communication.
   - Implemented [Appointment.cs](LarvaX.Core/Entities/Appointment.cs) domain entity, status enums, and database migrations.
   - Built [TelemedicineController.cs](LarvaX.Web/Controllers/TelemedicineController.cs) along with Razor views for doctor discovery, appointment booking, and the interactive WebRTC video room ([Room.cshtml](LarvaX.Web/Views/Telemedicine/Room.cshtml)).
2. **Cloud Deployment & Production Hosting (Render & Supabase):**
   - **.NET 10 on Render:** Packaged and deployed the full ASP.NET Core web application to **Render** using a high-performance multi-stage Docker build container.
   - **PostgreSQL on Supabase:** Provisioned and configured the cloud **Supabase PostgreSQL** database instance, setting up SSL connection pooling (`pooler.supabase.com`), live schema auto-migrations on container startup, and environment secret injection (`ConnectionStrings__DefaultConnection`).
3. **Enterprise 4-Stage QA Test Matrix & Test Suite ([PR #9](LarvaX.Tests)):**
   - Engineered the comprehensive 4-Stage test architecture across 32 enterprise test scenarios:
     - **Stage 1 Unit Tests ([Stage1UnitTests.cs](LarvaX.Tests/Stage1UnitTests.cs)):** Unit tested core services; discovered and fixed a premature state mutation defect in [InventoryService.cs](LarvaX.Infrastructure/Services/InventoryService.cs).
     - **Stage 2 Integration Tests ([Stage2IntegrationTests.cs](LarvaX.Tests/Stage2IntegrationTests.cs)):** Covered SignalR alert dispatching, Twilio SMS fallback, laboratory diagnosis lifecycle, and RBAC authorization barriers.
     - **Stage 3 System Tests ([Stage3SystemTests.cs](LarvaX.Tests/Stage3SystemTests.cs)):** Tested Hangfire background worker orchestration, multi-user report concurrency, QuestPDF rendering, and WebRTC signaling idempotency.
     - **Stage 4 Acceptance Tests ([Stage4AcceptanceTests.cs](LarvaX.Tests/Stage4AcceptanceTests.cs)):** End-to-end user persona journey validations for all 6 system roles.
   - Authored the comprehensive [TESTS.md](TESTS.md) and [TEST_REPORT.md](TEST_REPORT.md).
4. **Database Modernization & PostgreSQL Migration ([PR #6](LarvaX.Infrastructure)):**
   - Migrated the application persistence engine from SQL Server / LocalDB to PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`.
   - Built automatic schema migration on startup and configured Supabase cloud connection pooling using ASP.NET Core User Secrets.
5. **Containerization & Deployment Configuration:**
   - Authored the production-ready multi-stage [Dockerfile](Dockerfile) and [.dockerignore](.dockerignore) for .NET 10 deployments.

---

### New Contribution: sajid-25
**Role:** Feature contributor (PWA + Payment integration scaffolding)
**Git Identity:** `sajid-25` <sajid.cse.20230104025@aust.edu>

#### Summary of contributions (branch: feature/about)
- Implemented initial PWA offline support: service worker (sw.js), offline fallback page (wwwroot/offline.html), and client queue helper (wwwroot/js/pwa.js).
- Modified Reports Create view to queue reports while offline and added a lightweight API endpoint (POST /api/reports) to accept queued reports when the user is authenticated.
- Scaffolding for bKash payment integration: added IBkashClient, BkashOptions, and a placeholder BkashClient implementation; added Checkout UI wiring for SSLCOMMERZ redirect and webhook endpoint skeleton for bKash notifications.
- Added About page and wiring to Dashboard.

Commit: feat: add PWA offline queue for reports; add service worker fallback and API endpoint; scaffold bKash client files
Branch: feature/about

---

6. **GIS Tile Layer Modernization ([PR #8](LarvaX.Web/Views/Home/Index.cshtml)):**
   - Replaced deprecated CARTO raster tile providers with OpenStreetMap GIS tiles on the live dashboard.
7. **Documentation & Developer Experience ([PR #5](README.md), [PR #7](README.md)):**
   - Authored, maintained, and streamlined technical documentation and quickstart instructions in [README.md](README.md).

---

### 2. Sadman Rahman Arnab (`SadmanRahman12`)
**Role:** Project Creator, Lead Architect & Primary Full-Stack Engineer  
**Activity Period:** Aug 28, 2026 – Sep 26, 2026  
**Metrics:** 17 Commits on `main` | +127,006 Additions / -1,305 Deletions | 122,330 Active Lines (HEAD)  

#### Key Deliverables & Modules Implemented:
1. **Initial Architecture & Scaffolding (Phase 1):**
   - Established the Clean Architecture solution structure ([LarvaX.Core](LarvaX.Core), [LarvaX.Application](LarvaX.Application), [LarvaX.Infrastructure](LarvaX.Infrastructure), [LarvaX.Web](LarvaX.Web)).
   - Implemented ASP.NET Core Identity authentication and Role-Based Access Control (`RoleSeeder.cs`) across Citizen, Doctor, HealthWorker, LabStaff, Administrator, and GovernmentAuthority personas.
   - Built citizen outbreak reporting, GIS mapping, SignalR notifications, and Hangfire background processing.
2. **Clinical Intelligence & Support Network (Phase 2):**
   - Created the AI Symptom Checker and emergency First Aid Guide.
   - Integrated the bilingual DenAI Chatbot (Bengali/English) and Blood Donor Network.
3. **Surveillance, Inventory & Government Reporting (Phase 4):**
   - Implemented public health awareness modules and hospital medical inventory tracking.
   - Built the Government Surveillance Dashboard with automated QuestPDF epidemiological report generation, data analytics, and Twilio SMS fallback integration.
4. **Administrator Operations Dashboard:**
   - Engineered the 10-feature Administrator Control Panel (system metrics, outbreak zone governance, user lifecycle approval, audit log viewer).
5. **DenAI Machine Learning Engine:**
   - Implemented an in-process ML.NET bilingual intent classifier for real-time symptom analysis and triage.
6. **Commercial Subscriptions & Monetization (Phase 5):**
   - Implemented the subscription billing engine with multi-tier plan management, invoicing, and gated premium features.

---

### 3. Md. Adnan (`madnan4980-dot`)
**Role:** Clinical Fluid Management Specialist  
**Activity Period:** Aug 30, 2026  
**Metrics:** 1 Commit on `main` (PR #3) | +964 Additions / 0 Deletions | 708 Active Lines (HEAD)  

#### Key Deliverables & Modules Implemented:
1. **Clinical Dengue Fluid Management System ([PR #3](LarvaX.Web/Views/Clinical)):**
   - Engineered the interactive Dengue IV Fluid Calculator implementing Holliday-Segar maintenance fluid calculations and WHO fluid resuscitation rate guidelines.
   - Built the Hematocrit (HCT) monitoring guide and dynamic fluid titration protocol views.
   - Authored patient clinical education guides and scientific medical formula reference sections.

---

## 4. Pull Request & Commit History Log on `main`

| PR / Commit | Primary Contributor | Title / Scope | Key Modules Touched |
| :---: | :--- | :--- | :--- |
| **#9** (`dc201c8`) | **Nafis Fuad** | Feature/test suite elaboration | 4-Stage 32-scenario QA test suite (`Stage1..4`), `TESTS.md`, `TEST_REPORT.md`, `InventoryService` fix |
| **#8** (`f740d4f`) | **Nafis Fuad** | Replace CARTO basemap with OpenStreetMap | Leaflet GIS map tile layer update |
| **—** (`4996fd2`) | **Sadman Rahman Arnab** | feat(monetization): Subscription System (Phase 5) | Subscription plans, billing, payment simulation |
| **—** (`629847f`) | **Sadman Rahman Arnab** | feat(denai): ML.NET Bilingual Intent Classifier | ML.NET model training & in-process inference |
| **—** (`b010f6f`) | **Sadman Rahman Arnab** | feat(admin): LarvaX Administrator Dashboard | 10-feature administrative control panel |
| **#7** (`3630a41`) | **Nafis Fuad** | Docs/concise readme | Concise setup & operational documentation |
| **#6** (`f6b6ac1`) | **Nafis Fuad** | Feature/postgres migration | Npgsql PostgreSQL migration, Dockerfile, Supabase config |
| **#5** (`8a13b28`) | **Nafis Fuad** | Feature/setup and docs | Initial PostgreSQL & LocalDB setup guides |
| **#3** (`db73b79`) | **Md. Adnan** | Feature/eduhealth v2 | Dengue IV fluid calculator, formula references, patient education |
| **#2** (`55e0c84`)\* | **Nafis Fuad** | feature(telemedicine): VideoConsultHub & Appointments | WebRTC Video Consultation Room, `VideoConsultHub`, `AppointmentsController` |
| **#1** (`23eb00c`) | **Sadman Rahman Arnab** | Core application loop & Phase 2 features | Core loop, First Aid guides, Phase 2 features |
| **—** (`8ec959c`) | **Sadman Rahman Arnab** | Implement Phase 1 core architecture | Identity auth, RBAC approval flow, EF Core, Citizen reporting |

*\*Note: PR #2 was originally authored by Nafis Fuad in branch commit `b7507c1e` and squashed into `main`.*

---

## 5. Summary Conclusion

- **Sadman Rahman Arnab** served as the platform architect and primary full-stack engineer, building the core Clean Architecture framework, Identity/RBAC system, Citizen outbreak reporting, QuestPDF analytics, ML.NET intent classifier, Administrator control center, and monetization engine.
- **Nafis Fuad** led the Telemedicine & Video Consultation suite (WebRTC/SignalR), architected and executed cloud deployments on **Render** (.NET 10 Docker container) and **Supabase** (managed PostgreSQL database), authored the comprehensive 4-stage 32-scenario QA test matrix and test suite, resolved GIS map tile dependencies, and maintained system technical documentation.
- **Md. Adnan** developed the clinical Dengue IV fluid resuscitation calculation tools based on WHO and Holliday-Segar standards, hematocrit monitoring guidelines, and clinical patient education resources.
