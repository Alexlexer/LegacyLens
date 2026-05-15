# ADR-0003: Read-Only Security Model

## Status

Accepted

## Context

LegacyLens analyzes source repositories that may contain sensitive code and secrets.

## Decision

Default all analysis features to read-only behavior. Validate repository paths against allowed roots before any filesystem, Git, search, or LLM operation.

## Consequences

The tool is safer for legacy and private repositories. Any future write-capable feature must be explicitly scoped, documented, reviewed, and tested.
