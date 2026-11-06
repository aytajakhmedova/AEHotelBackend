# User-management security regression tests

Run from the backend root:

```powershell
dotnet test AEHotel.sln -c Release --logger "trx;LogFileName=security.trx"
```

The HTTP tests use the actual controllers, services, Identity stores, JWT authentication handler,
and exception middleware with a separate SQLite `:memory:` connection for every test.
They never start production `Program.cs`, load its connection string, run its migrations or seeders,
or send requests to the running application. All accounts use `example.test` addresses.

Coverage includes Admin restrictions on every existing staff mutation and role endpoint,
self-promotion, mixed roles, case-insensitive role input, revoked/restricted actors with stale JWTs,
last-active SuperAdmin, legitimate Admin/SuperAdmin actions, hotel-scope helpers, and rollback after
a failed password change. SQL Server concurrent transaction/deadlock behavior needs a separate test environment.

Frontend browser tests (requires the actual frontend path and an installed Microsoft Edge):

```powershell
npm install --prefix artifacts/browser-tests --no-audit --no-fund playwright-core
node AEHotel.SecurityTests/frontend-security.mjs "<frontend-root>" artifacts/browser-tests/node_modules/playwright-core/index.mjs
```

This starts an isolated Vite server on `127.0.0.1:5198` and headless Edge. Every `/api/` request is
intercepted: reads return fixtures, writes receive a test 403, and external requests are aborted.
The fixture JWTs are for client rendering only; backend tests separately exercise real JWT validation.
Tests cover controls on both pages, forged cached roles, mixed target roles, SuperAdmin controls,
invalid/expired tokens, and direct URL access for other roles and anonymous visitors.
