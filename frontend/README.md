# ProductVault Angular frontend

This Angular 22 standalone-component SPA is the browser client for the ProductVault ASP.NET Core Web API.

## Run

1. Start the API at `https://localhost:7253` from the repository root.
2. Install packages: `pnpm install`
3. Start the client: `pnpm start`
4. Open `http://localhost:4200`.

The client uses a route guard to protect the catalogue workspace and an HTTP interceptor to attach the active JWT bearer token to API requests.

## Feature areas

- `features/auth`: registration and sign-in.
- `features/dashboard`: per-user catalogue summary.
- `features/categories`: category create/edit and active status.
- `features/products`: paging, search, filtering, CRUD, images, and Excel operations.
- `core`: API client, authentication service, interceptor, guard, and TypeScript models.

Build a production bundle with `pnpm run build`.
