# QA Task - Todo Application

A simple Todo application built with a .NET API backend, React frontend, Keycloak authentication, and PostgreSQL database. Your task is to write automated tests using Playwright.

## Architecture

- **Backend**: ASP.NET Core 10.0 Minimal API with Entity Framework Core
- **Frontend**: React 19 + Vite
- **Auth**: Keycloak 26 (OpenID Connect)
- **Database**: PostgreSQL 16

## Prerequisites

- [Docker](https://www.docker.com/) and Docker Compose

For local development only:
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)

## Getting Started

### Option A: Docker Compose (recommended)

Start everything (PostgreSQL, Keycloak, API, and frontend) with a single command:

```bash
docker compose up -d
```

Wait ~30 seconds for Keycloak to fully start, then open http://localhost:4101.

### Option B: Local Development

#### 1. Start Infrastructure (PostgreSQL + Keycloak)

```bash
docker compose up -d postgres keycloak
```

Wait ~30 seconds for Keycloak to fully start. You can check at http://localhost:6101.

#### 2. Start the API

```bash
cd ApiService
dotnet run
```

The API will start at http://localhost:5118. It auto-migrates the database on startup.

#### 3. Start the Frontend

```bash
cd frontend
npm install
npm run dev
```

The frontend will be at http://localhost:4101.

### Open the App

Navigate to http://localhost:4101. You will see a login prompt. Click "Log In with Keycloak" to authenticate.

## Test Credentials

| User | Email | Password |
|------|-------|----------|
| Regular User | testuser@example.com | TestPassword123! |
| Admin User | admin@example.com | AdminPassword123! |

## Keycloak Admin Console

- URL: http://localhost:6101/admin
- Username: admin
- Password: admin

## API Endpoints

All `/api/*` endpoints require authentication (cookie-based via Keycloak login).

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Health check (no auth) |
| GET | `/auth/user` | Current user info (no auth required) |
| GET | `/login` | Initiate Keycloak login |
| GET | `/logout` | Log out and redirect |
| GET | `/api/todos` | List all todos |
| GET | `/api/todos/{id}` | Get a single todo |
| POST | `/api/todos` | Create a todo |
| PUT | `/api/todos/{id}` | Update a todo |
| DELETE | `/api/todos/{id}` | Delete a todo |

### Todo Item Schema

```json
{
  "id": 1,
  "title": "string",
  "isComplete": false,
  "createdAt": "2026-01-01T00:00:00Z",
  "updatedAt": "2026-01-01T00:00:00Z"
}
```

## Your Task

Write automated tests using **Playwright** that cover the following areas:

### 1. Frontend Tests
- Verify the homepage loads and displays the app title
- Log in via Keycloak and verify the user name is displayed
- Add a new todo item and verify it appears in the list
- Mark a todo as complete and verify it shows as completed
- Mark a completed todo as incomplete and verify it updates correctly
- Delete a todo and verify it is removed from the list

### 2. API Tests
- Verify the health endpoint returns a successful response
- Authenticate and call the `/api/todos` endpoint to list todos
- Create a new todo via the API and verify it is returned
- Update a todo via the API and verify the changes persist
- Toggle a todo's completion status (both directions) and verify the state is correct
- Delete a todo via the API and verify it is gone

### 3. Bug Hunting
- This application contains at least one intentional bug. Your tests should be thorough enough to catch it. Document any bugs you find.

### Notes
- The Keycloak login flow uses a standard browser-based OIDC flow. Your Playwright tests should handle the Keycloak login form.
- The app uses cookie-based authentication. Once logged in via the browser, the session cookie will be available for API calls made from the same browser context.
- The app comes seeded with 3 todo items.
- `data-testid` attributes are provided on key UI elements to help with selectors.

### Deliverables
- A `tests/` directory in this repo containing your Playwright tests
- A `playwright.config.ts` file at the repo root
- Instructions on how to run your tests (you can add to this README)
- Any helper utilities you create (e.g., auth helpers, fixtures)

Good luck!
