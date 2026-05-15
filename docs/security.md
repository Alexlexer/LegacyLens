# Security Model

RefactorGuard defaults to read-only analysis. It must not modify source repositories unless a later approved feature explicitly enables that behavior.

## Allowed Roots

All repository paths must be validated against configured allowed roots. Reject paths outside allowed roots, path traversal attempts, and ambiguous relative paths before any Git, search, or filesystem operation.

Diff preview uses this model now. If `RefactorGuard:AllowedRoots` is empty, repository diff requests are rejected instead of falling back to broad filesystem access.

## Secrets and Sensitive Data

Never commit secrets, `.env` files, private keys, local credentials, or machine-specific absolute paths. Logs and errors must not expose environment variables, API keys, full `.env` contents, private key material, or full source file content.

## External Providers

LLM providers must be explicit configuration choices. When using an external LLM, document the risk that source snippets or summaries may leave the machine. Prefer local providers for sensitive repositories.

LM Studio support targets a local OpenAI-compatible endpoint. Treat it as sensitive because diff snippets are sent to the configured server whenever `useLlm` is true.

## Network Exposure

Local services should bind to `127.0.0.1` by default. If remote access is needed, prefer a private network such as Tailscale and document the exposure clearly.

## Error Handling

Use structured problem-details style API errors. Return enough information to debug safely without leaking secrets or sensitive paths.
