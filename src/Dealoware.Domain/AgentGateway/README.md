# Agent Gateway — Stage C #67

## Overview

The Agent Gateway implements the **dual wall** architecture for the agent/tool plane,
enforcing the same `IFieldPolicy.Evaluate` rules as the API/DB plane (defense #2).

## Architecture

```
┌─────────────┐      ┌──────────────┐      ┌───────────────┐
│   Agent     │──────│  AgentGateway │──────│ ToolAllowlist │
│  (OwnAgent) │      │  (mediated)   │      │ (deny-default)│
└─────────────┘      └───────┬───────┘      └───────────────┘
                             │
                    ┌────────▼────────┐
                    │  IToolExecutor   │
                    │ (platform tools) │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │AgentContextScrubber│
                    │(same IFieldPolicy) │
                    └─────────────────┘
                             │
                    ┌────────▼────────┐
                    │ ScrubbedResponse │
                    │ (safe for model) │
                    └─────────────────┘
```

## Key Security Properties

### 1. Platform Tools Only
Agents invoke platform tools via the gateway — not raw DB, not arbitrary internal HTTP.
No privileged back doors that skip `Evaluate`.

### 2. Tool Allowlist (Deny-by-Default)
Each tool must be registered on the allowlist with explicit FieldClass declarations.
Undeclared tools are denied. No tool declares `LoginEmail`.

### 3. Server-Side Scrub Before Model Context
Every tool response passes through `AgentContextScrubber` which calls the **same**
`IFieldPolicy.Evaluate` as the API/DB wall. Denied fields are stripped **before**
the response enters model context.

### 4. No LoginEmail in Agent Context
- No tool on the allowlist declares `LoginEmail`
- OwnAgent Deny for `LoginEmail` is held from Stage A
- Scrubber strips any `LoginEmail` that might accidentally appear

### 5. ShareOutbound Accept-Gated
`ShareOutbound(ContactEmail)` is allowed only when:
- `HasAcceptGrant` is true (server-side, not prompt-based)
- Principal is the authorized counterparty
- `LoginEmail` is **never** shared via ShareOutbound

### 6. Reject Prompt-Only Control
The gateway architecture is server-side enforcement — prompt text cannot escalate
FieldClass rights. Prompt-only soft guidance is **rejected** as sole control.

## Cross-Agent Messaging

**MVP Status: No cross-agent messaging path exists.**

If cross-agent messaging is added in future:
- Must go through mediated gateway path
- Payloads must be scrubbed (denied FieldClasses stripped)
- Prompt cannot escalate rights
- Gateway+scrub provides trust — not model trust

## Soft OTel/Audit Touchpoints

Per Spec §5 / Locked #9, soft touchpoints are wired where deny/scrub paths hit
SA-REV-MVP-C hooks:

- `ScrubbedToolResponse.StrippedFieldCount`: Emitted when fields are denied
- Error sanitization: Sensitive values stripped from error messages
- Tool allowlist checks: Logged when deny-by-default triggers

**No 5th Story** — these are touchpoints only, not observability product.

## Files

| File | Description |
|------|-------------|
| `AgentTool.cs` | Platform tool with FieldClass declarations |
| `FieldClassDeclaration.cs` | Declares actions allowed on a FieldClass |
| `ToolAllowlist.cs` | Deny-by-default tool registry |
| `IAgentGateway.cs` | Gateway interface |
| `AgentGateway.cs` | Gateway implementation |
| `IAgentContextScrubber.cs` | Scrubber interface |
| `AgentContextScrubber.cs` | Scrubber using same IFieldPolicy |
| `ToolResponse.cs` | Raw tool response with FieldClass tags |
| `ScrubbedToolResponse.cs` | Safe response for model context |
| `IToolExecutor.cs` | Tool execution abstraction |

## Testing

Tests in `StageCAgentHardwallTests.cs` cover:
- Allowlist deny-by-default
- No LoginEmail tool/context
- Server-side scrub before model
- ShareOutbound Accept-gated
- Stranger/cross-tenant deny
- Unauthenticated deny
- Reject prompt-only sole control
- Dual wall all FieldClasses

## References

- Issue: https://github.com/ioaikh/dealoware/issues/67
- Spec: `specs/2026-09-28__spec__spec__mvp-stage-c-agent-tool-hardwall-scrubber.md`
- Plan: `plans/2026-09-28__devplan__plan__mvp-stage-c-agent-tool-hardwall-scrubber.md`
- Stage A #31: `IFieldPolicy` API/DB wall (consumed)
- Stage B #42: `AcceptGrant` ShareOutbound patterns (consumed)
