# ADR-004: At-least-once delivery and idempotent consumers

Status: Accepted

## Context

Broker ACKs and database commits can fail or be interrupted independently.

## Decision

Use at-least-once delivery. Consumers persist `ProcessedMessages` keyed by consumer and `MessageId` in the same save as the business effect. A unique index is the final race-condition guard; retry uses TTL queues and exhausted messages go to a DLQ.

## Alternatives

Claim exactly-once delivery, accept duplicates, or requeue forever.

## Consequences

Duplicates are harmless for supported consumers, but retention and manual replay require care. Manual replay preserves `MessageId`; a new ID would bypass deduplication.

## Impact

New consumers need a durable idempotency key, transactional business effect where possible, and a documented DLQ procedure.
