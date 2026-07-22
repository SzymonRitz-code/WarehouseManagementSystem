# Messaging operations

The WMS transaction stores domain changes and an outbox record together. The publisher sends pending records to `wms.events`; a broker-confirmed publish marks the outbox record as published. Broker outages leave it retryable. Publication failures are retried three times with a 30-second delay, then remain `Abandoned` with `LastError` for operator investigation.

Consumers use at-least-once delivery. `MessageId`, type, correlation ID and creation time travel in AMQP properties. A durable `ProcessedMessages` record (`Consumer`, `MessageId`) identifies duplicates; a duplicate is ACKed without repeating business work.

On a consumer error, the delivery is copied to `wms.events.retry` with `x-wms-retry-count`, `x-wms-last-error` and `x-wms-last-attempt-at`. The retry queue waits 10 seconds (TTL), then dead-letters it back to the main exchange. After three retries the consumer rejects it without requeue and RabbitMQ routes it to `shipping.document-confirmed.dlq` through `wms.events.dlx`. This avoids a hot requeue loop.

Use the RabbitMQ management UI to inspect the DLQ. Its message properties and headers identify the original message, retry count and final error. To replay manually, republish the original payload and properties to `wms.events` with routing key `document.confirmed`. Preserve the original `MessageId`: changing it would bypass idempotency and can repeat the business effect. No automatic DLQ replay is provided.

This design does not provide exactly-once delivery. A crash after a durable database commit but before ACK can redeliver a message; the idempotent consumer makes that safe. A crash after RabbitMQ confirms publishing but before the outbox status update can also republish an event.
