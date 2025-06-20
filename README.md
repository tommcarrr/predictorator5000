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

To run the application in Docker using the latest Compose Specification:

```bash
docker compose up --build
```

This will start both the web application and a SQL Server container.
