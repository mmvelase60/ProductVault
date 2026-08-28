# ADR-004: Use local Prometheus and Grafana

**Status:** Accepted

## Decision

Expose development-only Prometheus metrics and provide Prometheus/Grafana through Docker Compose.

## Rationale

This adds practical observability without cloud cost or a deployment dependency. It gives useful visibility into HTTP health and catalogue activity during demos and development.

## Consequence

The stack is not a production deployment. Its local credentials and self-signed certificate exception must not be reused in a shared environment.
