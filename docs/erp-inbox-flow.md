# FakeERP ↔ WMS Inbox flow

`ERP order + ERP Outbox -> RabbitMQ -> WMS Inbox + WMS Document -> document.confirmed -> ERP ProcessedMessage + order Confirmed`

- **Outbox** reliably records intent to send after a local state change.
- **Inbox** reliably and idempotently accepts a message before the WMS local state change.
- **ProcessedMessages** is consumer-side technical deduplication for FakeERP confirmations.
- **ExternalOrderId** is business idempotency: a new transport message must not create another WMS document.

The contracts project contains transport DTOs only. `DocumentConfirmedIntegrationEvent` remains compatible; WMS preserves the ERP `CorrelationId` in `ErpOrderImports` rather than placing ERP fields on the WMS document aggregate.

## Follow-up refactorings

- bounded-context boundaries between ERP and WMS;
- anti-corruption layer (ACL);
- command versus event naming;
- contract versioning;
- document aggregate boundary;
- transient versus permanent retry classification;
- reconciliation.
