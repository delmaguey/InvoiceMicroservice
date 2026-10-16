# Invoice Microservices with RabbitMQ

A small .NET example of asynchronous messaging between two microservices using [RabbitMQ](https://www.rabbitmq.com/). The **Invoice** service publishes `InvoiceCreated` messages and the **Payment** service consumes them.

## Architecture

```
┌──────────────────────┐      ┌─────────────────────────────┐      ┌──────────────────────┐
│  InvoiceMicroservice │      │          RabbitMQ           │      │  PaymentMicroservice │
│      (Producer)      │ ───▶ │  default exchange ("")      │ ───▶ │      (Consumer)      │
│                      │      │  queue: invoice-service     │      │                      │
└──────────────────────┘      └─────────────────────────────┘      └──────────────────────┘
                     JSON-serialized InvoiceCreated messages
```

| Project | Role | Description |
| --- | --- | --- |
| [InvoiceMicroservice](InvoiceMicroservice/) | Producer | Console app. Each key press creates a random invoice and publishes it to the `invoice-service` queue. Press `q` to exit. |
| [PaymentMicroservice](PaymentMicroservice/) | Consumer | Console app. Listens on the `invoice-service` queue and prints the invoice number of each message it receives. |
| [MessageContracts](MessageContracts/) | Shared library | Message types (`InvoiceCreated`, `InvoiceToCreate`, `InvoiceItems`) shared by both services. |

### Message contract

`InvoiceCreated` is serialized as JSON with `System.Text.Json`:

```json
{
  "InvoiceNumber": 54321,
  "InvoiceData": {
    "CustomerNumber": 12345,
    "InvoiceItems": [
      {
        "Description": "Item 1",
        "Price": 100.0,
        "ActualMileage": 50.0,
        "BaseRate": 10.0,
        "IsOversized": false,
        "IsRefrigerated": false,
        "IsHazardousMaterial": false
      }
    ]
  }
}
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started/) installed and running. It is required to start the RabbitMQ instance below.

## Getting started

### 1. Start RabbitMQ

> **Docker must be installed and running** to use this command.

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management
```

- AMQP port: `5672`
- Management UI: <http://localhost:15672> (default login `guest` / `guest`)

### 2. Run the Payment service (consumer)

```bash
dotnet run --project PaymentMicroservice
```

### 3. Run the Invoice service (producer)

In a second terminal:

```bash
dotnet run --project InvoiceMicroservice
```

Press any key to create an invoice. The Payment terminal should print something like:

```
 [x] Received invoice number: 54321
```

## Configuration

Both services connect to RabbitMQ at `localhost` with the default credentials. To use a different server, change the `HostName` in the `ConnectionFactory` at the top of each service's `Program.cs`:

```csharp
var factory = new ConnectionFactory { HostName = "your-rabbitmq-host" };
```

The queue name (`invoice-service`) must be the same in both services.

## RabbitMQ details

- **Client library:** [RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client) 7.2.2 (async API).
- **Queue:** `invoice-service`, declared by both services (non-durable, non-exclusive, no auto-delete), so start order doesn't matter. Because it is non-durable, the queue and any unconsumed messages are lost if the broker restarts.
- **Exchange:** the default (nameless) exchange, with the queue name as the routing key. A commented-out fanout exchange in the producer shows how to broadcast to multiple services instead.
- **Acknowledgements:** the consumer uses `autoAck: true`, so messages are removed from the queue as soon as they are delivered. A crash mid-processing loses the message.
- **Competing consumers:** run several Payment instances and RabbitMQ distributes messages between them round-robin.

## Possible next steps

- Make the queue durable and publish persistent messages to survive broker restarts.
- Switch to manual acknowledgements (`autoAck: false`) with `BasicAckAsync` / `BasicNackAsync` and a dead-letter queue.
- Replace the default exchange with a fanout or topic exchange so other services can subscribe to invoice events.
- Read the RabbitMQ host and credentials from configuration or environment variables.
