# SAMS — Seat Allocation Management System

A seat-reservation system for class enrolment, built around a **time-limited
hold**. A student places a hold on a seat, which reserves it for five minutes;
they either confirm it in that window or it lapses and the seat returns to the
pool automatically. It is the ticketing/checkout problem applied to classrooms:
the interesting part is not CRUD, it is keeping seat counts honest while holds
are created, confirmed, released and expired concurrently.

The system is split across two repositories:

| Part | Repository | Stack |
| --- | --- | --- |
| API | <https://github.com/MashrafeJess/SAMS> (this one) | .NET 10, ASP.NET Core, EF Core 10, PostgreSQL |
| Web client | <https://github.com/MashrafeJess/SAMS---Front> | React 19, TypeScript, Vite 8, React Router 7 |

You need both to run the system. Clone the default branch of each.

---

## Architecture

Three projects, dependencies pointing inward:

```
WebAPI      controllers, DI wiring, cookie auth, background jobs
   │
   ▼
Business    DTOs, service interfaces, business rules, Result envelope
   │
   ▼
Database    entities, SamsDbContext, EF Core migrations
```

`Database` and `Business` have no dependency on ASP.NET Core — the hosting
concern lives only in `WebAPI`. That is why the hold-expiry sweep is split in
two: the rule (`HoldService.ReleaseExpiredHolds`) sits in `Business`, and the
`BackgroundService` that drives it on a timer sits in `WebAPI`.

**Response envelope.** Services return a `Result { IsSuccess, Message, Data }`
rather than throwing for expected failures. Controllers map `IsSuccess == false`
to `400` with the message, and let genuinely unexpected exceptions surface as
`500`. So "no seats left" is a clean 400, while a dropped database connection
is a 500 with a real stack trace.

**Authentication** is a signed HTTP-only cookie (`SAMS.Auth`) issued by
ASP.NET Core cookie authentication, with passwords hashed by
`PasswordHasher<Users>` from ASP.NET Core Identity. Login verifies against a
dummy hash when the email is unknown, so a wrong email and a wrong password
cost the same time and the endpoint cannot be used to enumerate accounts.

### The hold lifecycle

```
          place                      confirm
  (none) ------->  Held  ----------------------->  Confirmed
                    │  \
           delete   │   \  ExpiresAt passes
                    ▼    `---------->  Released  --delete-->  (gone)
                 (gone)
```

- A hold lasts five minutes (`HoldService.HoldWindow`).
- Releasing a hold **deletes** the row and returns the seat immediately.
- A hold that **lapses** is marked `Released` rather than deleted, so the
  student can still see what happened to their seat.
- `HoldExpirySweeper` runs every 30 seconds and returns lapsed seats to the
  pool. It is housekeeping, not enforcement: the confirm path re-checks expiry
  itself, so no seat is double-issued if the sweeper stops.

---

## Running it

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- PostgreSQL 14+ running on `localhost:5432`

### 1. Database

Create an empty database named `SAMS`, then point the API at it. The connection
string lives in `WebAPI/appsettings.json`:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=SAMS;Username=postgres;Password=123456"
```

Apply the migrations:

```bash
dotnet ef database update --project Database --startup-project WebAPI
```

If `dotnet ef` is not installed: `dotnet tool install --global dotnet-ef`.

### 2. API

```bash
dotnet run --project WebAPI --launch-profile https
```

Swagger UI: <https://localhost:7153/swagger>

> **Use the `https` profile.** `Program.cs` calls `app.UseHttpsRedirection()`,
> so under the `http` profile every request answers `307` pointing at a port
> that profile never binds.

### 3. Web client

In the front-end repository:

```bash
npm install
npm run dev          # http://localhost:5173
```

Vite proxies `/api` to `https://localhost:7153`. That proxy is load-bearing,
not a convenience: the session cookie is `Secure; SameSite=Lax`, and because a
"site" is scheme + domain, calling `https://localhost:7153` directly from a
page served over `http://localhost:5173` is a *cross-site* request, so the
browser would strip the cookie from every call. The proxy makes it
first-party. Override the target with `VITE_API_TARGET` (see `.env.example`).

### 4. Seed enough data to click through

There is no endpoint for shifts, and no endpoint that grants Admin, so two
steps are SQL. This is a known gap, not an oversight (see *Known limitations*).

**a. Create at least one shift.** Classes join to `Shift` for their display
name; without a row, class names render with a dangling separator.

```sql
INSERT INTO "Shift" ("ShiftName", "ShiftStart", "ShiftEnd") VALUES
  ('Morning', TIMESTAMPTZ '2026-01-01 08:00:00Z', TIMESTAMPTZ '2026-01-01 12:00:00Z'),
  ('Evening', TIMESTAMPTZ '2026-01-01 17:00:00Z', TIMESTAMPTZ '2026-01-01 21:00:00Z');
```

**b. Register a user** at <http://localhost:5173/register>, then promote them:

```sql
UPDATE "Users" SET "RoleId" = 1, "RoleName" = 'Admin' WHERE "Email" = 'you@example.com';
```

Sign out and back in for the new role to appear in the cookie.

### 5. Walk the happy path

1. As **Admin** → *Admin → Classes*: create a class (use a `ShiftId` from step 4a).
2. *Admin → Students*: enrol a student directly.
3. Sign in as that student → **Classes**: *Hold a seat*, watch the countdown.
4. *Confirm seat* within five minutes, or leave it and watch the sweeper
   return the seat (the API logs `Released N expired hold(s)`).
5. **My holds**: review, confirm, release and remove holds.

---

## Where the engineering decisions are

If you are reviewing this, these are the files where real choices were made
rather than boilerplate:

| File | What it shows |
| --- | --- |
| `Business/Service/HoldService.cs` | The hold state machine, seat-counter integrity, capacity checks, UTC handling |
| `WebAPI/BackgroundJobs/HoldExpirySweeper.cs` | Scoped-vs-singleton DI lifetime handling, a timer loop that survives a failing tick |
| `Business/Helper/HelperResultClass.cs` | The success/failure envelope the whole API is built on |
| `Business/Service/UserService.cs` | Password hashing, rehash-on-login, constant-time unknown-email handling |
| `sams-front/src/api/client.ts` | Normalising three different server error shapes into one message |
| `sams-front/vite.config.ts` | The cookie/`SameSite` reasoning behind the dev proxy |
| `sams-front/src/auth/` | Cookie session bootstrapping and route guards |

---

## Known limitations

Deliberately listed rather than hidden; these are the next things to fix.

**Security**
- `ClassController` and `HoldController` carry no `[Authorize]` attribute.
- `GET /api/Hold/me/holds` and `DELETE /api/Hold/DeleteHold` take the id from
  the query string rather than the session cookie, so one student can read or
  delete another's holds.
- The development connection string, including its password, is committed in
  `appsettings.json`. It should move to user-secrets.

**Correctness under load**
- `Class.HoldNumber` is a read-modify-write counter with no transaction and no
  concurrency token, so two students racing for the last seat can both pass the
  capacity check. The robust fix is to derive the count from the `Holds` table
  instead of storing it.
- Nothing stops one student holding several seats in the same class.

**Modelling**
- Entities use plain `long` foreign keys with no navigation properties or
  database-level relationships, so there is no referential integrity.
- `Users` stores both `RoleId` and `RoleName`, and `Holds` both
  `HoldStatusId` and `HoldStatusName`; the duplicated state can disagree.
- The `Seats` table is defined but unused.

**Missing endpoints**
- No endpoint lists, creates or updates shifts.
- No endpoint lists users, or grants the Admin role.
- No password reset.
