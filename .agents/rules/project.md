---
trigger: always_on
---

# Studio Booking Platform
## Stack
- Backend: ASP.NET Core Web API, Clean Architecture
  (Domain, Application, Infrastructure, API), EF Core, PostgreSQL, xUnit
- Frontend: React + TypeScript + Vite, Arabic RTL-first
## Commands
- dotnet build / dotnet test
- npm run dev / npm run build
## Rules
- One small task at a time, then stop and wait.
- Business rules live in the Domain layer, never in controllers.
- Write tests with every business rule. Run build and tests before saying done.
- Never add a package or touch files outside the task without asking.
- Propose a plan first for anything touching more than 3 files.