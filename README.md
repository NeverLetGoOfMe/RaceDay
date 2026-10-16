# RaceDay

RaceDay is a web-based event management system for the South African road running, walking and cycling community. Organisers create and manage events, categories and results, while participants browse events, enter them and track their own results.

The project is built in three parts:

| Part | Content | Folder |
|---|---|---|
| 1 | System planning: ERD, endpoint plan, SQL script | `/docs` |
| 2 | RESTful API in C# with unit tests and CI/CD | `/Part2` |
| 3 | MVC web application, Azure Blob Storage and Docker | added later |

## User roles

- **Organiser** - can create, edit and delete their own events, manage the categories of those events, capture participant results, and view all enrolments for their events.
- **Participant** - can register, browse events and categories, enter an event by choosing a category, view their own enrolments, and view their own results.

Both roles can view and update their own profile. Role rules are enforced on the API: a participant calling an organiser endpoint (or the other way round) gets a `403 Forbidden`, and anyone who is not logged in gets a `401 Unauthorized` on protected endpoints.

## Part 1 - Planning documents (`/docs`)

- `RaceDay_ERD.png` - entity relationship diagram (six entities: Roles, Users, Events, Categories, Enrolments, Results)
- `RaceDay_Endpoint_Plan.md` - the API endpoint plan
- `RaceDay_Schema.sql` - SQL Server script that creates and seeds the database

## Part 2 - RaceDay API (`/Part2`)

- `RaceDay.Api` - ASP.NET Core Web API, Entity Framework Core (Code-First), Swagger
- `RaceDay.Tests` - xUnit tests

### Setup and run

1. Install the .NET 10 SDK, Visual Studio and SQL Server (Express is fine). Make sure the SQL Server service is running.
2. Clone the repository.
3. Open `Part2/RaceDay.Api/appsettings.json` and check the `DefaultConnection` string matches your SQL Server instance (the default is `localhost\SQLEXPRESS`).
4. Create the database from the migrations. In Visual Studio open **Tools > NuGet Package Manager > Package Manager Console**, select `RaceDay.Api` as the default project, and run:
   ```
   Update-Database
   ```
   (or from a terminal in `Part2/RaceDay.Api`: `dotnet ef database update`)
5. Run the `RaceDay.Api` project. Swagger opens at `https://localhost:<port>/swagger`.
6. To run the tests: `dotnet test Part2/RaceDay.Tests/RaceDay.Tests.csproj` (or Test Explorer in Visual Studio).

### Using the API through Swagger

1. Call `POST /api/auth/register` once as an Organiser and once as a Participant.
2. Call `POST /api/auth/login` with one of them. The browser keeps the session cookie, so all the other endpoints now work as that user.
3. Call `POST /api/auth/logout` and log in as the other role to see the role rules (for example, creating an event as a Participant returns 403).

### Authentication design

- Passwords are hashed with PBKDF2 (random salt, 100 000 iterations) before they are stored. The original password is never saved.
- Logging in stores the user's ID and role in a server-side session. Protected endpoints use a `[SessionAuthorize]` attribute that reads the session: no session gives 401, the wrong role gives 403.
- Organisers can only change their own events (and the categories, enrolments and results that belong to them), so one organiser cannot edit another organiser's event.
- Registration lets a user choose Organiser or Participant, as the brief describes. In a real product, organiser accounts would need approval; I kept self-selection to match the brief.

### Differences from Part 1

- The login now uses sessions instead of JWT tokens, because the Part 2 brief requires session management. I added `POST /api/auth/logout` for this reason.
- `Distance` and `EventType` (Run, Walk, Cycle) moved onto Events, as the Part 2 brief requires, and `Distance` was removed from Categories. The ERD, SQL script and endpoint plan were updated to match.
- Added `GET /api/results/me` so participants can view their own results.
- The database is created by EF Core migrations (Code-First), so column types are EF's defaults (for example `nvarchar` instead of `varchar`). The tables, columns and relationships match the ERD. The SQL script in `/docs` is the Part 1 design and seed data.

## CI/CD

Two GitHub Actions workflows run on every push:

- `validate-repo.yml` - checks the repository structure (`/docs` files, Part 2 projects and this README).
- `build-and-test.yml` - builds the API and runs all unit tests.

Green build screenshot:

![CI/CD green build](./docs/ci-success-screenshot.png)

## AI tool disclosure

I used Claude (Anthropic) to help plan the database and API structure and to produce first drafts of the diagram, SQL script and code. I reviewed, ran and changed this work myself.
