# WarehouseManagementSystem

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

## Development auth workaround

The API currently has a development-only JWT/certificate workaround for the local IdentityServer at `https://localhost:44380`.

Known technical debt:

- authority, metadata address, issuer and audience are still hardcoded in `Program.cs`;
- JWKS keys are resolved manually;
- local certificate validation is bypassed in development;
- auth diagnostics still use console output.

Before production hardening, move auth values to configuration, remove the certificate bypass, replace console diagnostics with structured logging and prefer the standard JWT bearer metadata/JWKS flow where possible.
