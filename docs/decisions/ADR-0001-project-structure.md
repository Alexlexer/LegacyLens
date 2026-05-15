# ADR-0001: Project Structure

## Status

Accepted

## Context

RefactorGuard needs clear boundaries between HTTP endpoints, review orchestration, domain models, and infrastructure integrations.

## Decision

Use separate .NET projects for API, Application, Domain, and Infrastructure, with matching test projects under `tests/`.

## Consequences

The solution has more projects, but dependencies remain explicit and easier to maintain. Domain stays independent, and infrastructure can be tested or replaced behind interfaces.
