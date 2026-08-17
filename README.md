# Task Management API

A production-grade project and task management REST API built with **ASP.NET Core (.NET 10)** following **Clean Architecture** and **CQRS**. It features role-based access control, project membership roles, task status workflow governance, labels, comments, pagination, filtering, sorting, searching, secure JWT authentication with hashed refresh tokens, rate limiting and structured logging.

---

## Tech Stack

| Concern            | Technology |
|--------------------|------------|
| Runtime            | .NET 10 / ASP.NET Core Web API |
| Architecture       | Clean Architecture + CQRS (MediatR) |
| Validation         | FluentValidation |
| Persistence        | Entity Framework Core 10 + SQL Server |
| Identity           | ASP.NET Core Identity (IdentityCore) |
| Auth               | JWT bearer access tokens + hashed refresh tokens |
| API Documentation  | OpenAPI + Scalar (development) |
| Testing            | xUnit + WebApplicationFactory integration tests |

---

## Project Structure

```text
src/
├── TaskManagement.Domain          # Entities, enums, business rules, permissions
├── TaskManagement.Application     # CQRS features, validators, abstractions
├── TaskManagement.Infrastructure  # EF Core, Identity, JWT, authorization policies
├── TaskManagement.API             # Controllers, middleware, endpoints
tests/
└── TaskManagement.Tests           # Unit + integration tests
```

Dependency flow: `API → Application → Domain` and `Infrastructure → Application`.

---

## Features

### Authentication & Users

- Register, login, refresh and logout.
- JWT access tokens (30 min) and rotating refresh tokens (7 days), stored **hashed**.
- Refresh tokens are revoked on logout and rotated on refresh.
- Global roles: `Admin`, `Member`.

### Projects & Membership

- Create/read/update/delete projects.
- Add, update and remove project members.
- Project roles: `Owner`, `Admin`, `Member` with distinct capabilities:

| Capability                        | Owner | Admin | Member |
|-----------------------------------|:-----:|:-----:|:------:|
| Read project data                 | ✅    | ✅    | ✅     |
| Create tasks / comments           | ✅    | ✅    | ✅     |
| Manage tasks & comments           | ✅    | ✅    | ❌     |
| Manage members                    | ✅    | ✅    | ❌     |
| Assign the Admin role             | ✅    | ❌    | ❌     |
| Delete project / self-removal     | ✅    | ❌    | ❌     |

### Tasks

- Title, description, status, priority, due date, assignee, labels.
- Statuses: `Todo → InProgress → InReview → Done`, plus `Cancelled`.
- Priorities: `Low`, `Medium`, `High`, `Critical`.
- **Status workflow governance:**
  - Forward moves are allowed for contributors.
  - Backward moves (rework, reopen) require project Owner/Admin.
  - Unassigned tasks cannot enter `InProgress`.
  - `Done` tasks are immutable (status, priority, assignee, due date, labels).
  - Deleting a task that has comments is blocked (409).
- Due date: required for `Todo`/`InProgress`/`InReview`, forbidden for `Done`/`Cancelled`.

### Labels & Comments

- Per-project labels with unique names and colors.
- Attach/detach labels to tasks.
- Comments with author attribution; paginated per task.

### Querying

- Pagination (`page`, `pageSize`, max 100).
- Filtering: `status`, `priority`, `assignedToId`, `createdById`, `dueBefore`, `dueAfter`.
- Sorting: `sortBy` (`title`, `status`, `priority`, `dueDate`, `createdAt`) + `sortOrder`.
- Searching: `search` over title and description.
- Consistent `PagedResult<T>` response shape with `items`, `page`, `pageSize`, `totalCount`, `totalPages`.

### Production Concerns

- Global exception handling → RFC 9110 **ProblemDetails** for every error (including auth failures).
- **Rate limiting**: global fixed window (300 req/min/IP) + strict auth limiter (5 req/min/IP).
- **CORS**: configurable allowed origins, deny-by-default.
- **Secret management**: JWT signing key never stored in source; production fails fast without it.
- Request body size limit (1 MB).
- Structured JSON console logging with business events; passwords/tokens are never logged.
- Input sanitization: due dates are date-truncated, pagination bounds enforced server-side.

---

## API Endpoints

```text
/api/auth
  POST   /register                  Create account (Member role)
  POST   /login                     Access + refresh token
  POST   /refresh                   Rotate refresh token
  POST   /logout                    Revoke refresh token

/api/users
  GET    /me                        Current user profile + roles

/api/projects
  GET    /                          Projects I belong to
  POST   /                          Create project (creator becomes Owner)
  GET    /{id}                      Project details
  PUT    /{id}                      Update project
  DELETE /{id}                      Delete project (Owner only)
  GET    /{id}/members              List members
  POST   /{id}/members              Add member
  PUT    /{id}/members/{userId}     Change member role
  DELETE /{id}/members/{userId}     Remove member
  GET    /{id}/tasks                Task summaries
  GET    /{id}/labels               Label summaries

/api/tasks
  GET    /                          Paginated, filterable, sortable, searchable
  POST   /                          Create task
  GET    /{id}                      Task details
  PUT    /{id}                      Update task
  DELETE /{id}                      Delete task
  PATCH  /{id}/status               Status transition (workflow rules)
  PATCH  /{id}/priority             Priority change (blocked on Done)
  PATCH  /{id}/assignee             Assign/unassign
  PATCH  /{id}/due-date             Due date change
  GET    /{id}/labels               List labels
  POST   /{id}/labels               Attach label
  DELETE /{id}/labels/{labelId}     Detach label
  GET    /{id}/comments             Paginated comments
  POST   /{id}/comments             Add comment

/api/labels
  POST   /                          Create label

/api/comments
  PUT    /{id}                      Edit comment (author or manager)
  DELETE /{id}                      Delete comment (author or manager)
```

Interactive API reference: run the app in Development and open `/scalar/v1`.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or container), e.g. `mcr.microsoft.com/mssql/server:2022-latest`

### Configuration

Connection string and JWT issuer/audience live in `src/TaskManagement.API/appsettings.Development.json`.

The JWT signing secret is **not** kept in source. Provide it via user-secrets or an environment variable:

```bash
cd src/TaskManagement.API
dotnet user-secrets init TaskManagement.Api --project .
dotnet user-secrets set "JwtSettings:Secret" "<random 64+ char secret>" --project .
```

> Without a configured secret, Development generates a random ephemeral key at startup (tokens do not survive restarts). Production refuses to start without one.

### Run

```bash
dotnet run --project src/TaskManagement.API
```

Migrations are applied and sample data (users, projects, tasks) is seeded automatically in Development. Log in with a seeded account:

| Role  | Email            | Password   |
|-------|------------------|------------|
| Admin | admin@taskmanagement.local | Admin123!  |
| User  | user@taskmanagement.local  | User123!   |

---

## Testing

```bash
dotnet test
```

- **Unit tests**: validation, pagination, filtering/sorting/searching, membership rules, status workflow.
- **Integration tests**: full request pipeline through `WebApplicationFactory` — auth, RBAC, workflow and error mapping.

---

## Security Overview

- Passwords hashed by ASP.NET Identity; JWTs signed with HMAC-SHA256.
- Refresh tokens stored as SHA-256 hashes, rotated on use, revoked on logout.
- Fine-grained authorization: global permission policies + project membership checks (defense in depth in handlers).
- Rate limiting on all routes, extra strict on `/api/auth/*`.
- No secrets, passwords or tokens in logs; structured JSON logs only.
- Production error responses never leak internals.

