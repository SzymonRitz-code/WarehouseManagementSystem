# ADR-006: Restrict generic repository capabilities with IReadOnlyRepository

Status: Accepted

## Context

Depending on a full `IRepository<TEntity>` for a query-only use case grants unnecessary mutation capabilities such as update and delete. This weakens the dependency boundary and makes accidental writes easier to introduce. It is a common side effect of a generic repository base interface.

Audit logs illustrate a different persistence profile: entries are append-only. They must be read and added, but must never be updated or deleted through the repository contract.

## Decision

Use `IReadOnlyRepository<TEntity>` as the narrow dependency for consumers that do not need update or delete operations. `IAuditLogRepository` depends on this contract, so audit-log code cannot call `Update`, `UpdateRange` or `Delete`.

This resolves the important mutability problem of the generic repository base: a caller receives only the capabilities required by its use case instead of the full CRUD surface.

`IReadOnlyRepository<TEntity>` currently also declares `Add`. This is deliberate for the append-only audit-log use case, so the name means *read and append*, not a mathematically pure read-only/CQRS contract.

## Alternatives

- Inject the full `IRepository<TEntity>` everywhere.
- Expose `DbContext` directly to query services.
- Create separate repository interfaces for every read method, with no shared base contract.
- Remove `Add` from `IReadOnlyRepository<TEntity>` immediately and require a separate append-only contract.

## Consequences

The interface prevents update and delete operations where they are not legitimate. It also communicates intent during review and makes the audit trail append-only at the contract level.

The current name has a limitation: `Add` means the contract is not strictly read-only. If more append-only use cases appear, introduce `IAppendOnlyRepository<TEntity>` and let `IAuditLogRepository` compose `IReadOnlyRepository<AuditLog>` with that interface. Until then, the smaller existing contract avoids unnecessary abstraction while preserving the relevant safety boundary.

## Impact

New services should depend on the narrowest repository capability that satisfies their use case. Write workflows continue to use aggregate-specific repository interfaces; query and audit-log workflows should not receive update or delete capabilities.
