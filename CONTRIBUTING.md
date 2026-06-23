# Contributing to DeveloperStore Sales API

Thank you for your interest in contributing! This document outlines the standards, guidelines, and workflows used in this project to maintain a clean, robust, and highly structured codebase.

---

## 🛠️ Development Setup & Workflow

### Prerequisites
- **.NET SDK 10.x**
- **Docker** and **Docker Compose**
- **EF Core CLI tool**: `dotnet tool install --global dotnet-ef --version 9.*` (or use local tool manifest)

### Standard Workflow
1. **Branch Naming**: Use descriptive branch prefixes:
   - `feat/some-feature`
   - `fix/some-bug`
   - `refactor/some-code-improvement`
   - `docs/updating-documentation`
2. **Formatting Check**: Before committing, verify formatting:
   ```bash
   dotnet format DeveloperStore.slnx --verify-no-changes
   ```
   To automatically format changes, run:
   ```bash
   dotnet format DeveloperStore.slnx
   ```
3. **Running the Test Suite**:
   Ensure all tests are passing:
   ```bash
   # Run all unit tests
   dotnet test tests/DeveloperStore.Unit/DeveloperStore.Unit.csproj
   
   # Run in-memory integration tests
   dotnet test tests/DeveloperStore.Integration/DeveloperStore.Integration.csproj
   
   # Run HTTP-level functional tests
   dotnet test tests/DeveloperStore.Functional/DeveloperStore.Functional.csproj
   
   # Run real PostgreSQL integration tests
   ./scripts/validate-postgres.sh
   ```

---

## 📝 Commit Message Guidelines

We follow the **Conventional Commits** specification. Every commit message must conform to the following format:

```text
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Allowed Types
- **feat**: A new feature for the user, not a new feature for build script.
- **fix**: A bug fix for the user, not a fix to a build script.
- **docs**: Changes to the documentation.
- **style**: Formatting, missing semi colons, etc; no production code change.
- **refactor**: Refactoring production code, e.g. renaming a variable.
- **test**: Adding missing tests, refactoring tests; no production code change.
- **chore**: Updating grunt tasks etc; no production code change.
- **ci**: CI configuration changes (e.g. GitHub Actions workflows).

### Examples
- `feat(sales): add logical cancellation endpoint for sales items`
- `fix(sales): calculate discount rate strictly according to quantity tiers`
- `refactor(domain): model money as semantic value object instead of decimal`
- `test(postgres): cover edge cases of sales retrieval pagination with PostgreSQL`

---

## 🏗️ Architectural & Clean Code Constraints

This project follows **Clean Architecture** and **Domain-Driven Design (DDD)** principles, inspired by *Domain Modeling Made Functional*.

### 1. Layers & Dependency Flow
- **Domain** is the core. It holds the ubiquitous model, aggregate roots, entities, value objects, domain events, and domain-level exceptions. It has no external dependencies (e.g., no EF Core, no ASP.NET, no Serilog).
- **Application** orchestrates use cases. Handlers receive commands and queries, invoke domain aggregates, and save changes via repositories. It does not implement business logic.
- **ORM (Infrastructure)** handles persistence mapping. Repositories translate database structures to domain entities and vice versa.
- **WebApi (Presentation)** handles HTTP transport, model binding, and converts HTTP payloads to domain/application types.
- **IoC** handles wiring composition.

### 2. Domain Modeling (Primitive Obsession Avoidance)
- Do not use raw types (`string`, `Guid`, `decimal`, `int`) to represent domain concepts.
- Use explicit types (e.g., `SaleId`, `SaleNumber`, `Money`, `ItemQuantity`, `CustomerReference`).
- Invariant protection should be enforced inside the aggregates/value objects via smart constructors or factory methods.

### 3. Error Handling and API Contracts
- **Fail-Fast**: Validate input payloads as early as possible (using FluentValidation at the application boundary).
- Error responses must follow the semantic schema containing `type`, `error`, `detail`, `status`, and an array of granular validation/domain errors (`code`, `field`, `message`).

### 4. Code Hygiene [Strict Constraints]
- **NO C# Comments**: Do not write inline or block comments (`//` or `/* */`) in `.cs` files. Write clean, self-explanatory code. Document architectural rationale in `.agents/AGENTS.md` if necessary, never in code.
- **No Dead Code**: Remove all unused classes, private methods, constants, and helper functions immediately.
- **Event Ordering**: Always use the sequence: `SaveChangesAsync` -> `DequeueDomainEvents` -> `PublishAsync` to ensure events are never lost if persistence fails.
