# BA business verification — Story #42 Contact on accept (P7 / A9 minimum)

**Status:** Senior BA recommendation — **PASS** (pending BAQA verify-QA)  
**Date:** 2026-09-22 (BA verify executed ~2:42 PM ET)  
**Author:** Dealoware Senior BA  
**Story:** https://github.com/ioaikh/dealoware/issues/42  
**Impl PR:** https://github.com/ioaikh/dealoware/pull/52 (**MERGED** @ main `ac5bc136c018c44fc7e67f89ea4280fa03c2f794`; MergedAt 2026-09-22 2:20:45 PM ET)  
**CI (main @ merge):** https://github.com/ioaikh/dealoware/actions/runs/35766544506 — **SUCCESS**  
**Method:** Business-intent AC check from Story #42 body + merged PR #52 evidence on **main** (`gh` remote reads of AcceptGrant / StageBContactOnAcceptTests / FieldPolicy ShareOutbound; no clone) + KB Product QA / Security Doc+ProductQA confirms / Doc weave (not code review). No invented requirements. Stage B named slice only. Extends PoC **#7** under ACL (does not rewrite #7). Sibling **#40/#41 OUT**. Soft CLOSE SoR CLEAR (#59/#60/#61 @ `68c2196…`). PoC $0. No MotorMarket/DC4. No Assistant inventing.

## Binding AC (Story #42 — do not invent)

1. **Until Accept:** Negotiation/Offer APIs **omit** counterparty contact PII (extend #7 seal; no regression)
2. **Accept grant:** User Accept recorded → resourceContext **HasAcceptGrant** (or equiv) for FieldPolicy
3. **On Accept:** authorized **ShareOutbound(ContactEmail)** may release ContactEmail to counterparty only with Accept on **that** offer/negotiation
4. **Before Accept:** ShareOutbound(ContactEmail) **Deny** for all principals
5. **ContactEmail** rows: User R/W; OwnAgent Read; Counterparty Deny until grant; Stranger/Unauth Deny
6. **LoginEmail** remains **User-only** — never shared on Accept (do not conflate with ContactEmail)
7. Automated tests: pre-Accept no contact leak; post-Accept ContactEmail to authorized CP only; stranger deny; LoginEmail never; unauth deny; uniform deny
8. Documented as **P7** / **A9** MVP **minimum**; vault → **V3**; extends **#7** under ACL (no rewrite)

## AC checklist

| # | AC | Verdict | Evidence |
|---|-----|---------|----------|
| 1 | Until Accept: Neg/Offer omit contact PII (extend #7) | **PASS** | Facts: `PreAccept_GetNegotiation_NoContactEmail_NoLoginEmail`, `PreAccept_GetOffer_NoContactEmail_NoLoginEmail`. Product QA AC1 MET @ `ac5bc136…`; Security ProductQA pt1 MET. |
| 2 | Accept grant → HasAcceptGrant (or equiv) for FieldPolicy | **PASS** | Main `AcceptGrant` entity + `FieldResourceContext.HasAcceptGrant`; Fact `Accept_HasAcceptGrant_PersistsInDatabase`; post-Accept ShareOutbound Facts. Product QA AC2 MET. |
| 3 | On Accept: ShareOutbound(ContactEmail) to authorized counterparty only | **PASS** | Facts: `Accept_CounterpartyReceivesContactEmail_LoginEmailNever`, `ContactEmail_Counterparty_ShareOutbound_AllowedPostAccept`, `PostAccept_ShareOutbound_Allow_*`. Product QA AC3 MET. |
| 4 | Before Accept: ShareOutbound Deny all | **PASS** | Fact `PreAccept_ShareOutbound_Deny_FieldPolicy`. Product QA AC4 MET; Security pt3 MET. |
| 5 | ContactEmail policy rows: User R/W; OwnAgent Read; CP until grant Deny; Stranger/Unauth Deny | **PASS** | Facts: `ContactEmail_PolicyMatrix_PreAccept` Theory; `ContactEmail_Counterparty_ShareOutbound_AllowedPostAccept`. Product QA AC5 MET. |
| 6 | LoginEmail never on Accept (User-only) | **PASS** | Facts: `ShareOutbound_LoginEmail_AlwaysDeny`, `Accept_LoginEmail_NeverIncluded`, `LoginEmail_PolicyMatrix`. Product QA AC6 MET; Security pt5 MET. |
| 7 | Tests matrix | **PASS** | `tests/Dealoware.Api.Tests/StageBContactOnAcceptTests.cs` on main (~16–21 Facts/Theories; size 21786 via `gh`); also Unauth/stranger Facts. CI SUCCESS run 35766544506. Product QA AC7 MET. Soft: no live `dotnet` — CI + inventory equivalent. |
| 8 | P7/A9 minimum; vault V3 OUT; extend #7 no rewrite | **PASS** | PR scoped to AcceptGrant + ShareOutbound + tests; #7 extended not rewritten (IdentitySealTests updated for Stage B behavior). Story OOS + Doc weave + Product QA AC8 MET; Security pts 7–8 MET. |

## Out of scope held

| OOS item (Story #42 + locks) | Held? | Evidence |
|------------------------------|-------|----------|
| Mature PII vault retention/erasure (**V3**) | **Yes** | Not in PR; weave Constraints; Security pt8; Product QA AC8 |
| Stage C agent/tool share-plane | **Yes** | API/DB ShareOutbound-after-Accept only; no agent `share_contact_email` tool; weave; Security pt7 |
| Instant search (**#40**) | **Yes** | #40 OUT — separate BA verify |
| Strategy ACL (**#41**) | **Yes** | #41 OUT — separate BA verify; Assistant soft OUT on #41 not expanded here |
| Unlocking gate **#25** / parent **#18** Spec/SD | **Yes** | HOLD; weave Explicit separations; Security pts 7–8 |
| Rewriting PoC **#7** history | **Yes** | Extends seal→contact under ACL; #7 stub claims not rewritten; Security pt7 |
| MotorMarket / DC4 | **Yes** | Story + PR OUT; weave; Security pts 7–9 |
| Cognito / SSO / IdP inventing | **Yes** | PoC $0; Security pt9 |
| PoC/MVP spend (**$0**) | **Yes** | No IdP/vault/AWS; Product QA Sec pt9; Doc weave PoC $0 |

## Soft gaps (non-blocking for BA business verify)

- No live `dotnet restore|build|test` on evidence box — accepted; CI SUCCESS run 35766544506 + `StageBContactOnAcceptTests` inventory via `gh`.
- AcceptGrant DB-row assertion thin (SD soft) — non-blocking; HasAcceptGrant + ShareOutbound Facts still evidence AC2–4.
- Accept response ContactEmail + subsequent GET may remain sealed — SD Security accepted soft (cite Product QA/SD); business AC met via Accept-path ShareOutbound + counterparty receive Facts.
- Soft CLOSE SoR CLEAR via docs PRs #59/#60/#61 @ `68c2196…` — eng HOLD until CBA confirm.

## Prior gate chain (evidence, not re-scored here)

| Gate | Result | Path / link |
|------|--------|-------------|
| Spec QA | PASS | `verification/2026-09-22__spec__verification__mvp-stage-b-contact-on-accept.md` + Spec Security confirm |
| Dev Plan QA | PASS | `verification/2026-09-22__devplan__verification__mvp-stage-b-contact-on-accept.md` + DevPlan Security confirm |
| SD / Dev Code QA | PASS | `verification/2026-09-22__sd__verification__mvp-stage-b-contact-on-accept.md` |
| SD Security | PASS (1–10 MET) | `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-sd-qa-confirm.md` |
| CQ | `cq:no-refactor` | Eng context + Product QA header |
| Product QA | PASS | `qa/2026-09-22__qa__qa-report__mvp-stage-b-contact-on-accept.md` |
| Product QA Security | PASS (1–10 MET) | `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-productqa-qa-confirm.md` |
| Doc Security weave | PASS (overall Doc step) | `ops/2026-09-22__docs__ops__mvp-stage-b-contact-on-accept-doc-security-weave.md` |
| Doc Security QA | PASS (1–10 MET) | `verification/2026-09-22__security__verification__mvp-stage-b-contact-on-accept-doc-qa-confirm.md` |
| Soft CLOSE SoR | CLEAR | PRs #59/#60/#61 @ `68c219699fff9e8afccd491510e587ca087bf024` |

## Recommendation to CBA

**PASS** — deliverable meets Story #42 business AC (pre-Accept seal held extending #7; AcceptGrant/HasAcceptGrant; ShareOutbound(ContactEmail) only after Accept to authorized counterparty; pre-Accept Deny all; ContactEmail policy rows; LoginEmail never on Accept; StageBContactOnAcceptTests + CI SUCCESS; P7/A9 minimum with vault→V3 OUT), and OOS held (#40/#41 separate; Stage C share-tool; #25/#18 HOLD; #7 not rewritten; MotorMarket; Cognito; PoC $0). Soft gaps non-blocking. Hand to BAQA for verify-QA; eng `done` HOLD until CBA final confirm after BAQA. Do **not** CLOSE issue #42, unlock #25, invent Stage C share-tool / Assistant, or merge sibling tracks from this step. Stage B named slice only. PoC $0.
