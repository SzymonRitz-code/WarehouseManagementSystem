# ADR-005: Avoid a universal generic repository

Status: Accepted

## Context

A universal `Repository<TEntity>` often becomes a CRUD-shaped wrapper around an ORM. It creates several risks:

- it encourages application services to treat aggregates as interchangeable records, bypassing aggregate-specific invariants;
- a large collection of generic query methods either leaks `IQueryable` and persistence concerns, or reimplements an incomplete ORM abstraction;
- it exposes mutation methods to consumers that only need to read data;
- it makes specialised queries, loading strategies and performance characteristics difficult to express and review.

The project needs a small amount of shared repository behaviour, but document, stock and warehouse workflows require aggregate-specific contracts.

## Decision

Keep repository interfaces specific to their aggregates, for example `IDocumentRepository` and `IStockRepository`. Do not expose a universal repository with arbitrary filtering, sorting, eager-loading or persistence operations.

Use generic interfaces only as narrow capability contracts. `IRepository<TEntity>` contains the shared write capabilities; read-oriented consumers depend on the smaller `IReadOnlyRepository<TEntity>` contract described in ADR-006.

## Alternatives

- Expose `DbSet<TEntity>` or `IQueryable<TEntity>` outside infrastructure.
- Build a feature-rich `IGenericRepository<TEntity>` with generic CRUD and query methods.
- Duplicate every basic lookup method in every repository interface.

## Consequences

Repository APIs remain intentional and reflect the domain language. Query optimisation and aggregate loading stay behind repository implementations. There is more interface code than in a universal CRUD wrapper, but dependencies show the capability a service actually needs.

## Impact

New repository methods must be added only when a concrete use case needs them. Generic abstractions must not grow into a second ORM API.
