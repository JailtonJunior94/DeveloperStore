## Description
Please include a summary of the change, the motivation, and context. List any dependencies that are required for this change.

## Related Issues
Fixes # (issue)

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Refactoring / Tech Debt (non-breaking change which improves code quality or structure)
- [ ] Documentation update
- [ ] CI/CD or Project Configuration update

## Verification & Proof
Please describe the tests that you ran to verify your changes. Provide instructions so we can reproduce.

- [ ] **Unit Tests**: All unit tests pass locally.
- [ ] **Integration/Functional Tests**: Mocks and integration suites pass.
- [ ] **PostgreSQL Proof**: Executed verification against real PostgreSQL container (`./scripts/validate-postgres.sh`).
- [ ] **Format check**: `dotnet format --verify-no-changes` is green.

## Architectural & Governance Checklist
- [ ] **Clean Dependency Flow**: Internal dependencies point towards the domain core (`WebApi -> Application -> Domain <- ORM`). No framework leakage into `Domain`.
- [ ] **Domain Modeling**: Followed *Domain Modeling Made Functional* principles. Modeled concepts with explicit semantic Value Objects rather than primitive types.
- [ ] **Validation Fail-Fast**: Validation is performed early at boundaries (commands/DTOs) using semantically detailed error responses.
- [ ] **Domain Events Sequence**: The correct sequence is followed strictly: `SaveChangesAsync` -> `DequeueDomainEvents` -> `PublishAsync` (ensuring events are not lost if saving fails).
- [ ] **Code Hygiene**:
  - [ ] NO C# comments (inline or block) are included in C# code.
  - [ ] No dead code, unused private methods, or unused helper classes.
