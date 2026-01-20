# Development Guide

This document explains how to set up your environment and contribute to Mokit.

## Prerequisites

- **.NET 10 SDK** - https://dotnet.microsoft.com/download
- **IDE** - Visual Studio 2022, JetBrains Rider, or VS Code
- **PostgreSQL** - Required for database
- **Git** - For version control

## Setting Up Locally

1. Clone the repository:
   ```bash
   git clone https://github.com/mokitapp/mokit-core.git
   cd mokit-core
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Configure database connection in `src/Mokit.Web/appsettings.Development.json`

4. Run migrations:
   ```bash
   dotnet ef database update -p src/Mokit.Infrastructure -s src/Mokit.Web
   ```

5. Run the application:
   ```bash
   dotnet run --project src/Mokit.Web
   ```

## Branching Strategy

- **`main`** - Production-ready code. Do not push directly.
- **`develop`** - Integration branch for next release (optional).
- **Feature branches** - `feature/<short-description>` (e.g., `feature/add-jwt-validation`)
- **Bug fixes** - `fix/<issue-description>` (e.g., `fix/cors-header-issue`)
- **Refactoring** - `refactor/<scope>` (e.g., `refactor/clean-architecture`)

Choose branch names that represent the full scope of your work.

## Testing

Unit tests are located in `tests/Mokit.UnitTests`. Run them before pushing:

```bash
dotnet test
```

## Coding Standards

- Use C# 12+ features
- Follow standard .NET conventions
- Use `async/await` consistently - avoid `.Result` or `.Wait()`
- Document public APIs and complex logic

## Database

Mokit uses PostgreSQL with Entity Framework Core.

To add a new migration:
```bash
dotnet ef migrations add <MigrationName> -p src/Mokit.Infrastructure -s src/Mokit.Web
```

To apply migrations:
```bash
dotnet ef database update -p src/Mokit.Infrastructure -s src/Mokit.Web
```

## Contribution Process

1. Create a branch from `main`
2. Make your changes
3. Run tests locally
4. Push your branch
5. Open a Pull Request
