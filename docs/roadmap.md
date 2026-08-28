# Feature roadmap

## Completed

- [x] Secure Identity registration and login
- [x] User-isolated category and product management
- [x] Paging, image upload, Excel import/export
- [x] Auditing, validation, and optimistic concurrency
- [x] Protected API endpoints
- [x] Unit tests, GitHub Actions CI/CD, and local monitoring

## Next release: usability and resilience

- [ ] Product search, sorting, and category filtering
- [ ] Category delete/deactivate workflow with clear product-impact messaging
- [ ] Image replacement/removal option in the edit screen
- [ ] Downloadable Excel import template and per-row error report
- [ ] Integration tests for authorization and concurrency paths

## Future: operational maturity

- [ ] Role-based administration and account management
- [ ] Structured logging with correlation IDs
- [ ] Alert thresholds in Grafana for sustained errors or slow requests
- [ ] Database health check with a dependency probe
- [ ] Automated database backup/migration release process

## Future: integration scale

- [ ] JWT/OAuth only when a separate SPA, mobile app, or external client is introduced
- [ ] Background queue for large imports or image processing when the workload becomes asynchronous
- [ ] Cloud deployment target when there is a concrete hosting requirement

The roadmap deliberately does not add Kafka or RabbitMQ prematurely. A message broker should follow a demonstrated asynchronous or cross-service need, not precede it.
