# ADR-0002: gpu-search HTTP Integration

## Status

Accepted

## Context

LegacyLens needs code search, semantic search, block reading, skeleton reading, and dependency impact from `gpu-search-mcp`.

## Decision

Integrate with `gpu-search-mcp` through a typed HTTP client behind an `IGpuSearchClient` interface. Preserve existing MCP behavior in the search service.

## Consequences

LegacyLens remains independent from the search implementation and can mock search behavior in tests. HTTP configuration, health checks, and timeout handling must be explicit.
