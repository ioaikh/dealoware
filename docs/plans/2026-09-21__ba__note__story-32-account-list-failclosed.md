# BA note — Story #32 Account list fail-closed (SA→Spec)

**Status:** Refined for SA→Spec · no Business open questions  
**Date:** 2026-09-21  
**Author:** Dealoware Senior BA  
**GitHub Story:** https://github.com/ioaikh/dealoware/issues/32  
**Sibling:** https://github.com/ioaikh/dealoware/issues/31  
**Parent:** https://github.com/ioaikh/dealoware/issues/18  
**DOC-FLOW:** `plans/2026-09-21__ba__note__story-32-account-list-failclosed.md`

## What changed (BA refine)
Clarified “owning account” using Stage A SA options §1 (do not invent):
| Resource | Authorized viewer (Stage A) |
|----------|-----------------------------|
| Artifact list/get | Owner (`OwnerParticipantId` == principal `sub`) |
| Negotiation list/get | Party (A or B) |
| Offer list/get | Party to parent negotiation |

AC now require query-plane filters (not UI-only), uniform deny bodies without private fields, and IDOR/cross-tenant/unauth tests. OUT: Strategy lists, discovery, Stage B/C, #24 backlog, #7 untouched, $0, no MM/DC4.

## Open questions for Business/Product
**None.** Owner vs party locked in SA Stage A options. Spec chooses 401/403/404 consistency with PoC.

## Spec / BAQA handoff
Do not invent admin/multi-tenant views. Sibling #31 supplies FieldPolicy; this Story is resource-scope list/get fail-closed.
