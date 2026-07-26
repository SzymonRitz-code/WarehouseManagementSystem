# WarehouseManagementSystem

[![CI](https://github.com/SzymonRitz-code/WarehouseManagementSystem/actions/workflows/ci.yml/badge.svg)](https://github.com/SzymonRitz-code/WarehouseManagementSystem/actions/workflows/ci.yml)

Architecture decisions are recorded in [docs/adr](docs/adr/README.md). Messaging operations are described in [docs/messaging-operations.md](docs/messaging-operations.md).

## Backend stabilization

Current stabilization scope:

- `CancelDocumentAsync` runs in a serializable transaction so reservation release, document cancellation, audit log entry and save commit as one unit.
- The old standalone transfer endpoint is not exposed. Transfer remains a domain workflow state for documents.
- Stock availability is exposed by `GET /api/stocks/availability` and linked from the main client navigation.
- Demo/placeholder entries were removed from the main sidebar/header navigation.
- Backend and frontend test/build commands should stay green before seeding or workload tests.

## Seeder profiles

Database seeding is disabled by default.

```json
"Seeding": {
  "Enabled": false,
  "Profile": "Medium"
}
```

Available profiles:

- `Demo`: small local smoke-test data.
- `Medium`: first realistic workload pass.
- `Stress`: larger dataset for bottleneck discovery.
- `Extreme`: keeps the existing 10,000,000 movement-item seed volume.

Operational order:

1. Seed master data: warehouses, zones, products, product batches, stock.
2. Seed operational data: PZ/WZ/MM documents and document items.
3. Start with `Medium`, then `Stress`, then `Extreme` only when the system is ready for long-running tests.

## Workload screens

Primary screens for workload and regression checks:

- Document list
- Pending documents
- Stock list
- Stock availability
- Product batch list
- Audit log list

Track SQL duration, logical reads, payload size, API P50/P95, Angular render time, runtime memory and number of SQL queries per screen.

Likely first bottlenecks:

- `CountAsync` plus page query on large lists.
- `Contains` search patterns over large text fields.
- Document item joins and audit payload size on high-volume data.

## Docker quick start

Minimal development stack contains:

- `client`: Angular SPA served by Nginx, exposed at `https://localhost:4201`.
- `api`: WMS ASP.NET Core API, exposed at `https://localhost:8081`.
- `idp`: Duende IdentityServer, exposed at `https://localhost:8091`.
- `sqlserver`: SQL Server 2022, exposed on host port `14333`.

Useful commands:

Create and trust the shared localhost certificate once before starting the stack:

```powershell
dotnet dev-certs https --trust
New-Item -ItemType Directory -Force .certs | Out-Null
dotnet dev-certs https -ep .certs\localhost.pfx -p wms-local-dev
dotnet dev-certs https -ep .certs\localhost.pem --format Pem --no-password
```

The PEM export creates both `.certs\localhost.pem` and `.certs\localhost.key`.

```powershell
docker compose up -d --build
```

Builds API and IDP images, starts SQL Server, waits until SQL Server is healthy, then starts the API.

```powershell
docker compose ps
```

Shows running containers and published ports.

```powershell
docker compose logs -f api
docker compose logs -f idp
```

Streams logs for the selected service.

```powershell
docker compose down
```

Stops and removes containers, keeping the SQL Server volume.

```powershell
docker compose down -v
```

Stops containers and removes the SQL Server volume. Use this only when you want a clean development database.

Smoke-test URLs:

- Angular client: `https://localhost:4201`
- API Swagger: `https://localhost:8081/swagger/index.html`
- IDP discovery: `https://localhost:8091/.well-known/openid-configuration`

Ports `4200`, `8080`, and `8090` are HTTP entry points used for automatic redirects to the HTTPS addresses above. All containers mount the same host-trusted development certificate from `.certs`, so the browser does not show a certificate warning.

### Docker HTTPS and login fix

The Docker setup previously mixed browser-visible addresses with Docker-internal service names and sent plain HTTP traffic to HTTPS-only ports. This caused Nginx `400 Bad Request`, empty API/IDP responses, rejected JWT issuers, and an OIDC login flow that did not return reliably to the Angular application.

The current setup separates HTTP redirect ports from HTTPS application ports, uses one trusted `localhost` certificate for Nginx and Kestrel, configures the public IdentityServer issuer independently from its internal Docker address, permits the Docker client origin in CORS/OIDC, and completes `checkAuth()` on the Angular callback route before navigation.

`Database__MigrateOnStartup` is disabled in Docker Compose because the current EF migration chain fails on a clean SQL Server database while dropping `PK_Users` before removing the dependent `FK_Documents_Users_TransferStartedById` foreign key.

## Development auth workaround

The API has a development-only JWT/certificate workaround for IdentityServer.

Known technical debt:

- local Docker auth settings are provided through environment variables in `docker-compose.yml`;
- JWKS keys are resolved manually;
- local certificate validation is bypassed in development;
- auth diagnostics still use console output.

Before production hardening, move auth values to configuration, remove the certificate bypass, replace console diagnostics with structured logging and prefer the standard JWT bearer metadata/JWKS flow where possible.

## Event-driven learning slice: WMS to FakeShipping and FakeBilling

`docker compose up -d --build` now starts SQL Server, RabbitMQ (management UI: http://localhost:15672, `guest` / `guest`), the WMS API, `fake-shipping` and `fake-billing`. The fake consumers have separate SQLite volumes (`shipping-data`, `billing-data`) and never use the WMS database.

Flow: confirm a document in WMS -> one SQL transaction saves the document and `OutboxMessages` -> WMS publisher sends a durable `document.confirmed` message to `wms.events` -> RabbitMQ routes a copy to both durable queues -> FakeShipping writes `FakeShipments`; FakeBilling writes `FakeInvoices`; both write their local `ProcessedMessages` before ACK.

- Inspect the WMS outbox with authenticated `GET /api/integration/outbox` (latest 100 messages, including status, retry count and error).
- Inspect RabbitMQ queues and the DLQ in the management UI.
- For a local, non-Docker run start RabbitMQ/SQL with Compose, then run `dotnet run --project WarehouseManagementSystem.FakeShipping`, `dotnet run --project WarehouseManagementSystem.FakeBilling` and the API. The worker configuration is in each project's `appsettings.json`.
- To demonstrate duplicate delivery, republish the same JSON payload in RabbitMQ with the original `MessageId`; FakeShipping logs a duplicate and leaves exactly one `FakeShipments` row.

The delivery guarantee is at-least-once, not exactly-once: a crash after a broker confirm but before WMS records `Published` can produce a duplicate. Billing additionally has a unique `SourceDocumentId`, so even a new event ID cannot bill the same WMS document twice.
