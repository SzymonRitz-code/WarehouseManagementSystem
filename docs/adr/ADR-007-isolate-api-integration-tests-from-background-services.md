# ADR-007: Isolate API integration tests from production background services

Status: Accepted

## Context

API integration tests boot the ASP.NET Core application in-process through `WebApplicationFactory<Program>` and send requests through `TestServer`. The test host needs a SQL Server, which is supplied by a Testcontainers fixture.

The production composition root also registers `IHostedService` implementations for database seeding, reservation expiration, the outbox publisher and ERP/RabbitMQ consumers. A GitHub-hosted test runner does not provide the production RabbitMQ endpoint at `localhost:5672`. If a worker fails while the test host starts, the host can stop and dispose `TestServer`. The HTTP tests then fail with `ObjectDisposedException`, even though the controller or middleware under test is not the root cause.

The earlier test factory also combined ownership of the SQL container, the web host and xUnit lifecycle callbacks. This made resource ownership and disposal order difficult to reason about.

## Decision

Use a test-specific composition root in `WmsApiFactory`.

- `ApiFixture` owns the SQL Server Testcontainer for the `Api` test collection.
- `WmsApiFactory` owns only the in-process API host and receives the fixture connection string.
- Each API test creates and disposes its own `WmsApiFactory` and `HttpClient`.
- The test factory removes all production `IHostedService` registrations with `services.RemoveAll<IHostedService>()`.

This makes the API HTTP tests responsible for the HTTP pipeline, authentication, routing and persistence through SQL Server, but not for starting RabbitMQ consumers, periodic jobs or production seeding.

## Alternatives

- Run RabbitMQ, Redis and all other production dependencies in CI through Docker Compose or Testcontainers, and keep hosted services enabled.
- Add a separate configuration flag to every hosted service and disable each worker individually for tests.
- Mock RabbitMQ connection abstractions while retaining the workers in the test host.
- Skip API integration tests in CI.

## Consequences

The HTTP integration tests no longer depend on unavailable production infrastructure and avoid a failed background worker shutting down the test host. Resource ownership is explicit: the database fixture outlives the API factory, and the API factory outlives its HTTP client.

The trade-off is that these tests do not verify background processing, outbox publishing or RabbitMQ consumer behavior. Those concerns require dedicated messaging integration tests with a RabbitMQ container, or end-to-end tests that provision the complete runtime topology.

## Impact

New HTTP integration tests should use the `Api` collection and must not re-register production hosted services unless the test explicitly provisions their dependencies. New workers should be covered by their own integration tests rather than becoming an implicit dependency of controller tests.
