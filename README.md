# Warehouse Management System

[![CI](https://github.com/SzymonRitz-code/WarehouseManagementSystem/actions/workflows/ci.yml/badge.svg)](https://github.com/SzymonRitz-code/WarehouseManagementSystem/actions/workflows/ci.yml)

Portfolio implementation of a warehouse-management system, built around domain modelling, reliable asynchronous integration and a production-oriented ASP.NET Core API.

## License

This repository is available exclusively for recruitment and non-commercial educational use. Commercial use and redistribution are prohibited. See [LICENSE](LICENSE) for the full terms.

## Recruiter quick tour

1. Start with the [architecture](#architecture) section below.
2. Review the document workflow in `WarehouseManagementSystem.Domain/Model/Documents` and `WarehouseManagementSystem.API/Services/Documents`.
3. Follow the reliable messaging path: [`ADR-002`](docs/adr/ADR-002-transactional-outbox.md), [`messaging operations`](docs/messaging-operations.md), `OutboxPublisherWorker`, and the FakeShipping/FakeBilling consumers.
4. Inspect the executable specifications in `WarehouseManagementSystem.Test`.
5. Run the system through [Docker Compose](#run-with-docker-compose) and open Swagger.

## Highlights

- ASP.NET Core 8 API with a separate Angular client and Duende IdentityServer.
- Modular-monolith structure with explicit domain, infrastructure, contracts and API layers.
- CQRS-style split: command services change aggregate state; query services serve read models.
- Warehouse, zone, product, batch, stock, reservation and PZ/WZ/MM document workflows.
- Transactional outbox with RabbitMQ publisher confirms; idempotent FakeShipping, FakeBilling and ERP inbox flows.
- SQL Server persistence through EF Core, Redis-backed query cache, audit log, health checks and Serilog logging.
- FluentValidation automatic API validation with RFC 7807-style `422 Unprocessable Entity` responses.
- xUnit, FluentAssertions, Moq and Testcontainers-based API/repository/performance coverage.

## Architecture

```text
Angular client ── OIDC ──> IdentityServer
      │
      └── HTTPS/JWT ──> ASP.NET Core API
                            │
        Domain aggregates + command/query services
                            │
                    EF Core / SQL Server
                            │
                  Transactional Outbox
                            │
                        RabbitMQ
                  ┌─────────┴─────────┐
             FakeShipping         FakeBilling
                  │
               FakeERP ──> WMS Inbox
```

The system intentionally uses a modular monolith: it keeps domain transactions simple while preserving explicit integration boundaries for possible future extraction. See the [ADR index](docs/adr/README.md) for the decision record.

## Repository layout

| Path | Responsibility |
| --- | --- |
| `WarehouseManagementSystem.Domain` | Aggregates, value objects, domain rules, repository contracts and domain exceptions. |
| `WarehouseManagementSystem.Infrastructure` | EF Core persistence, repository implementations and background reservation processing. |
| `WarehouseManagementSystem.Contracts` | Integration-message contracts shared with external simulators. |
| `WarehouseManagementSystem.API` | Controllers, command/query services, validation, authentication, cache and messaging publisher/consumers. |
| `WarehouseManagementSystem.Idp` | Local Duende IdentityServer for the development OIDC flow. |
| `WarehouseManagementSystem.FakeShipping` | Idempotent downstream shipping consumer. |
| `WarehouseManagementSystem.FakeBilling` | Idempotent downstream billing consumer. |
| `WarehouseManagementSystem.FakeERP` | ERP outbox and WMS inbox demonstration. |
| `WarehouseManagementSystem.Test` | Unit, controller, repository, API-integration and query-performance tests. |
| `WarehouseManagementSystemClient` | Angular single-page client. |
| `docs` | Architecture and operations documentation. |

## Run with Docker Compose

### Prerequisites

- Docker Desktop (or another Docker Engine) running.
- A trusted local .NET HTTPS certificate.

Create the shared development certificate once:

```powershell
dotnet dev-certs https --trust
New-Item -ItemType Directory -Force .certs | Out-Null
dotnet dev-certs https -ep .certs\localhost.pfx -p wms-local-dev
dotnet dev-certs https -ep .certs\localhost.pem --format Pem --no-password
```

Then build and start the complete local environment:

```powershell
docker compose up -d --build
```

| Service | Address |
| --- | --- |
| Angular client | `https://localhost:4201` |
| API / Swagger | `https://localhost:8081/swagger/index.html` |
| API health check | `https://localhost:8081/health` |
| IdentityServer discovery | `https://localhost:8091/.well-known/openid-configuration` |
| RabbitMQ management | `http://localhost:15672` (`guest` / `guest`) |
| SQL Server | `localhost,14333` |

Useful commands:

```powershell
docker compose ps
docker compose logs -f api
docker compose down
```

`docker compose down -v` also removes the local database and simulator volumes.

## Database seeding

Seeding is performed by a hosted service when the API starts. It creates master data first (warehouses, zones, products, batches and stock), then operational documents and their items. It is disabled in `appsettings.json`; Docker Compose enables it explicitly.

| Profile | Recommended use | Operational volume |
| --- | --- | --- |
| `Demo` | Recruiter walkthrough, local smoke test and UI exploration. | 1,000 movement items |
| `Medium` | First realistic local workload. | 20,000 movement items |
| `Stress` | Query and capacity investigation. | 500,000 movement items |
| `Extreme` | Deliberate long-running stress experiment only. | 10,000,000 movement items |

The repository has also been exercised with the `Extreme` profile to validate the high-volume seeding and workload-oriented screens. It remains intentionally unsuitable for a first local startup.

Recommended workflow:

1. Use `Demo` for normal development and presentations.
2. Move to `Medium`, then `Stress`, only when measuring a specific query or screen.
3. Run `Extreme` only on a disposable database with enough disk, memory and time; it is not suitable for a first startup.
4. The seeder skips a stage when its target data already exists. To seed a different profile, start with a clean development database rather than mixing profiles.

For Docker, set `Seeding__Profile` to `Demo` (or the required profile) in `docker-compose.yml` before the first startup. To rebuild a disposable local database, run:

```powershell
docker compose down -v
docker compose up -d --build
```

This removes the SQL Server and simulator volumes. For a non-Docker API run, enable the seeder and select a profile through environment variables:

```powershell
$env:Seeding__Enabled = "true"
$env:Seeding__Profile = "Demo"
dotnet run --project WarehouseManagementSystem.API
```

## API and validation

All controller routes require authentication by default. Swagger is available in Development and supports a Bearer token. Main resource areas are warehouses and zones, products and batches, stock and reservations, documents/items, audit logs, and integration diagnostics.

Request validators are registered automatically from `WarehouseManagementSystem.API/Validators`. FluentValidation failures are returned as `application/problem+json` with HTTP `422`; the exception middleware uses the same problem-details format for domain and application errors.

## Messaging demonstration

Confirming a document records the document change and its integration event in one SQL transaction. A background worker publishes pending outbox rows to `wms.events`; downstream consumers use durable queues, retry/DLQ topology and persistent processed-message records to tolerate at-least-once delivery.

- `FakeShipping` creates a shipping projection.
- `FakeBilling` only invoices eligible `WZ` documents and also guards against duplicate source documents.
- `FakeERP` demonstrates the inverse direction: ERP outbox → RabbitMQ → WMS inbox → WMS document.
- Inspect recent WMS outbox rows through authenticated `GET /api/integration/outbox`.

For operational details and delivery guarantees, see [Messaging operations](docs/messaging-operations.md) and [ERP inbox flow](docs/erp-inbox-flow.md).

## Tests

Run all tests:

```powershell
dotnet test WarehouseManagementSystem.sln --configuration Release
```

Docker must be running: repository, API-integration and performance tests use Testcontainers with SQL Server. The CI workflow restores in locked mode, builds Release and runs the same test suite with coverage collection.

Tests use xUnit and FluentAssertions. Coverage includes domain invariants, command/query services, controller behaviour, outbox/retry/idempotency flows, repositories, API authorization and query-count/performance guardrails.

## Documentation

- [Architecture comparison](docs/ARCHITECTURE_COMPARISON.md)
- [Messaging operations](docs/messaging-operations.md)
- [ERP inbox flow](docs/erp-inbox-flow.md)
- [Architecture Decision Records (ADR)](docs/adr/README.md)
  - [ADR-001: Modular monolith](docs/adr/ADR-001-modular-monolith.md)
  - [ADR-002: Transactional outbox](docs/adr/ADR-002-transactional-outbox.md)
  - [ADR-003: Domain events vs integration events](docs/adr/ADR-003-domain-events-vs-integration-events.md)
  - [ADR-004: At-least-once delivery and idempotent consumers](docs/adr/ADR-004-at-least-once-delivery-and-idempotent-consumers.md)
  - [ADR-005: Avoid a universal generic repository](docs/adr/ADR-005-avoid-universal-generic-repository.md)
  - [ADR-006: Restrict repository capabilities with IReadOnlyRepository](docs/adr/ADR-006-restrict-repository-capabilities-with-ireadonlyrepository.md)
  - [ADR-007: Isolate API integration tests from production background services](docs/adr/ADR-007-isolate-api-integration-tests-from-background-services.md)

## Current limitations

This is a portfolio/development environment, not a production deployment. Development credentials and local certificate settings are intentionally present in Compose; production configuration must use a secret store and hardened TLS/authentication settings. The code also marks follow-up work directly around messaging resilience (connection reuse/reconnect, multi-instance outbox claiming, retention/metrics and further failure-injection tests).
