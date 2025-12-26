# QIE Shard Management

This repository provides a Clean Architecture solution for QIE Shard Management. It includes a REST API host and three background workers that coordinate enrollment ingest, shard lifecycle management, and platform event notifications.

## Architecture

The solution follows Clean Architecture boundaries:

- `QIE.SM.SharedKernel`: shared primitives and cross-cutting models.
- `QIE.SM.Domain`: core domain entities and enums.
- `QIE.SM.Contracts`: integration contracts and message envelopes.
- `QIE.SM.Application`: ports, configuration models, and application services.
- `QIE.SM.Infrastructure`: Kafka, MongoDB, routing, provisioning, and storage implementations.
- `QIE.SM.Api.Middleware`: API-specific middleware for correlation IDs, logging, and trusted headers.
- `QIE.SM.Api`: REST API host (no authentication, header-based authorization only).
- `QIE.SM.Workers`: background services for enrollment ingest, shard management, and event notification.

Clean Architecture dependencies are enforced in project references:

- Domain depends only on SharedKernel.
- Application depends on Domain + Contracts + SharedKernel.
- Infrastructure depends on Application + Domain + Contracts + SharedKernel.
- Api depends on Application + Contracts + Api.Middleware + Infrastructure + SharedKernel.
- Workers depends on Application + Infrastructure + Contracts + SharedKernel.

For a full architecture specification, see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Logging and ELK

Both the API and workers emit JSON logs to standard output using the built-in JSON console formatter. These logs are compatible with log collectors such as Filebeat or Fluent Bit for ingestion into Elasticsearch (ELK).

Key behaviors:

- Correlation IDs are generated from `X-Correlation-Id` and included in log scopes.
- Request logging captures method, path, status code, and duration.
- Worker logs capture Kafka processing steps and error details.

To ship logs to ELK in production, configure a log collector to read container output or log files and forward to Elasticsearch. No additional application code is required.

## API Authorization

The API host does not perform authentication. It builds `HttpContext.User` from trusted headers:

- `X-User`: username
- `X-Roles`: comma-separated roles (`sm-admin`, `sm-operator`)

Policies:

- `SmAdmin`: requires `sm-admin`
- `SmOperator`: requires `sm-operator` or `sm-admin`

If the headers are missing, the request is treated as anonymous.

## Topics and Message Flows

Kafka topic usage:

- `qie.enrollment.events`: Enrollment committed events
- `qie.shard.ingest.commands`: Ingest commands for shards
- `qie.sm.commands`: Shard lifecycle commands
- `qie.sm.events`: Shard lifecycle events
- `qie.platform.events`: Platform-wide events from all microservices

### Enrollment Ingest Worker

1. Subscribes to `qie.enrollment.events`.
2. Reads `EnrollmentCommitted` from `EventEnvelope<T>`.
3. Loads manifest JSON from MongoDB using `ManifestId`.
4. Resolves the target shard via consistent hashing on active shards.
5. Publishes `ShardIngestCommand` to `qie.shard.ingest.commands` with message key equal to `Identifier`.

### Shard Management Worker

1. Subscribes to `qie.sm.commands`.
2. Executes provisioning via `IShardProvisioner` (Kubernetes or agent gRPC stub).
3. Updates shard registry status transitions.
4. Publishes shard events to `qie.sm.events` with `EventEnvelope<T>`.
5. Commits offsets only after successful processing.

### Event Notification Worker

1. Subscribes to `qie.platform.events`.
2. Accepts `EventEnvelope<JsonElement>` to handle any schema.
3. Stores notifications in `IEventNotificationStore`.
4. Applies optional filters (source, type, severity).
5. Commits offsets after storing.

## Running the API

From the repository root:

```
dotnet run --project src/QIE.SM.Api
```

Sample requests:

```
curl -H "X-User: admin" -H "X-Roles: sm-admin" http://localhost:5000/api/sm/shards/fleet-overview
```

```
curl -X POST -H "Content-Type: application/json" -H "X-User: admin" -H "X-Roles: sm-admin" \
  -d '{"shardId":"shard-01","modality":"face","capacity":1000}' \
  http://localhost:5000/api/sm/shards
```

```
curl -X POST -H "X-User: admin" -H "X-Roles: sm-admin" \
  http://localhost:5000/api/sm/shards/shard-01/start
```

Liveness:

```
curl http://localhost:5000/api/health/live
```

## Running the Workers

From the repository root:

```
dotnet run --project src/QIE.SM.Workers
```

Configure Kafka and MongoDB in `src/QIE.SM.Workers/appsettings.json` before running.

## Configuration

Both API and Workers read configuration from `appsettings.json`.

Kafka:

- `BootstrapServers`
- `EnrollmentIngestConsumerGroupId`
- `ShardManagementConsumerGroupId`
- `EventNotificationConsumerGroupId`
- `Topics.*`

Mongo:

- `ConnectionString`
- `Database`
- `ManifestsCollection`

Event notification filter:

- `EventNotificationFilter.AllowedSources`
- `EventNotificationFilter.AllowedEventTypes`
- `EventNotificationFilter.AllowedSeverities`

## Solution Structure

```
./src
  QIE.SM.Api
  QIE.SM.Api.Middleware
  QIE.SM.Application
  QIE.SM.Contracts
  QIE.SM.Domain
  QIE.SM.Infrastructure
  QIE.SM.SharedKernel
  QIE.SM.Workers
QIE.ShardManagement.sln
```
