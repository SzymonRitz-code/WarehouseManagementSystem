# ADR-002: Transactional outbox

Status: Accepted

## Context

Writing a confirmed document and publishing directly to RabbitMQ cannot be one transaction.

## Decision

Store an `OutboxMessages` intent with the WMS database change, then publish it asynchronously with broker confirms.

## Alternatives

Direct publish after saving, distributed transactions, or polling domain tables.

## Consequences

The intent survives broker outages and is observable. It does not guarantee exactly once: a crash between broker confirmation and marking the row published can duplicate an event.

## Impact

Consumers must remain idempotent and operators must monitor failed or abandoned outbox rows.
