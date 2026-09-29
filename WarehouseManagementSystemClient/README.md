# WarehouseManagementSystemClient

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 21.1.4.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

Unit tests run through Angular's `@angular/build:unit-test` builder with the Vitest runner. The test target is configured in `angular.json` with `tsconfig.spec.json`, includes `src/**/*.spec.ts`, and loads `src/test-providers.ts` so component tests use `HttpClientTesting` instead of calling the API.

```bash
npm test -- --watch=false
```

Use `npm test` for interactive watch mode. This project does not use Karma.

## End-to-end tests

The Playwright suite covers the highest-value WMS workflows rather than individual Angular
implementation details:

- unauthenticated route protection and the real Duende IdentityServer login/logout flow,
- document creation, draft visibility, editing and cancellation,
- successful confirmation and removal from pending work,
- backend stock/business validation surfaced by the UI without changing the document state,
- representative stock availability data and status calculation,
- warehouse and product creation reflected in their tables,
- warehouse-zone assignment and product-batch creation reflected in their aggregate lists.

When credentials and the IdP are available, authenticated scenarios use the real OIDC
Authorization Code + PKCE flow once in the Playwright setup project and reuse its `storageState`.
Credentials are supplied only through environment variables and are never written to the repository.
Without credentials, authenticated workflows are skipped; the unauthenticated test still verifies
that the client sends an OIDC authorization request when the IdP connection is refused. The IdP's
login page itself cannot be tested while the server is not hosted.

### Local prerequisites

Start the API and IdentityServer using the existing Visual Studio launch profiles, then start
the Angular application from this directory:

```bash
npm install
npx playwright install chromium
npm start
```

In another terminal, set `E2E_USERNAME` and `E2E_PASSWORD` for a local IdentityServer test user
and run:

```bash
npm run e2e
```

The default URLs match the existing development configuration (`https://localhost:4200`,
`https://localhost:44380` and `https://localhost:44377`). Override the frontend URL with
`E2E_BASE_URL` when required. The suite ignores local development certificates only; it does not
mock authentication or the WMS API.

On failure, inspect the generated HTML report with `npm run e2e:report`. Failed tests retain a
Playwright trace and screenshot. The GitHub Actions workflow builds the solution and frontend, then
runs the offline OIDC-discovery check and uploads diagnostics on failure. The authenticated business
flows need a reachable API, IdP and migrated database; the database must contain at least one
warehouse and zone and one product. Tests create uniquely named warehouses, zones, products and
batches through the UI; the application seeder remains disabled by default and is not silently
changed by the tests. The user form is intentionally excluded: the current user list has no create
action and its client-side add method only mutates in-memory data, so it does not persist a business
entity or provide a meaningful E2E scenario.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
