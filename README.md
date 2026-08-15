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

`Program.cs` in `LoanApproval.Api` is the composition root — it's the only place
where interfaces are wired to concrete implementations, and it's where the DI
lifetimes (Scoped / Singleton / Transient) are chosen deliberately rather than
defaulted. See the comments there for the reasoning behind each choice.

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

```bash
docker build -t loan-approval-api -f src/LoanApproval.Api/Dockerfile .
docker run -p 8080:8080 loan-approval-api
```

## Next steps / things to extend

- Add authentication (Azure AD / member SSO)
- Replace `MockFundingGateway` with a real payments integration
- Add pagination and a GET endpoint for reviewing decision history
- Add a `ManualReviewQueue` concept for the `ManualReviewRequired` outcome
