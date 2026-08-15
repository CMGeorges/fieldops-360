# FieldOps 360

FieldOps 360 is a demonstrable operations workflow for property managers and service companies. It makes assignment ownership, response deadlines, appointments, exceptions and proof visible in one auditable timeline.

> Portfolio status: complete vertical demo, not a production SaaS. It uses synthetic identities and an in-memory persistence adapter. PostgreSQL, production identity and external notification providers are the next industrialization milestone.

## What works now

- create and triage a work order;
- offer an assignment to a contractor;
- accept and schedule the intervention;
- start work, submit it for review and close the case;
- model `NO_RESPONSE`, `NO_SHOW` and reassignment as first-class states;
- reject invalid transitions and unauthorized contractor actions;
- keep an append-only, versioned audit timeline;
- explore the full workflow in a responsive React/TypeScript control room;
- run the API and web client together with Docker Compose.

## Run the complete demo

Prerequisite: Docker with Compose.

```bash
docker compose up --build
```

Open `http://localhost:4173`. The API health endpoint is available at `http://localhost:8080/health`.

For separate development processes:

```bash
dotnet run --project src/FieldOps.Api/FieldOps.Api.csproj --urls http://localhost:8080
cd web && npm install && npm run dev
```

## Architecture

```text
React / TypeScript
        │
        ▼
ASP.NET Core minimal API
        │
        ▼
Application use cases + repository port
        │
        ├──► Domain aggregate + append-only events
        │
        └──► In-memory demo adapter
```

The code is factored by responsibility:

- `FieldOps.Domain` — business invariants and state transitions;
- `FieldOps.Application` — use cases, DTOs and repository contract;
- `FieldOps.Infrastructure` — replaceable persistence adapters;
- `FieldOps.Api` — HTTP transport, demo identity and error translation;
- `web` — operator dashboard and interactive workflow;
- `tests` — domain regression tests.

The design intentionally starts as a modular monolith. Service extraction is postponed until load, deployment boundaries or team ownership justify it.

## Verification

```bash
dotnet test tests/FieldOps.Domain.Tests/FieldOps.Domain.Tests.csproj \
  /p:CollectCoverage=true \
  /p:CoverletOutput=./TestResults/coverage \
  /p:CoverletOutputFormat=cobertura
cd web && npm ci && npm run build
```

GitHub Actions executes backend build/tests and the production frontend build on every pull request and push to `master`. The domain quality gate requires at least 90% line coverage and 85% branch coverage; the Cobertura report is retained as a workflow artifact.

## Documentation

- [Product definition](PRODUCT.md)
- [Architecture and threats](docs/architecture.md)
- [API walkthrough](docs/api.md)
- [Security policy](SECURITY.md)

## Industrialization roadmap

1. PostgreSQL persistence with optimistic concurrency and migrations.
2. Outbox processor for reliable notifications.
3. Microsoft Entra ID / OIDC and organization-scoped authorization policies.
4. Evidence upload with short-lived signed URLs and malware scanning.
5. Integration tests with Testcontainers, browser E2E and deployment observability.
