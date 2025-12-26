# QIE Shard Management Architecture Specification

## Purpose

This document describes the architecture, runtime responsibilities, and operational characteristics of the QIE Shard Management solution. It is intended to be versioned alongside the codebase and serve as the source of truth for system design decisions.

## Scope and Goals

- Coordinate shard lifecycle management for QIE enrollment workloads.
- Provide an API for shard management operations.
- Process enrollment events, shard commands, and platform events through workers.
- Maintain clear service boundaries using Clean Architecture principles.

## Solution Structure

The solution is organized into projects with explicit dependencies:

- **QIE.SM.SharedKernel**: shared primitives, value objects, and cross-cutting models.
- **QIE.SM.Domain**: core domain entities and enums.
- **QIE.SM.Contracts**: integration contracts and message envelopes.
- **QIE.SM.Application**: ports, configuration models, and application services.
- **QIE.SM.Infrastructure**: Kafka, MongoDB, routing, provisioning, and storage implementations.
- **QIE.SM.Api.Middleware**: API middleware (correlation IDs, logging, trusted headers).
- **QIE.SM.Api**: REST API host.
- **QIE.SM.Workers**: background services for enrollment ingest, shard management, and event notification.

Dependency flow is enforced by project references:

- Domain depends only on SharedKernel.
- Application depends on Domain + Contracts + SharedKernel.
- Infrastructure depends on Application + Domain + Contracts + SharedKernel.
- Api depends on Application + Contracts + Api.Middleware + Infrastructure + SharedKernel.
- Workers depends on Application + Infrastructure + Contracts + SharedKernel.

## System Context

External systems and services:

- **Kafka** for event streams and command routing.
- **MongoDB** for enrollment manifests and event notifications.
- **Kubernetes or Agent gRPC** for shard provisioning (via `IShardProvisioner`).

## Runtime Components

### API Host (`QIE.SM.Api`)

Responsibilities:

- Exposes REST endpoints for shard lifecycle operations.
- Publishes shard commands/events via Kafka.
- Uses trusted headers to construct `HttpContext.User` and enforce authorization policies.

### Workers (`QIE.SM.Workers`)

Three background services run in the worker host:

1. **Enrollment Ingest Worker**
   - Consumes enrollment committed events from Kafka.
   - Loads manifests from MongoDB.
   - Resolves target shard using routing/consistent hashing.
   - Emits shard ingest commands.

2. **Shard Management Worker**
   - Consumes shard lifecycle commands.
   - Invokes `IShardProvisioner` (Kubernetes or agent mode).
   - Updates shard registry state transitions.
   - Emits shard lifecycle events.

3. **Event Notification Worker**
   - Consumes platform events.
   - Stores notifications in MongoDB.
   - Applies optional filters by source/type/severity.

## Messaging and Data Flow

### Kafka Topics

Configured topics (see `appsettings.json`):

- `qie.enrollment.events`
- `qie.shard.ingest.commands`
- `qie.sm.commands`
- `qie.sm.events`
- `qie.platform.events`

### Core Event Flow

1. Enrollment service publishes to `qie.enrollment.events`.
2. Enrollment Ingest Worker resolves target shard and publishes to `qie.shard.ingest.commands`.
3. API or external systems publish shard lifecycle commands to `qie.sm.commands`.
4. Shard Management Worker provisions shard capacity and publishes lifecycle events to `qie.sm.events`.
5. Platform services publish to `qie.platform.events` and notifications are stored by the Event Notification Worker.

## Data Stores

MongoDB collections:

- `enrollment_manifests` for enrollment manifest documents.
- Event notification storage (collection name configured via infrastructure settings).

## API Authorization Model

- API does **not** authenticate users.
- Trusted headers (`X-User`, `X-Roles`) are used to construct the principal.
- Policies:
  - `SmAdmin`: requires `sm-admin` role.
  - `SmOperator`: requires `sm-operator` or `sm-admin`.

## Configuration

Configuration is read from `appsettings.json` for both API and workers.

Key sections:

- `Kafka`: bootstrap servers, consumer group IDs, and topic names.
- `Mongo`: connection string, database, and collection names.
- `EventNotificationFilter`: optional filters for worker notifications.
- `ShardProvisioner.Mode`: selects provisioning implementation (e.g., `Kubernetes`).

## Observability

- API and workers emit JSON logs to standard output.
- Correlation IDs are set via `X-Correlation-Id` and included in log scopes.
- Logs are compatible with ELK-compatible collectors (Filebeat/Fluent Bit).

## Deployment Notes

- API and worker hosts can be deployed independently.
- Kafka and MongoDB must be reachable from both services.
- Provisioner dependencies (e.g., Kubernetes cluster access) must be available to the Shard Management Worker.

## Failure Handling and Reliability

- Kafka offsets are committed only after successful processing in workers.
- Errors are logged with context and correlation identifiers.
- Idempotency should be considered when reprocessing commands/events.

## Extensibility

- New shard provisioners can be added by implementing `IShardProvisioner` in Infrastructure.
- New Kafka topics and message contracts should be added to Contracts and Application layers.
- Additional workers can be registered in the worker host without changing the API.
