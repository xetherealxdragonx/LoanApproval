# Small-Dollar Loan Approval API

A .NET 8 Web API that simulates an instant small-dollar loan approval flow, loosely
modeled on the "apply → decision → funding within seconds" pattern of products like
Q-Cash. Built to demonstrate clean architecture, deliberate dependency injection,
EF Core against SQL Server, automated testing, and CI/CD to Azure.

## Why this project

This mirrors real financial-services patterns: rules-based eligibility decisions,
an auditable decision trail, and a funding step abstracted behind an interface so
it can later point at a real payments processor without touching business logic.

## Architecture

Four layers, each with a single direction of dependency (outer layers depend on
inner ones, never the reverse):

```
LoanApproval.Domain          <- entities only, zero dependencies
LoanApproval.Application     <- interfaces, DTOs, business logic (depends on Domain)
LoanApproval.Infrastructure  <- EF Core, repository, mock funding gateway (depends on Application)
LoanApproval.Api             <- controllers, DI composition root (depends on all of the above)
```

A React + TypeScript front end lives separately in `web/`, consuming the API over
HTTP. It is not part of the .NET solution and builds independently.

`Program.cs` in `LoanApproval.Api` is the composition root — it's the only place
where interfaces are wired to concrete implementations, and it's where the DI
lifetimes (Scoped / Singleton / Transient) are chosen deliberately rather than
defaulted. See the comments there for the reasoning behind each choice.

## Endpoints

| Method | Route                              | Purpose                                          |
|--------|------------------------------------|--------------------------------------------------|
| `POST` | `/api/loanapplications`            | Submit an application; evaluate, persist, fund    |
| `GET`  | `/api/applicants`                  | List members with a count of their applications   |
| `GET`  | `/api/applicants/{memberNumber}`   | One member with their applications and decisions  |

`DecisionType` is serialized as a string (`"Approved"`), not its ordinal, so
clients never have to depend on the enum's declaration order.

## Request flow

1. `POST /api/loanapplications` with a member number and requested amount
2. `LoanApplicationService` loads the applicant, runs `EligibilityService`
3. The decision (Approved / Denied / ManualReviewRequired) is persisted with
   full reasoning and timing, for audit purposes
4. If approved, `IFundingGateway` is called to simulate funding
5. Response includes the outcome, reasoning, evaluation time, and funded timestamp

## Running locally

```bash
# Restore and build
dotnet restore
dotnet build

# Run the API (requires a local SQL Server / LocalDB instance)
dotnet run --project src/LoanApproval.Api
```

Swagger UI will be available at `https://localhost:<port>/swagger` in development.

### Database and demo data

In the `Development` environment the API applies pending migrations and seeds five
demo applicants on startup (see `DbSeeder`), so there is no manual database step.
The seed is idempotent — it is skipped if any applicant already exists.

To apply migrations without running the API:

```bash
dotnet ef database update --project src/LoanApproval.Infrastructure --startup-project src/LoanApproval.Api
```

### Startup resilience

Migration and seeding happen on the startup path in every environment, so a
database that is still waking up would otherwise take the whole application down.
`DbSeeder.SeedAsync` retries connectivity failures with exponential backoff —
six attempts over roughly 31 seconds (1s, 2s, 4s, 8s, 16s).

If every attempt fails the exception is allowed to escape and the process exits.
That is deliberate: crashing lets App Service or Docker restart the process, which
is a longer and better supervised retry than anything done in-process. Starting up
anyway would leave a process that answers requests with errors while still looking
healthy to a platform health probe.

The seeded members are each chosen to trip exactly one branch of the rules engine:

| Member   | Profile                          | Outcome for a $400 request |
|----------|----------------------------------|----------------------------|
| `M-1001` | $3,200/mo, 0 open loans, clean   | Approved (and funded)      |
| `M-1002` | Recent delinquency flag          | ManualReviewRequired       |
| `M-1003` | 2 open loans (at the limit)      | Denied                     |
| `M-1004` | $650/mo income                   | ManualReviewRequired       |
| `M-1005` | $800/mo — exactly on the line    | Approved (and funded)      |

Requesting over the $500 cap, or a non-positive amount, is denied for any member;
an unknown member number returns 404.

## Front end

A React 19 + TypeScript app in `web/`, built with Vite. It lists the members held
in the database and drills into any one of them to show their submitted
applications, each with its decision, reasoning, evaluation time, and funding status.

```bash
cd web
npm install
npm run dev      # http://localhost:5173
```

#### Branding

Both brand assets are the supplied artwork, used as-is rather than reproduced:

| File | Used for | Source size |
|------|----------|-------------|
| `web/public/alloya-fcu-logo.jpg` | Header lockup (`Logo.tsx`), rendered 56px tall | 400×171 |
| `web/public/alloya-icon.jpg`     | Favicon and apple-touch-icon                  | 200×200 |

Both are comfortably above the resolution they render at, including on 2x
displays, so neither is redrawn as SVG.

Because they are JPEGs they have no alpha and carry a solid white matte, which
would show as a pale rectangle against the page's `#f6f7f9` background. The
header image uses `mix-blend-mode: multiply`, which blends white to the backdrop
exactly while leaving the artwork's own colours effectively unchanged. That works
only against a light background — a dark theme would need transparent PNG or SVG
versions of both files.

Run the API first — the Vite dev server proxies `/api` to
`https://localhost:54744` (see `web/vite.config.ts`). Going through the proxy
keeps every browser request same-origin, which is why the API needs **no CORS
policy** for local development. The proxy sets `secure: false` because the
ASP.NET Core developer certificate is self-signed; that is a local-development
setting only.

If you later host the front end on its own origin, the proxy no longer applies
and the API will need a CORS policy at that point.

### How the front end is deployed

There is no Node process in production. `npm run build` compiles the app to
static files, and `web/vite.config.ts` writes them directly into
`src/LoanApproval.Api/wwwroot`. The Web SDK includes `wwwroot` in
`dotnet publish` output, so the API and the UI ship as a single Azure Web App
deployment on one origin — which also means CORS is never needed in production.

`Program.cs` serves those files with `UseStaticFiles`, and falls back to
`index.html` for unmatched paths so a hard refresh on a client-side route such as
`/members/M-1001` loads the app. Unmatched `/api/...` paths are excluded from that
fallback and still return 404, so API clients never receive the HTML shell.

Both CI pipelines therefore build the front end *before* `dotnet publish`:

```bash
cd web && npm ci && npm run build     # writes to src/LoanApproval.Api/wwwroot
dotnet publish src/LoanApproval.Api/LoanApproval.Api.csproj -c Release -o ./publish
```

To reproduce the deployed layout locally, run those two commands and then
`dotnet run --project src/LoanApproval.Api` — the whole app is served from
`https://localhost:54744`, with no Vite dev server involved.

To reset the demo to a clean slate:

```bash
dotnet ef database drop --force --project src/LoanApproval.Infrastructure --startup-project src/LoanApproval.Api
```

## Running tests

```bash
dotnet test
```

Covers the eligibility rules engine directly, and the orchestrating
`LoanApplicationService` with all four dependencies mocked via Moq —
no database or network calls required to run the suite.

## CI/CD

Two equivalent pipeline options are included:

- `azure-pipelines.yml` — Azure DevOps: restore → build → test → publish →
  deploy to an Azure Web App (deploy stage gated to the `main` branch)
- `.github/workflows/ci-cd.yml` — GitHub Actions equivalent, for GitHub-hosted repos

Both require an Azure Web App (Linux, .NET 8) to already exist; update the
`webAppName` / `app-name` value before running.

## Containerizing

The Dockerfile is a three-stage build: a `node:22-alpine` stage compiles the React
app, the SDK stage copies those assets into `wwwroot` before `dotnet publish`, and
the runtime stage carries only the published output. No Node process runs in the
final image — it contains the ASP.NET runtime and static files.

```bash
docker build -t loan-approval-api -f src/LoanApproval.Api/Dockerfile .
```

The container needs a reachable SQL Server: the app applies migrations and seeds
on startup, retrying for about 31 seconds and then **exiting if the database still
cannot be reached** (see "Startup resilience" below). `(localdb)` from
`appsettings.json` is not reachable from inside a container, so supply a
connection string via environment variable. The `__` separator is .NET's
convention for nesting, so `ConnectionStrings__LoanDb` overrides the
`ConnectionStrings:LoanDb` key — the same mechanism Azure App Service uses.

```bash
docker network create loan-net

docker run -d --name loan-sql --network loan-net \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Str0ng!LocalTest1' -e MSSQL_PID=Developer \
  mcr.microsoft.com/mssql/server:2022-latest

docker run -d --name loan-app --network loan-net -p 8080:8080 \
  -e 'ConnectionStrings__LoanDb=Server=loan-sql;Database=LoanApprovalDb;User Id=sa;Password=Str0ng!LocalTest1;TrustServerCertificate=True;' \
  loan-approval-api
```

The full app is then at `http://localhost:8080`. Tear down with
`docker rm -f loan-app loan-sql && docker network rm loan-net`.

The SA password above is a throwaway for local use only. Never put a real
connection string in `appsettings.json` — it is tracked in git. Use
`dotnet user-secrets` locally and App Service connection strings in Azure.

## Next steps / things to extend

- Add authentication (Azure AD / member SSO)
- Replace `MockFundingGateway` with a real payments integration
- Add pagination and a GET endpoint for reviewing decision history
- Add a `ManualReviewQueue` concept for the `ManualReviewRequired` outcome
