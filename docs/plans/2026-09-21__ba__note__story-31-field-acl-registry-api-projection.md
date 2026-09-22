# BA note — Story #31 Field ACL registry + API projection (SA→Spec)

**Status:** Refined for SA→Spec · no Business open questions  
**Date:** 2026-09-21  
**Author:** Dealoware Senior BA  
**GitHub Story:** https://github.com/ioaikh/dealoware/issues/31  
**Sibling:** https://github.com/ioaikh/dealoware/issues/32  
**Parent:** https://github.com/ioaikh/dealoware/issues/18  
**DOC-FLOW:** `plans/2026-09-21__ba__note__story-31-field-acl-registry-api-projection.md`

## What changed (BA refine)
Tightened Stage A Story body from locked sources only:
- CEO Stage A named-slice unlock + Option A accept (FieldPolicy = any account info)
- Binding proposal `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md`
- Stage A SA options `architecture/2026-09-21__sa__architecture__mvp-stage-a-field-acl-list-failclosed.md`
- Stage A Security checklist `verification/2026-09-21__security__verification__mvp-stage-a-field-acl-list-sa-checklist.md`

Added Spec-ready AC: open-ended FieldClass; `IFieldPolicy.Evaluate`; CEO example classes; LoginEmail OwnAgent Deny (API policy rows); ContactEmail OwnAgent Read + ShareOutbound Deny in Stage A; DTO omit; deny-by-default; tests. Explicit OUT: Stage B/C, #24 backlog, #7 untouched, $0, no MM/DC4.

## Open questions for Business/Product
**None.** Deny status-code shape (403 vs 404) left to Spec consistency with PoC.

## Spec / BAQA handoff
Spec must not invent FieldClass names beyond CEO examples unless labeled illustrative. Agent hard-wall impl is out; API/DB half only.
