# Security QA — Architecture Soft HOLD SoR · Core admin dashboard vs Chief Security SA checklist

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~1:22pm ET; re-ACK after Senior refresh vs amended checklist) |
| Verdict | **PASS** (10/10 MET) |
| Asked by | Dealoware CPM Soft HOLD Arch QA until Soft HOLD SoR qa-confirm; Senior Security PASS filed; handshake Soft HOLD SoR = **qa-confirm only** |
| Chief SA checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` (pts 1–10 ISSUED) |
| Senior Security points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-points-review.md` (**PASS** 10/10 refreshed vs **amended** checklist — cited; independently re-scored; agrees) |
| Architecture Soft HOLD SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` (§6 Security answers + §§3.1–3.8) |
| Sibling Spec Security | Spec qa-confirm PASS 10/10 · `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` |
| Moment | SA-REV-CORE-ADMIN |
| Host lock | `admin.core.dealoware.com` only |
| Principal | **CoreOwner** |
| PoC | **$0** |
| DOC-FLOW / Soft HOLD SoR | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` |

**Handshake Soft HOLD SoR = qa-confirm only.** Do **not** invent points-review Soft HOLD SoR. **Not** build / deploy / Stories / code / CDK / spend unlock. Soft HOLD harden redeploy (H1) stands separately. No AWS account / CDK / bot-platform internals invented here.

## Soft notes accepted (non-blocking)

| Soft note | Disposition |
|-----------|-------------|
| Soft HOLD Architecture QA formal PASS until Security Soft HOLD SoR CLEAR + this qa-confirm Soft HOLD SoR | **Accepted** — Security Soft HOLD SoR CLEARED by this PASS |
| Soft HOLD build / Stories / code / CDK / spend / provision / deploy | **Accepted** |
| Handshake Soft HOLD SoR = qa-confirm only (no points-review Soft HOLD SoR) | **Accepted** — this file only |
| Soft HOLD invent SSO/IdP/Cognito as delivered; Soft HOLD invent admin agents / second permission matrix | **Accepted** |
| Soft HOLD harden redeploy (H1) — separate | **Accepted** |
| Senior points-review refreshed vs amended checklist | **Accepted** — Senior now matches Chief amended pts 1–10; this confirm independently re-scores same |
| Cost/critical → COO → CEO | **Accepted** |
| Not build unlock | **Accepted** |

## Sources checked

| Source | Path | Result |
|--------|------|--------|
| Chief Security SA checklist | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-checklist.md` | Binding pts 1–10 ISSUED |
| Senior Security points-review | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-points-review.md` | **PASS** 10/10 — present; content aligned |
| Architecture Soft HOLD SoR | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` | §6 answers 1–10 **MET** with cites; Option A + §§3.3–3.6 picks concrete |
| Spec Security qa-confirm (sibling) | `…core-admin-dashboard-spec-qa-confirm.md` | PASS 10/10 — cite-only; not re-scored |

## Independent re-score (Security QA)

Score vs **Chief SA checklist pts 1–10** only. Soft HOLD SoR surface: Option A + §§3.1–3.9 + §4 + §6. Senior PASS cited, not rubber-stamped.

| # | Point (Chief SA checklist) | Senior content | Security QA | Evidence |
|---|----------------------------|----------------|-------------|----------|
| 1 | Host + role boundary | MET | **MET** | Soft HOLD SoR §2 Option A / reject B·E; §3.1 host `admin.core.dealoware.com` only; §3.2 principal **CoreOwner** ≠ Participant UI ≠ platform admin; §3.9 / §4 OUT O1 human users, Participant-UI-as-admin, `admin.platform` |
| 2 | CoreOwner auth / session (sign-in mechanism pick) | MET | **MET** | §3.3: password non-SSO; hash in Core store only; server-side session + HttpOnly Secure SameSite=Strict cookie on admin host; **8h absolute / 30m idle**; CoreOwner claim at login; re-sign-in after expiry; auth secrets never in API bodies / audit / logs / metrics / traces; Soft HOLD invent SSO/IdP/Cognito; fail-closed missing/invalid/expired session |
| 3 | Fail-closed dual-wall FieldPolicy; no parallel admin ACL | MET | **MET** | §3.2 / §3.3 / §3.7: all admin reads/writes bind Domain `IFieldPolicy.Evaluate` for **CoreOwner** (Option A §3a/§3b cite, do not rewrite); missing CoreOwner → deny list/edit/delete/stats beyond safe unauthorized; denied FieldClass → no write and no dump; parallel admin ACL rejected; Soft HOLD invent admin agents |
| 4 | Confirm-before-delete + cascade summary integrity | MET | **MET** | §3.4: UI modal names type + identity + cascade summary; cancel → no mutation and no delete audit; API confirm token single-use short-lived (e.g. 5 min) bound to actor + entity + cascade set; blind DELETE rejected; §3.6 cascade counts in confirm |
| 5 | Audit append-only integrity | MET | **MET** | §3.5: append-only audit table; who/when/type+id/action/before/after; cascade rows + correlation id; cannot edit/delete from admin UI/API; same-transaction pairing (audit fail → roll back); FieldPolicy redacts sensitive snapshots; bodies >4 KiB truncated with length + hash (no StrategyBody dump) |
| 6 | Soft-delete + cascades | MET | **MET** | §3.6: marker `DeletedAt`; hard delete deferred; Offer → offer only; Negotiation → negotiation + child offers + open warn; Artifact → **block** while non-deleted negotiation references it; Participant → Participant + negotiations + those offers (confirm counts), no auto-delete Artifacts; settlement OUT |
| 7 | Edit write surface FieldPolicy-only; concurrency fail-closed | MET | **MET** | §3.7: FieldPolicy-only writable sets; no second permission matrix; LoginEmail/ContactEmail/StrategyBody/auth secrets follow FieldPolicy — no dump into errors/logs/metrics/traces. §3.8: optimistic concurrency Version/ETag; stale → **409 Conflict** safe message, no silent overwrite; denied FieldClass values not echoed |
| 8 | Stats / list scrub soft-deleted; no warehouse invent | MET | **MET** | §3.6 list default hide soft-deleted (optional include-deleted toggle only); §3.8 all five Core stats exclude soft-deleted (`DeletedAt IS NULL`); Soft HOLD invent charts product or warehouse (§4 OUT) |
| 9 | OUT / Soft HOLD pack | MET | **MET** | §4 OUT/HOLD + header Locks + §3.9: Soft HOLD Stories/code/CDK/spend/provision/deploy until harden live QA PASS after app image bake then separate unlock; no AWS account/CDK/bot-platform internals; App Runner OUT; platform admin OUT; inbound bot after; settlement OUT; SSO/IdP OUT; PoC **$0**; Cost/critical → COO → CEO |
| 10 | Traceability + handshake Soft HOLD SoR | MET | **MET** | Sources cite Product + Spec + Spec QA + Spec Security PASS + Option A §3a/§3b + Arch QA interim content PASS; §5 / §6 handshake; Architecture QA must not formal PASS until Security QA confirms via **this** Soft HOLD SoR path; handshake Soft HOLD SoR = **qa-confirm only**; Soft HOLD build; not build unlock |

## Alignment with Senior Security done-list

Senior SA points-review (**refreshed** vs amended ISSUED checklist) scored **PASS 10/10** on Soft HOLD SoR Option A + §§3.3–3.8 picks with Soft HOLDs for Arch QA formal until Soft HOLD SoR CLEAR + qa-confirm Soft HOLD SoR only, Soft HOLD build, Soft HOLD invent SSO/admin agents/charts, Soft HOLD harden H1 separate. Independent Security QA re-score vs **amended Chief SA checklist** **agrees** on all 10; soft notes **accepted**. No bounce. Soft HOLD Security Soft HOLD SoR CLEARED by this file.

## Guardrails (spot-check)

| Guardrail | Status |
|-----------|--------|
| Host `admin.core.dealoware.com` only; principal **CoreOwner** | Held (§3.1–§3.2) |
| Password non-SSO; 8h/30m; secrets never dump | Held (§3.3) |
| FieldPolicy dual wall; no parallel admin ACL | Held (§3.2) |
| Confirm modal + API confirm token; blind DELETE rejected | Held (§3.4) |
| Append-only audit; same-transaction; not editable/deletable from admin | Held (§3.5) |
| Soft-delete `DeletedAt` + cascade table (Artifact block; Participant no Artifact auto-delete) | Held (§3.6) |
| Edit FieldPolicy-only; 409 on stale; no FieldClass echo | Held (§3.7–§3.8) |
| Soft-deleted scrubbed from lists/stats; no warehouse | Held (§3.6/§3.8) |
| Soft HOLD build; Soft HOLD SoR = qa-confirm only | Held |
| PoC $0; not build unlock; no AWS/CDK/bot-platform invent | Held |

## Gaps

**None.** Soft notes non-blocking. Soft HOLD handshake Soft HOLD SoR not invented beyond this qa-confirm. Soft HOLD build / Stories / code / CDK / spend not unlocked. Soft HOLD invent points-review Soft HOLD SoR not done. Spec track not re-scored.

## Handshake status

Security QA → **PASS** Soft HOLD SoR confirm. Soft HOLD Architecture QA Security Soft HOLD SoR **CLEARED** — Arch QA may lift Soft HOLD Security gate after this Soft HOLD SoR. Soft HOLD build / Stories / code / CDK / spend / provision / deploy. Soft HOLD invent SSO/IdP/Cognito / admin agents. Soft HOLD harden redeploy (H1) stands separately. Handshake Soft HOLD SoR = **qa-confirm only**. Not build unlock. PoC **$0**. Cost/critical: none from this Soft HOLD SoR → COO → CEO if any later spend.

**Next:** Chief Security PASS/HOLD to CPM + CA. Soft HOLD BM Spec+SA until CLEAR per CPM.
