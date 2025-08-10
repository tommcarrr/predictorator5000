# Predictorator5000

This repository hosts a Blazor Server application. To run the application locally:

```bash
dotnet run --project Predictorator/Predictorator.csproj
```

The project targets .NET 9.0. The required SDK version is specified in
`global.json`. If it isn't installed locally you can run the provided
`./dotnet-install.sh` script to install the correct version.

Tests can be executed with:

```bash
dotnet test Predictorator.sln
```

Playwright UI tests are included but disabled by default. Set the
`RUN_UI_TESTS` environment variable to `true` to enable them. Optionally
set `BASE_URL` and `UI_TEST_TOKEN` to control the test host and authentication.

The seeded admin account credentials are configured via `AdminUser` settings.
You can override these values by setting the `ADMIN_EMAIL` and
`ADMIN_PASSWORD` environment variables before running the application. Once
logged in as an administrator you can view background jobs via the Hangfire
dashboard at `/hangfire`.
SMS notifications use Twilio. Set `Twilio__AccountSid`, `Twilio__AuthToken`, and
`Twilio__FromNumber` environment variables with your Twilio credentials.
Email delivery is handled by [Resend](https://resend.com). Configure the
`Resend__ApiToken` and `Resend__From` settings to enable it.
The application also requires a Rapid API key for fixture data via
`ApiSettings__RapidApiKey`.
List any email addresses that should receive extra text in prediction emails under
`PredictionEmail:SpecialRecipients`.
Set `BASE_URL` to the public address of the site so scheduled notifications
contain valid links.
Each IP address may visit at most 50 unique routes per day. Returning to a cached
page does not count toward this limit. The threshold can be adjusted with the
`RouteLimiting:UniqueRouteLimit` setting. Specific IP addresses can be excluded
via `RateLimiting:ExcludedIPs` or environment variables such as
`RateLimiting__ExcludedIPs__0=127.0.0.1`. Requests to the Hangfire dashboard
under `/hangfire` are also exempt from rate limiting.

Game week data is cached to reduce database load. The duration defaults to two
hours but can be changed using the `GameWeekCache:CacheDurationHours` setting or
the `GAMEWEEKCACHE__CACHE_DURATION_HOURS` environment variable.

All variables used by Docker Compose can be placed in a `.env` file. A sample
is provided at `.env.example`. Copy this file to `.env` and update the values
as needed before running the containers.

Verification links sent to subscribers are valid for one hour. A background job
runs weekly to calculate unverified subscriptions that have expired.

Ceefax mode provides a retro Teletext-inspired theme. When enabled, dark mode is
also automatically activated for optimal contrast. If no prior preference is
stored in the browser, the site defaults to Ceefax mode with dark mode enabled.

Score prediction inputs automatically advance to the next field after a short
delay. Configure this delay with `ScoreInputFocusDelayMs` (milliseconds); the
default is 500ms.

To run the application in Docker using the latest Compose Specification:

```bash
docker compose up --build
```

This will start both the web application and a SQL Server container.

Data Protection keys are persisted to `./dp-keys` by default. When running in Docker,
the keys are stored in `/var/dp-keys` which is backed by the `dp-keys` volume.
You can override the location by setting the `DataProtection__KeyPath` environment
variable.


## TODO

- [ ] Get Flyway migrations working in the pipeline
- [ ] Port to the Predictotronix project (API & WASM)
- [ ] Use the Premier League API to retrieve gameweeks
