# Predictorator5000

This repository hosts a Blazor Server application. To run the application locally:

```bash
dotnet run --project Predictorator/Predictorator.csproj
```

Tests can be executed with:

```bash
dotnet test Predictorator.sln
```

The seeded admin account credentials are configured via `AdminUser` settings.
You can override these values by setting the `ADMIN_EMAIL` and
`ADMIN_PASSWORD` environment variables before running the application. Once
logged in as an administrator you can view background jobs via the Hangfire
dashboard at `/hangfire`.
SMS notifications use Twilio. Set `Twilio__AccountSid`, `Twilio__AuthToken`, and
`Twilio__FromNumber` environment variables with your Twilio credentials.
Set `BASE_URL` to the public address of the site so scheduled notifications
contain valid links.
The global rate limiter can exclude specific IP addresses. Add them under
`RateLimiting:ExcludedIPs` in configuration or via environment variables such as
`RateLimiting__ExcludedIPs__0=127.0.0.1`.

Game week data is cached to reduce database load. The duration defaults to two
hours but can be changed using the `GameWeekCache:CacheDurationHours` setting or
the `GAMEWEEKCACHE__CACHE_DURATION_HOURS` environment variable.

All variables used by Docker Compose can be placed in a `.env` file. A sample
is provided at `.env.example`. Copy this file to `.env` and update the values
as needed before running the containers.

Verification links sent to subscribers are valid for one hour. A background job
runs every 15 minutes to remove unverified subscriptions that have expired.

Ceefax mode provides a retro Teletext-inspired theme. When enabled, dark mode is
also automatically activated for optimal contrast. If no prior preference is
stored in the browser, the site defaults to Ceefax mode with dark mode enabled.

To run the application in Docker using the latest Compose Specification:

```bash
docker compose up --build
```

This will start both the web application and a SQL Server container.

Data Protection keys are persisted to `./dp-keys` by default. When running in Docker,
the keys are stored in `/var/dp-keys` which is backed by the `dp-keys` volume.
You can override the location by setting the `DataProtection__KeyPath` environment
variable.
