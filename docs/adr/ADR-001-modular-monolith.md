# ADR-001: Modular monolith

Status: Accepted

## Context

WMS currently deploys its API, domain and infrastructure together while keeping catalog, inventory, documents and warehouse concerns separated in code.

## Decision

Keep a modular monolith. Potential future extraction boundaries include shipping integration and inventory workflows, when independent ownership or scaling justifies it.

## Alternatives

Microservices now, or a single unstructured application.

## Consequences

This keeps local development, database transactions and change coordination simple. It delays independent deployment and requires preserving module boundaries so later extraction remains possible. Early splitting would add broker, deployment and distributed-data costs before there is evidence they are needed.

## Impact

New cross-module work should use explicit contracts and avoid direct infrastructure coupling.
