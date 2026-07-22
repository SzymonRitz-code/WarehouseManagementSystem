# ADR-003: Domain events vs integration events

Status: Accepted

## Context

The WMS domain model contains behavior and persistence relations that are not a stable external contract.

## Decision

Keep domain events and entities internal. Publish small integration contracts such as `DocumentConfirmedIntegrationEvent` through the outbox.

## Alternatives

Publish EF/domain entities directly, or duplicate every internal event externally.

## Consequences

External messages are stable and intentional but require mapping and version discipline. Not every internal fact becomes an integration event.

## Impact

Contract changes must be backward-conscious and never expose entity graphs.
