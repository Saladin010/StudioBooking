---
trigger: always_on
---

# Studio Booking Platform

## Stack
- Backend: ASP.NET Core Web API, Clean Architecture (Domain, Application, Infrastructure, API), EF Core, PostgreSQL, xUnit
- Frontend: React + TypeScript + Vite, Arabic RTL-first

## Commands
- dotnet build / dotnet test
- npm run dev / npm run build

## Rules
- Autonomous Execution: Execute tasks end-to-end from analysis to implementation and automated testing.
- No Routine Permissions: Do not ask for permission for routine file creations, edits, package installations required by the tech stack, or standard development commands.
- Strategic Inquiries Only: Only stop and ask when encountering a genuine open question regarding business logic, product rules, or a fundamental architectural trade-off.
- Never Guess Business Requirements: Never guess or assume ambiguous business requirements—clarify genuine open questions directly with the user.
- Safety Guardrails: Never execute destructive deletion commands or modifications outside project scope without explicit user approval.
- Clean Architecture Invariant: Business rules live strictly in the Domain layer, never in controllers.
- Test-Driven Quality: Write unit/integration tests with every business rule. Always run build and tests to verify success before declaring any task done.

## Language
- Always reply in English in the terminal. The product UI text is Arabic (RTL).