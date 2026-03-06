# QA Task Project

Simple todo app for QA candidate testing.

## Stack
- Backend: .NET 10 Minimal API + EF Core + PostgreSQL
- Frontend: React 19 + Vite
- Auth: Keycloak 26 (OIDC)
- Infra: Docker Compose (postgres on 6100, keycloak on 6101)

## Running
- `docker compose up -d` for postgres + keycloak
- `cd ApiService && dotnet run` for backend (port 5118)
- `cd frontend && npm run dev` for frontend (port 4101)

## Key Details
- API auto-migrates DB on startup
- Keycloak realm "qa-task" is auto-imported on first start
- Test user: testuser@example.com / TestPassword123!
- Admin user: admin@example.com / AdminPassword123!
- Frontend proxies /api, /auth, /login, /logout to backend
