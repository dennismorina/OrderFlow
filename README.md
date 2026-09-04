# OrderFlow

[![CI](https://github.com/dennismorina/OrderFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/dennismorina/OrderFlow/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2025-CC2927)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-4.3-FF6600)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED)
![License](https://img.shields.io/badge/License-MIT-green)

A production-oriented order workflow sample demonstrating domain modeling, explicit business-state transitions, reliable messaging, idempotency and concurrency handling.

OrderFlow deliberately focuses on **business workflows and integration reliability** rather than becoming another generic CRUD API.

## What it demonstrates

- ASP.NET Core / .NET 10
- Explicit order state machine through domain methods
- Domain events
- Transactional Outbox Pattern
- RabbitMQ topic exchange
- Asynchronous fulfillment consumer
- Inbox-based consumer idempotency
- SQL Server 2025
- Entity Framework Core
- EF Core migrations
- Optimistic concurrency with SQL Server `rowversion`
- Audit trail for every status transition
- Idempotent external order creation
- Docker / Docker Compose
- Unit and integration tests
- End-to-end SQL Server + RabbitMQ CI smoke test
- Dependabot

## Workflow

```text
Created
   |
   v
Validated
   |
   v
Approved
   |
   v
Processing
   |
   v
Completed
```

Cancellation is allowed only before processing:

```text
Created   ------> Cancelled
Validated ------> Cancelled
Approved  ------> Cancelled

Processing -X-> Cancelled
Completed  -X-> any further transition
Cancelled  -X-> any further transition
```

Status changes are expressed as domain operations such as:

```csharp
order.Validate();
order.Approve();
order.StartProcessing();
order.Complete();
order.Cancel();
```

There is intentionally no generic `PUT /status` endpoint that can bypass the workflow rules.

## Reliable messaging

Approving an order raises a domain event.

The order change and the integration event are committed in the **same SQL Server transaction**:

```text
Order.Approve()
      |
      v
SQL transaction
├── UPDATE Orders
├── INSERT OrderStatusHistory
└── INSERT OutboxMessages
      |
      v
COMMIT
```

The API background publisher then delivers the outbox message to RabbitMQ:

```text
OutboxMessages
      |
      v
orderflow.events
(topic exchange)
      |
      | order.approved
      v
orderflow.fulfillment
      |
      v
Fulfillment Worker
```

The consumer records every processed `MessageId` in `InboxMessages`.

If RabbitMQ redelivers a message, the consumer recognizes it and does not execute the fulfillment operation twice.

## Architecture

```text
src/
├── OrderFlow.Api
├── OrderFlow.Application
├── OrderFlow.Contracts
├── OrderFlow.Domain
├── OrderFlow.Infrastructure
└── OrderFlow.FulfillmentWorker

tests/
├── OrderFlow.UnitTests
└── OrderFlow.IntegrationTests
```

Dependencies are intentionally kept directional:

```text
API ----------------------+
 |                        |
 v                        v
Application ----------> Domain

Infrastructure -------> Application
      |                 Domain
      +---------------> Contracts

FulfillmentWorker ----> Infrastructure
                    \--> Contracts
```

`OrderFlow.Contracts` contains the RabbitMQ integration contracts shared between producer and consumer without coupling the worker to API implementation details.

## Order model

An order contains:

```text
Order
├── Id
├── OrderNumber
├── ExternalOrderId
├── CustomerNumber
├── CustomerName
├── Status
├── CreatedAtUtc
├── UpdatedAtUtc
├── Version (rowversion)
└── Items
```

Each item contains:

```text
OrderItem
├── ProductNumber
├── Description
├── Quantity
└── UnitPrice
```

`ExternalOrderId` has a unique database index and is used for idempotent external order submission.

## API

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/orders` | Create an order |
| `GET` | `/api/orders` | List recent orders |
| `GET` | `/api/orders/{id}` | Get one order |
| `GET` | `/api/orders/{id}/history` | Read status history |
| `POST` | `/api/orders/{id}/validate` | Validate |
| `POST` | `/api/orders/{id}/approve` | Approve |
| `POST` | `/api/orders/{id}/start-processing` | Start processing |
| `POST` | `/api/orders/{id}/complete` | Complete |
| `POST` | `/api/orders/{id}/cancel` | Cancel before processing |

## Quick start with Docker

Requirements:

- Docker Desktop

Start everything:

```bash
docker compose up --build -d
```

Services:

| Service | Address |
|---|---|
| API | `http://localhost:8081` |
| Health | `http://localhost:8081/health` |
| OpenAPI | `http://localhost:8081/openapi/v1.json` |
| SQL Server | `localhost:1436` |
| RabbitMQ AMQP | `localhost:5673` |
| RabbitMQ Management | `http://localhost:15673` |

RabbitMQ development login:

```text
Username: orderflow
Password: orderflow_dev
```

> The passwords in this repository are development-only credentials for the disposable local Docker environment.

Stop the environment:

```bash
docker compose down
```

Remove the SQL Server volume as well:

```bash
docker compose down -v
```

## Create an order

```bash
curl -X POST http://localhost:8081/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "externalOrderId": "SHOP-2026-4711",
    "customerNumber": "C10001",
    "customerName": "Example Customer",
    "items": [
      {
        "productNumber": "ART-100",
        "description": "Mechanical Keyboard",
        "quantity": 2,
        "unitPrice": 129.90
      }
    ]
  }'
```

Submitting the same `externalOrderId` again returns the already existing order instead of creating a duplicate.

## Run the workflow

Use the returned order id:

```bash
curl -X POST http://localhost:8081/api/orders/<id>/validate
curl -X POST http://localhost:8081/api/orders/<id>/approve
curl -X POST http://localhost:8081/api/orders/<id>/start-processing
curl -X POST http://localhost:8081/api/orders/<id>/complete
```

After approval, open RabbitMQ Management and inspect:

```text
Exchange: orderflow.events
Queue:    orderflow.fulfillment
Routing:  order.approved
```

The fulfillment worker records the asynchronous handoff in SQL Server.

## Cancellation

Before processing:

```bash
curl -X POST http://localhost:8081/api/orders/<id>/cancel \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Customer requested cancellation"
  }'
```

Trying to cancel a `Processing`, `Completed` or already `Cancelled` order returns `409 Conflict`.

## Local .NET development

Start only the infrastructure:

```bash
docker compose up -d sqlserver rabbitmq
```

Run the API:

```bash
dotnet run --project src/OrderFlow.Api
```

Run the fulfillment worker in another terminal:

```bash
dotnet run --project src/OrderFlow.FulfillmentWorker
```

Local API:

```text
http://localhost:5090
```

## Database migrations

The API automatically applies EF Core migrations on startup.

To create another migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/OrderFlow.Infrastructure
```

The design-time context uses:

```text
localhost:1436
```

You can override it through:

```text
ORDERFLOW_CONNECTION_STRING
```

## Optimistic concurrency

`Orders.Version` is a SQL Server `rowversion` column.

EF Core treats it as a concurrency token. If two processes load the same order and both attempt conflicting updates, the stale update fails instead of silently overwriting the newer state.

The API translates this into:

```text
409 Conflict
```

## Audit trail

Every successful status transition creates an `OrderStatusHistory` entry.

Example:

```text
Created     -> Validated
Validated   -> Approved
Approved    -> Processing
Processing  -> Completed
```

A cancellation also stores the supplied reason.

## Testing

Run all tests:

```bash
dotnet test --solution OrderFlow.sln --configuration Release
```

The unit tests focus on domain behavior:

- valid state transitions
- invalid state transitions
- cancellation rules
- final states
- domain-event creation

The integration tests exercise the HTTP API and idempotent order creation.

GitHub Actions additionally runs a real Docker-based end-to-end smoke test with:

```text
SQL Server 2025
RabbitMQ
OrderFlow API
Fulfillment Worker
```

The CI test creates an order, validates it, approves it and verifies that the RabbitMQ message was consumed into `FulfillmentRecords`.

## Technology stack

| Area | Technology |
|---|---|
| Language | C# |
| Runtime | .NET 10 |
| API | ASP.NET Core |
| Database | SQL Server 2025 |
| ORM | Entity Framework Core 10 |
| Messaging | RabbitMQ 4.3 |
| RabbitMQ Client | RabbitMQ.Client 7 |
| Reliability | Outbox + Inbox |
| Concurrency | SQL Server rowversion |
| Testing | xUnit v3 |
| Coverage | Coverlet MTP |
| Containers | Docker / Docker Compose |
| CI | GitHub Actions |
| Dependency Updates | Dependabot |

## Design goals

OrderFlow is deliberately limited to one meaningful business workflow.

It is not intended to be a complete ERP system.

The project focuses on demonstrating:

- business rules in the domain model
- explicit workflow transitions
- transactional event persistence
- asynchronous integration through RabbitMQ
- at-least-once delivery safety
- idempotent message consumption
- optimistic concurrency
- operational auditability
- automated end-to-end verification

## License

MIT.
