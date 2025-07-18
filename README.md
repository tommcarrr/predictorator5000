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
`ADMIN_PASSWORD` environment variables before running the application.
SMS notifications use Twilio. Set `Twilio__AccountSid`, `Twilio__AuthToken`, and
`Twilio__FromNumber` environment variables with your Twilio credentials.
Set `BASE_URL` to the public address of the site so scheduled notifications
contain valid links.

Verification links sent to subscribers are valid for one hour. A background job
runs every 15 minutes to remove unverified subscriptions that have expired.

Ceefax mode provides a retro Teletext-inspired theme. When enabled, dark mode is
also automatically activated for optimal contrast.

To run the application in Docker using the latest Compose Specification:

```bash
docker compose up --build
```

This will start both the web application and a SQL Server container.

Data Protection keys are persisted to `./dp-keys` by default. When running in Docker,
the keys are stored in `/var/dp-keys` which is backed by the `dp-keys` volume.
You can override the location by setting the `DataProtection__KeyPath` environment
variable.
