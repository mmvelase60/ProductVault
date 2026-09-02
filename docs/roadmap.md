# Feature roadmap

## Completed

- [x] Secure Identity registration and login
- [x] User-isolated category and product management
- [x] Paging, image upload, Excel import/export
- [x] Auditing, validation, and optimistic concurrency
- [x] Protected API endpoints
- [x] Role-based account directory and profile management
- [x] Downloadable import template and row-level import error report
- [x] Unit/integration tests, GitHub Actions CI, and local monitoring

## Next release: usability and resilience

- [x] Product search, sorting, and category filtering
- [ ] Category delete/deactivate workflow with clear product-impact messaging
- [ ] Image replacement/removal option in the edit screen
- [ ] More concurrency tests around simultaneous product and stock updates
- [ ] Product-image validation integration coverage

## Future: operational maturity

- [ ] Structured logging with correlation IDs
- [ ] Alert thresholds in Grafana for sustained errors or slow requests
- [ ] Database health check with a dependency probe
- [ ] Automated database backup/migration release process

## Future: integration scale

- [ ] OAuth/OpenID Connect when a third-party identity provider or production SSO requirement is introduced
- [ ] Background queue for large imports or image processing when the workload becomes asynchronous
- [ ] Cloud deployment target when there is a concrete hosting requirement

The roadmap deliberately does not add Kafka or RabbitMQ prematurely. A message broker should follow a demonstrated asynchronous or cross-service need, not precede it.
