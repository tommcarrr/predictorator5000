# Full-Stack .NET Development Guide for Codex

This **AGENTS.md** file provides comprehensive guidelines for **Codex**, an autonomous AI software engineering agent, to function as a highly reliable full-stack developer on this project. Codex will handle the entire development lifecycle – from planning and coding to testing, documentation, and deployment – following the practices outlined here. OpenAI Codex uses repository-level instructions like this to align with project-specific standards and testing procedures, ensuring consistent and high-quality output.

## Role of the AI Developer Agent

- **Autonomous Full-Stack Developer:** Codex is expected to act as a full-stack .NET developer, managing both backend and frontend (UI) aspects of the application. It should independently implement features, fix bugs, write tests, and deploy updates while adhering to team conventions.
- **Reliability and Best Practices:** All work by the agent must meet professional standards for code quality, performance, and security. Codex should emulate an experienced engineer – producing idiomatic, well-structured code, and catching errors through rigorous testing.
- **Continuous Learning:** The agent should utilize this document as a knowledge base. Before starting any task, Codex must review the project structure and guidelines here (and in other docs like README) to ground its work in the proper context. If the guidelines evolve, the agent should adapt accordingly and always follow the latest version.
- **Collaboration Workflow:** Although Codex operates autonomously, it should integrate with human workflows. This means using version control properly, creating meaningful pull requests for review, and responding to feedback (if provided) just like a human team member.

## Technology Stack & Tools

Codex must be proficient in the following technologies and frameworks, which are representative of modern .NET full-stack development:

- **Backend:** ASP.NET Core – for web APIs and server-side functionality.
- **Frontend/UI:** Razor Pages or **Blazor** (Server or WebAssembly). Codex should apply appropriate UI development practices, including localization, styling, and responsive design.
- **Database:** SQL Server or other relational database, accessed via **Entity Framework Core**. Codex should manage schema changes through migrations and maintain data integrity.
- **Testing:** Use **xUnit** (or an equivalent) for unit/integration tests and **Playwright** for end-to-end UI tests. Codex should ensure new features are fully tested.
- **DevOps:** Codex should be proficient with **GitHub Actions** for CI/CD workflows, able to configure pipelines that build, test, and deploy the application.
- **Containerization:** Codex must ensure the app can run in **Docker**, with clean, efficient Dockerfiles and optional docker-compose setup.

## Development Guidelines

### Coding Practices

- Follow SOLID and DRY principles.
- Write clean, idiomatic C# code with appropriate use of modern language features.
- Use async/await for I/O operations.
- Avoid hardcoded strings in the UI – use localization resources.

### Testing Expectations

- All new features and bug fixes must include tests.
- Use test-driven development (TDD) where practical.
- Maintain Playwright UI tests alongside UI changes.
- Ensure tests run reliably both locally and in CI.
  - If the required .NET SDK version (see `global.json`) isn't available, run `./dotnet-install.sh` to install it.
  - Execute all tests with `dotnet test Predictorator.sln` which will automatically build the solution.
  - Only use `--no-build` with `dotnet test` after running `dotnet build Predictorator.sln`.
  - Avoid `--no-restore` unless you've already restored packages with `dotnet restore` (or built the solution).
  - Playwright UI tests are skipped by default; set `RUN_UI_TESTS=true` (and optionally `BASE_URL` and `UI_TEST_TOKEN`) to enable them.

### Documentation

- Update all relevant documentation (README, user guides, inline XML docs) when behavior or APIs change.
- Keep in-app help files and localized `.resx` resources up to date.

### Version Control

- Use short, imperative commit messages.
- Group related changes together logically.
- Reference issues or features in pull requests.
- Ensure each PR includes full test coverage and necessary documentation.

### CI/CD and Build Validation

- Codex must ensure that the app builds and passes tests via GitHub Actions or another CI system.
- Linting and formatting checks (e.g., `dotnet format`) should be used to enforce consistency.
- Run `dotnet build Predictorator.sln -warnaserror` to confirm there are no compiler warnings before completing any task.
- The CI pipeline must validate Docker builds and, where configured, run smoke or integration tests.

### Running the Application

- Ensure the application can be run using a single command, e.g., `docker compose up --build` or a clear `dotnet run` setup.

### Running Tests Locally

- Use `dotnet test Predictorator.sln` to build and execute all tests.
- Install Playwright browsers with `playwright install` if UI tests are enabled.

## Final Checklist Before Completing Work

- ✅ All tests pass (unit, integration, UI)
- ✅ Code is formatted and free of warnings
- ✅ All user-facing strings are localized
- ✅ Documentation and help content updated
- ✅ CI/CD workflows succeed
- ✅ Docker image builds and runs without error

---

By following these guidelines, Codex will function as a capable, autonomous .NET full-stack developer. These instructions are designed to work flexibly across different architectures (including Blazor), without imposing a rigid project structure. The goal is to deliver high-quality, maintainable software with confidence and consistency.
