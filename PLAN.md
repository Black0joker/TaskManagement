# Task Management API — ASP.NET Core 10 Development Plan

## 1. Project Goal

Build a production-style REST API similar to the backend of Trello/Jira.

The API will support:

* Users
* Authentication and authorization
* Projects
* Tasks
* Comments
* Labels
* Task statuses
* Task priorities
* Due dates
* Pagination
* Filtering
* Sorting
* Searching
* Validation
* Error handling
* Database persistence
* API documentation
* Automated testing

---

# 2. Recommended Technology Stack

### Backend

* ASP.NET Core 10 Web API
* C#
* Entity Framework Core 10
* PostgreSQL or SQL Server
* ASP.NET Core Identity
* JWT Bearer Authentication
* FluentValidation
* AutoMapper or manual mapping
* Swagger / OpenAPI
* xUnit
* Moq or NSubstitute
* Testcontainers for integration tests

### Architecture

Use **Clean Architecture** with the following projects:

```text
TaskManagement/
│
├── TaskManagement.API
├── TaskManagement.Application
├── TaskManagement.Domain
├── TaskManagement.Infrastructure
└── TaskManagement.Tests
```

Dependency direction:

```text
API
 ↓
Application
 ↓
Domain

Infrastructure → Application
Infrastructure → Domain
```

The Domain layer should not depend on Infrastructure or API.

---

# 3. Phase 1 — Solution Setup

## Tasks

* [x] Create the ASP.NET Core 10 solution.
* [x] Create the API project.
* [x] Create the Application project.
* [x] Create the Domain project.
* [x] Create the Infrastructure project.
* [x] Create the Tests project.
* [x] Configure project references.
* [x] Configure nullable reference types.
* [x] Configure global usings.
* [x] Configure development/production settings.
* [x] Configure Swagger/OpenAPI.
* [x] Configure dependency injection.

Expected structure:

```text
src/
├── TaskManagement.API/
├── TaskManagement.Application/
├── TaskManagement.Domain/
└── TaskManagement.Infrastructure/

tests/
└── TaskManagement.Tests/
```

---

# 4. Phase 2 — Domain Design

Define the core business entities.

## User

Possible properties:

```text
User
├── Id
├── UserName
├── Email
├── FirstName
├── LastName
├── CreatedAt
└── UpdatedAt
```

Authentication-related data should be handled through ASP.NET Core Identity rather than manually storing passwords.

---

## Project

```text
Project
├── Id
├── Name
├── Description
├── CreatedById
├── CreatedAt
└── UpdatedAt
```

Relationships:

```text
User 1 ──── * Project
```

---

## Task

Avoid naming the C# entity simply `Task`, because `Task` already exists in .NET.

Use something such as:

```text
TaskItem
```

Properties:

```text
TaskItem
├── Id
├── ProjectId
├── Title
├── Description
├── Status
├── Priority
├── DueDate
├── AssignedToId
├── CreatedById
├── CreatedAt
└── UpdatedAt
```

Relationships:

```text
Project 1 ──── * TaskItem
User    1 ──── * TaskItem
```

---

## Comment

```text
Comment
├── Id
├── TaskItemId
├── AuthorId
├── Content
├── CreatedAt
└── UpdatedAt
```

Relationships:

```text
TaskItem 1 ──── * Comment
User     1 ──── * Comment
```

---

## Label

```text
Label
├── Id
├── Name
├── Color
└── ProjectId
```

Because a task can have multiple labels and a label can belong to multiple tasks, use a many-to-many relationship.

```text
TaskItem * ──── * Label
```

Create a join entity/table:

```text
TaskItemLabel
├── TaskItemId
└── LabelId
```

---

# 5. Phase 3 — Enumerations

Define task status and priority.

## Task Status

Start with:

```text
Todo
InProgress
InReview
Done
Cancelled
```

## Task Priority

```text
Low
Medium
High
Critical
```

Keep these as domain concepts rather than accepting arbitrary strings from the API.

---

# 6. Phase 4 — Database Design

Configure Entity Framework Core.

Create:

```text
AppDbContext
```

Add configurations for:

* User
* Project
* TaskItem
* Comment
* Label
* TaskItemLabel

Prefer separate entity configuration classes:

```text
Infrastructure/
└── Persistence/
    ├── AppDbContext.cs
    └── Configurations/
        ├── ProjectConfiguration.cs
        ├── TaskItemConfiguration.cs
        ├── CommentConfiguration.cs
        ├── LabelConfiguration.cs
        └── TaskItemLabelConfiguration.cs
```

Configure:

* Primary keys
* Foreign keys
* Required fields
* Maximum string lengths
* Indexes
* Unique constraints
* Cascade/restrict delete behavior

Important indexes should include fields commonly used for querying, such as:

```text
ProjectId
AssignedToId
Status
Priority
DueDate
CreatedAt
```

---

# 7. Phase 5 — Entity Framework Migrations

Set up migrations.

Development workflow:

```text
Modify entity
      ↓
Create migration
      ↓
Review migration
      ↓
Apply migration
      ↓
Test database
```

Create the initial database migration.

Also prepare seed data for development:

* Admin user
* Normal user
* Sample projects
* Sample tasks
* Sample labels

---

# 8. Phase 6 — Authentication

Implement authentication before building most protected endpoints.

Features:

* [x] Register
* [x] Login
* [x] JWT access token
* [x] Refresh token
* [x] Logout/revoke refresh token
* [x] Password hashing through ASP.NET Core Identity
* [x] Authentication middleware
* [x] Authorization policies (role-based + permission-based policies implemented in Phase 7)

Endpoints:

```http
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/users/me
```

---

# 9. Phase 7 — Authorization

Define roles:

```text
Admin
User
```

Potential permissions:

```text
Project.Create
Project.Read
Project.Update
Project.Delete

Task.Create
Task.Read
Task.Update
Task.Delete

Comment.Create
Comment.Update
Comment.Delete
```

Start with role-based authorization and evolve toward policy/permission-based authorization if the application becomes more complex.

---

# 10. Phase 8 — Projects API

Implement CRUD operations.

Endpoints:

```http
GET    /api/projects
GET    /api/projects/{id}
POST   /api/projects
PUT    /api/projects/{id}
DELETE /api/projects/{id}
```

Additional endpoints:

```http
GET /api/projects/{id}/tasks
GET /api/projects/{id}/labels
```

Rules:

* Only authorized users can create projects.
* Users should only access projects they belong to.
* Project creators/admins can modify project settings.
* Deletion behavior should be explicitly defined.

---

# 11. Phase 9 — Project Members

For a Trello/Jira-style application, add project membership.

Create:

```text
ProjectMember
├── ProjectId
├── UserId
└── Role
```

Possible roles:

```text
Owner
Admin
Member
Viewer
```

Relationships:

```text
Project * ──── * User
```

Endpoints:

```http
GET    /api/projects/{projectId}/members
POST   /api/projects/{projectId}/members
PUT    /api/projects/{projectId}/members/{userId}
DELETE /api/projects/{projectId}/members/{userId}
```

---

# 12. Phase 10 — Tasks API

Implement the main task functionality.

Endpoints:

```http
GET    /api/tasks
GET    /api/tasks/{id}
POST   /api/tasks
PUT    /api/tasks/{id}
DELETE /api/tasks/{id}
```

Task creation should support:

```json
{
  "projectId": "...",
  "title": "Implement authentication",
  "description": "Add JWT authentication",
  "status": "Todo",
  "priority": "High",
  "assignedToId": "...",
  "dueDate": "2026-09-01"
}
```

---

# 13. Phase 11 — Task Status

Allow changing task status.

Endpoint:

```http
PATCH /api/tasks/{id}/status
```

Example:

```json
{
  "status": "InProgress"
}
```

Consider whether status transitions should have rules.

For example:

```text
Todo
 ↓
InProgress
 ↓
InReview
 ↓
Done
```

Later, you could allow projects to define custom statuses.

---

# 14. Phase 12 — Task Priority

Endpoint:

```http
PATCH /api/tasks/{id}/priority
```

Example:

```json
{
  "priority": "Critical"
}
```

Supported values:

```text
Low
Medium
High
Critical
```

---

# 15. Phase 13 — Task Assignment

Endpoint:

```http
PATCH /api/tasks/{id}/assignee
```

Example:

```json
{
  "userId": "..."
}
```

Validate that the assigned user is actually a member of the project.

---

# 16. Phase 14 — Due Dates

Support:

```text
DueDate
```

Task queries should later support:

```text
Overdue
Due today
Due this week
No due date
Due before/after a date
```

Example:

```http
GET /api/tasks?dueBefore=2026-09-01
```

---

# 17. Phase 15 — Labels

Implement label CRUD.

Endpoints:

```http
GET    /api/projects/{projectId}/labels
POST   /api/projects/{projectId}/labels
PUT    /api/labels/{id}
DELETE /api/labels/{id}
```

Assign/remove labels:

```http
POST   /api/tasks/{taskId}/labels/{labelId}
DELETE /api/tasks/{taskId}/labels/{labelId}
```

Example label:

```json
{
  "name": "Backend",
  "color": "#3B82F6"
}
```

---

# 18. Phase 16 — Comments

Endpoints:

```http
GET    /api/tasks/{taskId}/comments
POST   /api/tasks/{taskId}/comments
PUT    /api/comments/{id}
DELETE /api/comments/{id}
```

Only the comment author or an authorized administrator should be able to edit/delete a comment.

---

# 19. Phase 17 — Pagination

Create a reusable pagination model.

Example request:

```http
GET /api/tasks?page=1&pageSize=20
```

Response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

Create reusable classes such as:

```text
PagedResult<T>
PaginationParameters
```

Set sensible limits:

```text
Default page size: 20
Maximum page size: 100
```

Never allow clients to request unlimited rows.

---

# 20. Phase 18 — Filtering

Implement task filtering.

Examples:

```http
GET /api/tasks?status=InProgress
GET /api/tasks?priority=High
GET /api/tasks?projectId=...
GET /api/tasks?assignedToId=...
GET /api/tasks?labelId=...
```

Combine filters:

```http
GET /api/tasks?projectId=...&status=InProgress&priority=High
```

Date filtering:

```http
GET /api/tasks?dueFrom=2026-08-01&dueTo=2026-08-31
```

Keep filtering logic out of controllers.

Use query/application services or specifications.

---

# 21. Phase 19 — Sorting

Support:

```http
GET /api/tasks?sortBy=createdAt
GET /api/tasks?sortBy=dueDate
GET /api/tasks?sortBy=priority
```

Direction:

```http
GET /api/tasks?sortBy=dueDate&sortDirection=desc
```

Whitelist sortable properties.

Do not dynamically concatenate arbitrary client input into SQL.

---

# 22. Phase 20 — Searching

Implement task search.

Example:

```http
GET /api/tasks?search=authentication
```

Search:

```text
Title
Description
```

Later, consider full-text search if the project becomes large.

---

# 23. Phase 21 — Combined Task Query

The final task endpoint should support multiple operations together.

Example:

```http
GET /api/tasks
    ?page=1
    &pageSize=20
    &search=authentication
    &status=InProgress
    &priority=High
    &projectId=123
    &assignedToId=456
    &sortBy=dueDate
    &sortDirection=asc
```

Recommended processing order:

```text
Authorization
      ↓
Filtering
      ↓
Searching
      ↓
Sorting
      ↓
Pagination
      ↓
Projection
      ↓
Database execution
```

Use `IQueryable` carefully so filtering/sorting/pagination happen in the database rather than loading all tasks into memory.

---

# 24. Phase 22 — DTOs

Do not expose EF Core entities directly from controllers.

Create DTOs.

Example:

```text
TaskDto
CreateTaskRequest
UpdateTaskRequest
UpdateTaskStatusRequest
UpdateTaskPriorityRequest
AssignTaskRequest
```

Separate:

```text
Request DTOs
Response DTOs
```

Example response:

```json
{
  "id": "...",
  "title": "Implement authentication",
  "status": "InProgress",
  "priority": "High",
  "dueDate": "2026-09-01",
  "assignedTo": {
    "id": "...",
    "name": "John"
  },
  "labels": [
    {
      "id": "...",
      "name": "Backend"
    }
  ]
}
```

---

# 25. Phase 23 — Validation

Use FluentValidation or equivalent validation.

Examples:

### Create Project

```text
Name:
- Required
- Maximum length
```

### Create Task

```text
Title:
- Required
- Maximum length

Description:
- Optional
- Maximum length

DueDate:
- Valid date
```

### Comment

```text
Content:
- Required
- Maximum length
```

Return validation errors consistently:

```json
{
  "title": "Title is required",
  "dueDate": "Due date cannot be in the past"
}
```

---

# 26. Phase 24 — Global Error Handling

Implement centralized exception handling.

Return consistent error responses using:

```text
ProblemDetails
```

Handle:

```text
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
500 Internal Server Error
```

Never expose internal exception details in production.

---

# 27. Phase 25 — API Response Conventions

Establish consistent HTTP behavior.

Example:

```text
GET     → 200 OK
POST    → 201 Created
PUT     → 200 OK
PATCH   → 200 OK
DELETE  → 204 No Content
Invalid → 400
Unauthenticated → 401
Forbidden → 403
Missing → 404
Conflict → 409
```

---

# 28. Phase 26 — API Documentation

Configure Swagger/OpenAPI.

Document:

* Authentication
* Endpoints
* Request bodies
* Response models
* Validation errors
* Query parameters
* Pagination
* Filtering
* Sorting
* Searching

Add JWT authentication support to Swagger so protected endpoints can be tested from the UI.

---

# 29. Phase 27 — Testing

Create tests at multiple levels.

## Unit Tests

Test:

* Task creation
* Task updates
* Status changes
* Priority changes
* Authorization rules
* Validation
* Filtering
* Sorting
* Pagination

Example:

```text
CreateTaskHandlerTests
UpdateTaskHandlerTests
DeleteTaskHandlerTests
TaskQueryTests
ProjectAuthorizationTests
```

## Integration Tests

Test the complete API:

```text
Register
 ↓
Login
 ↓
Create project
 ↓
Add project member
 ↓
Create task
 ↓
Assign task
 ↓
Add label
 ↓
Add comment
 ↓
Filter tasks
 ↓
Sort tasks
 ↓
Paginate tasks
```

---

# 30. Phase 28 — Logging

Configure structured logging.

Log important events:

```text
User registered
User logged in
Project created
Task created
Task status changed
Task assigned
Comment created
```

Do not log:

* Passwords
* JWT tokens
* Sensitive authentication information

---

# 31. Phase 29 — Performance

Before considering the API production-ready:

* [ ] Use `AsNoTracking()` for read-only queries.
* [ ] Project directly into DTOs where appropriate.
* [ ] Avoid N+1 queries.
* [ ] Add database indexes.
* [ ] Paginate every potentially large collection.
* [ ] Avoid loading entire tables into memory.
* [ ] Use async EF Core APIs.
* [ ] Review generated SQL for important queries.

---

# 32. Phase 30 — Security

Implement:

* JWT authentication
* Authorization policies
* Password hashing through Identity
* Rate limiting
* CORS configuration
* HTTPS
* Input validation
* SQL injection protection through EF Core
* Secure refresh tokens
* Proper secret management
* Production-safe error responses
* Request size limits

Never store JWT signing secrets directly in source code.

---

# 33. Phase 31 — Docker

Create a Docker setup containing:

```text
ASP.NET Core API
PostgreSQL
```

Example architecture:

```text
                ┌──────────────┐
                │   Client     │
                │ Web/Mobile   │
                └──────┬───────┘
                       │
                       ▼
                ┌──────────────┐
                │ ASP.NET Core │
                │     API      │
                └──────┬───────┘
                       │
                       ▼
                ┌──────────────┐
                │ PostgreSQL   │
                └──────────────┘
```

---

# 34. Phase 32 — CI/CD

Add a CI pipeline that runs:

```text
Restore
 ↓
Build
 ↓
Unit Tests
 ↓
Integration Tests
 ↓
Publish
 ↓
Docker Build
```

Later add:

```text
Docker Registry
 ↓
Deployment
```

---

# 35. Suggested API Structure

Final endpoint structure:

```text
/api
│
├── /auth
│   ├── register
│   ├── login
│   ├── refresh
│   └── logout
│
├── /users
│   ├── me
│   └── {id}
│
├── /projects
│   ├── GET /
│   ├── POST /
│   ├── GET /{id}
│   ├── PUT /{id}
│   ├── DELETE /{id}
│   └── /{id}/members
│
├── /tasks
│   ├── GET /
│   ├── POST /
│   ├── GET /{id}
│   ├── PUT /{id}
│   ├── DELETE /{id}
│   ├── PATCH /{id}/status
│   ├── PATCH /{id}/priority
│   ├── PATCH /{id}/assignee
│   └── /{id}/labels
│
├── /comments
│   ├── PUT /{id}
│   └── DELETE /{id}
│
└── /labels
    ├── PUT /{id}
    └── DELETE /{id}
```

---

# 36. Recommended Application Architecture

Inside the Application project:

```text
Application/
├── Abstractions/
│   ├── Persistence/
│   ├── Authentication/
│   └── Services/
│
├── Features/
│   ├── Authentication/
│   ├── Users/
│   ├── Projects/
│   ├── Tasks/
│   ├── Comments/
│   └── Labels/
│
├── Common/
│   ├── Pagination/
│   ├── Filtering/
│   ├── Sorting/
│   ├── Searching/
│   └── Exceptions/
│
└── DependencyInjection.cs
```

For a larger project, organize features vertically:

```text
Tasks/
├── CreateTask/
│   ├── CreateTaskCommand.cs
│   ├── CreateTaskHandler.cs
│   ├── CreateTaskValidator.cs
│   └── CreateTaskResponse.cs
│
├── GetTask/
│   ├── GetTaskQuery.cs
│   ├── GetTaskHandler.cs
│   └── TaskResponse.cs
│
├── UpdateTask/
├── DeleteTask/
├── ChangeStatus/
└── SearchTasks/
```

This keeps each feature isolated and easier to maintain.

---

# 37. Development Order

Build the project in this order:

```text
1. Solution + projects
        ↓
2. Domain entities
        ↓
3. EF Core + database
        ↓
4. Migrations
        ↓
5. Identity + authentication
        ↓
6. Authorization
        ↓
7. Users
        ↓
8. Projects
        ↓
9. Project members
        ↓
10. Tasks
        ↓
11. Status + priority
        ↓
12. Assignment + due dates
        ↓
13. Labels
        ↓
14. Comments
        ↓
15. Pagination
        ↓
16. Filtering
        ↓
17. Sorting
        ↓
18. Searching
        ↓
19. Validation + error handling
        ↓
20. Testing
        ↓
21. Logging + security
        ↓
22. Docker
        ↓
23. CI/CD
```

---

# 38. MVP Milestones

## Milestone 1 — Foundation

* [x] ASP.NET Core 10 solution
* [x] Clean Architecture
* [x] EF Core
* [x] Database
* [x] Migrations
* [x] Swagger

## Milestone 2 — Authentication

* [x] Register
* [x] Login
* [x] JWT
* [x] Refresh tokens
* [x] Authorization (Phase 7)

## Milestone 3 — Projects

* [x] Project CRUD (Phase 8)
* [x] Project members (Phase 9)
* [x] Project authorization (Phases 7–9)

## Milestone 4 — Tasks

* [x] Task CRUD
* [x] Status
* [x] Priority
* [x] Assignment
* [x] Due dates

## Milestone 5 — Collaboration

* [x] Labels
* [x] Comments

## Milestone 6 — Querying

* [x] Pagination
* [x] Filtering
* [x] Sorting
* [x] Searching

## Milestone 7 — Production Quality

* [ ] Validation
* [ ] ProblemDetails
* [ ] Logging
* [ ] Unit tests
* [ ] Integration tests
* [ ] Security
* [ ] Docker
* [ ] CI/CD

---

# 39. Final Architecture

The finished application should roughly look like:

```text
                         Client
                           │
                           ▼
                  ┌─────────────────┐
                  │ ASP.NET Core 10 │
                  │      API        │
                  └────────┬────────┘
                           │
             ┌─────────────┴─────────────┐
             │                           │
             ▼                           ▼
     ┌────────────────┐          ┌────────────────┐
     │  Application   │          │ Authentication │
     │     Layer      │          │   / Identity   │
     └───────┬────────┘          └────────────────┘
             │
             ▼
     ┌────────────────┐
     │     Domain     │
     │     Layer      │
     └───────┬────────┘
             │
             ▼
     ┌────────────────┐
     │ Infrastructure │
     │     Layer      │
     └───────┬────────┘
             │
             ▼
     ┌────────────────┐
     │   PostgreSQL   │
     └────────────────┘
```

## End Goal

The final API should allow a user to:

```text
Register
   ↓
Login
   ↓
Create a project
   ↓
Invite project members
   ↓
Create tasks
   ↓
Assign tasks
   ↓
Set status
   ↓
Set priority
   ↓
Set due dates
   ↓
Add labels
   ↓
Comment on tasks
   ↓
Search tasks
   ↓
Filter tasks
   ↓
Sort tasks
   ↓
Paginate results
```

This gives you a strong **portfolio-level ASP.NET Core 10 backend** rather than just a CRUD API.
