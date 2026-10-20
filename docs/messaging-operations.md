# Messaging operations

The WMS transaction stores domain changes and an outbox record together. The publisher sends pending records to `wms.events`; a broker-confirmed publish marks the outbox record as published. Broker outages leave it retryable. Publication failures have a maximum of three attempts by default. Retry delay grows exponentially from 30 seconds with 20% jitter (base capped at one hour). Exhausted messages remain `Abandoned` with `LastError` and an error log for operator investigation.

Consumers use at-least-once delivery. `MessageId`, type, correlation ID and creation time travel in AMQP properties. A durable `ProcessedMessages` record (`Consumer`, `MessageId`) identifies duplicates; a duplicate is ACKed without repeating business work.

On a consumer error, the delivery is copied to `wms.events.retry` with `x-wms-retry-count`, `x-wms-last-error` and `x-wms-last-attempt-at`. The retry queue waits 10 seconds (TTL), then dead-letters it back to the main exchange. After three retries the consumer rejects it without requeue and RabbitMQ routes it to `shipping.document-confirmed.dlq` through `wms.events.dlx`. This avoids a hot requeue loop.

Use the RabbitMQ management UI to inspect the DLQ. Its message properties and headers identify the original message, retry count and final error. To replay manually, republish the original payload and properties to `wms.events` with routing key `document.confirmed`. Preserve the original `MessageId`: changing it would bypass idempotency and can repeat the business effect. No automatic DLQ replay is provided.

This design does not provide exactly-once delivery. A crash after a durable database commit but before ACK can redeliver a message; the idempotent consumer makes that safe. A crash after RabbitMQ confirms publishing but before the outbox status update can also republish an event.

## ERP -> WMS Inbox

WMS owns `wms.erp-document-create`, `.retry` and `.dlq`; FakeERP owns `erp.document-confirmed`, `.retry` and `.dlq`. Each consumer uses durable queues, manual ACK, TTL retry and a bounded retry count. A permanent command validation/conflict failure is rejected directly to the WMS DLQ.

`InboxMessages(Consumer, MessageId)` is WMS's technical receive marker. `ErpOrderImports.ExternalOrderId` is the separate business idempotency guard: a different message ID for an already imported equivalent order ACKs without another document; differing payload data is a permanent conflict. The Inbox row, import map and document are committed in one SQL transaction. The original ERP correlation ID is stored on the import map and copied to `DocumentConfirmedIntegrationEvent` on confirmation.

## Billing demonstration

Shipping owns `shipping.document-confirmed`, `.retry` and `.dlq`. Billing independently owns `billing.document-confirmed`, `.retry` and `.dlq`; after three retries, a Billing delivery is routed to `billing.document-confirmed.dlq`. WMS only declares `wms.events` and the shared dead-letter exchange. Each consumer declares its own queues.

`FakeBilling` consumes `document.confirmed` into its own SQLite database. Only a `WZ` is eligible: another document type is marked in `ProcessedMessages`, logged as `ignored`, and ACKed without an invoice. The invoice and processed-message marker are committed in one local transaction. `(Consumer, MessageId)` gives technical idempotency. The unique `FakeInvoices.SourceDocumentId` is the business guard: a second, different event for the same WMS document logs `duplicate` and cannot create another invoice.


## Publisher reliability and operations

The publisher keeps a connection and confirmed channel across polls. A broken session is disposed;
subsequent iterations reconnect with exponential delay and jitter. Connections use automatic recovery,
a 30-second heartbeat and the client name `wms-api`.

Publication is mandatory: an unroutable return or a missing broker confirmation leaves the row
retryable. Broker confirmation proves acceptance/routing, not that every intended downstream binding
exists or that a consumer completed its work. Declare consumer topology before sending business traffic.

SQL Server batch selection uses `UPDLOCK, READPAST, READCOMMITTEDLOCK` inside the transaction that
saves publication status. Concurrent instances skip locked rows. Process death rolls back the transaction
and releases ownership; there is no lease to expire. The tradeoff is holding database locks during broker
I/O (bounded by the batch size and confirmation timeout). This remains at-least-once delivery: a crash
after broker acceptance but before the database commit can publish the same event again.

`Messaging:PublishedRetentionDays` defaults to 30. Each poll deletes at most 500 old **Published** rows;
pending, failed and abandoned records are retained for investigation. `ProcessedMessages` identifiers
are retained indefinitely because operator replay has no maximum age. They contain no message payload.
Do not delete deduplication markers without first defining and enforcing a bounded replay window.

The `Wms.Outbox` .NET meter exposes `wms.outbox.publish.duration` (milliseconds),
`wms.outbox.oldest.age` (seconds), `wms.outbox.backlog` (pending/failed count) and
`wms.outbox.failures`. Connect a .NET metrics collector to export these instruments. Abandoned
publications emit error logs; alert delivery depends on the deployment's logging/monitoring system.

## Optional local shipping projection

The normal Compose flow uses the external FakeShipping database. Set
`Messaging:LocalShippingProjectionEnabled=true` to additionally enable the WMS-local demonstration
consumer and reconciliation job. It owns `wms.local.shipping.document-confirmed`, its `.retry` and
`.dlq` queues and `wms.local.shipping.retry` exchange, so it never competes with FakeShipping.
Retry dead-lettering uses the default exchange to return only to the local queue.

The local consumer validates document/actor identifiers, required fields, supported document type,
positive item quantities and contract versions before writes. Invalid JSON/contracts go directly to
its DLQ; processing failures use bounded delayed retry. Retry copies must be confirmed and routable
before the original delivery is ACKed. If that publication fails, the original remains retryable.
Both WMS consumers use asynchronous RabbitMQ dispatch, so database waits do not block a dispatcher thread.

`DocumentConfirmedIntegrationEvent` now includes `SchemaVersion=1` and `AggregateVersion=1`.
Missing fields retain version 1 for compatibility with existing payloads. The domain permits one
confirmation per document; a unique local shipment `DocumentId` is the permanent checkpoint for
that immutable version. Other versions are rejected to DLQ instead of overwriting a newer state.
There is no cancellation/update event stream in this contract; introducing one requires a new lifecycle
contract and explicit ordering/gap policy rather than pretending confirmation events form such a stream.

Every five minutes the local reconciler compares documents with a confirmation timestamp against local
shipments. It repairs at most 100 missing rows per pass from the authoritative document snapshot;
existing rows are untouched. Running it again is safe, and the document unique key arbitrates races
with message delivery. This rebuilds a local read projection only: it neither republishes events nor
creates a shipment in external FakeShipping. External system reconciliation requires that system's API.
