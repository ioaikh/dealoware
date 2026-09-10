# BA note — Story #5 PoC auth mechanism

**Status:** Open question resolved (Product lock)  
**Date:** 2026-09-10  
**Author:** Dealoware Senior BA  
**GitHub Story:** https://github.com/ioaikh/dealoware/issues/5  
**DOC-FLOW:** `plans/` · `YYYY-MM-DD__ba__note__{slug}.md`

## Question
Within SA-acceptable set (password vs API key / JWT), pick one PoC mechanism while keeping OIDC-compatible principal shape. Full registration completes at MVP.

## Decision (Product lock)
**API key / JWT** for PoC. Password belongs with fuller registration/UI at MVP.

## Sources (no inventing)
- Chief Product lock (2026-09-10) to Senior BA
- `architecture/2026-09-10__sa__architecture__poc-feasibility-roadmap.md` (auth: password or API key/JWT + OIDC-compatible principal)
- `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` (D6 PoC; completes MVP; O9 SSO later)
- GitHub #5 body updated to match

## Spec / Dev Plan implication
Spec PoC auth = API key / JWT issue/validate; OIDC-compatible principal claims (shape only). No password UX or SSO IdP in PoC.
