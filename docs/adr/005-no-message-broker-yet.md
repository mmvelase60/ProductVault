# ADR-005: Do not introduce RabbitMQ or Kafka yet

**Status:** Accepted

## Decision

Do not add RabbitMQ or Kafka to the current solution.

## Rationale

The application has no high-volume event stream, independent services, or long-running asynchronous workflow. Adding a broker would introduce operational complexity without meeting an active requirement.

## Consequence

If imports become large or image processing/email notifications require retries, introduce RabbitMQ as a focused background-work queue. Consider Kafka only for genuine event-streaming requirements.
