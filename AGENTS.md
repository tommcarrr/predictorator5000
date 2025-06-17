# AGENTS Instructions

The following guidelines apply to the entire repository and inform how Codex should work.

## Development workflow

- After modifying files, run `dotnet format --no-restore` to keep style consistent.
- Run `dotnet restore` and then `dotnet test <solutionPath>` to verify the solution builds and tests succeed.
- Run `dotnet build <solutionPath> -c Release -warnaserror` to ensure there are no Roslyn warnings.
- If these commands fail because the environment lacks `dotnet`, note this in the PR's testing section.

## Commit guidelines

- Use short, imperative commit messages (e.g. "Add new API method"). Group related changes together.
- Follow the existing C# style and project structure.
- Adhere to SOLID and DRY principles to keep the code maintainable.

## Pull request notes

- Reference relevant lines from `README.md` when explaining how to build or run the project.
- Ensure all functionality is covered by tests. Write tests first when possible.
- Update documentation whenever behavior or APIs change so README and other docs stay accurate.

## Running the application

- Use `docker compose up --build` to run the site locally.

## UI tests

- Playwright tests live in the `Predictorator.UiTests` project.
- Keep these tests current and add new coverage whenever the UI changes.
- The CD workflow deploys directly to production and then runs the Playwright
  tests against that environment. There is no staging deployment and the tests
  use the `TEST_TOKEN` secret to populate `UI_TEST_TOKEN`.
