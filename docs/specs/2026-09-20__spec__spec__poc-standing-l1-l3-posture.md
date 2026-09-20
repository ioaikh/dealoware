# Spec — PoC Standing license / repo / hosted posture (L1–L3)

**Status:** Senior Spec — Security-bound draft ready for Spec QA  
**Date:** 2026-09-20  
**Author:** Dealoware Senior Spec  
**Brief:** Chief Spec — PRIORITY #8 Standing L1–L3 (CEO unlock / CPM); chore Spec  
**Issue:** https://github.com/ioaikh/dealoware/issues/8  
**IDs:** **L1 L2 L3** · `stage:poc` · `type:chore` · standing **all stages**  
**DOC-FLOW:** `specs/2026-09-20__spec__spec__poc-standing-l1-l3-posture.md`  
**Constraints:** Chore only — **no** product feature inventing. No MotorMarket/DC4. No AWS/IdP spend. Do not change license or hosted-platform ownership. Spec QA must not PASS until Security QA confirms Spec-step points. Cost/critical → COO → CEO.

---

## Sources (cite only; no invented requirements)

| Source | Path / link | Role |
|--------|-------------|------|
| Issue #8 AC + OUT | https://github.com/ioaikh/dealoware/issues/8 | Binding acceptance |
| Product Later CEO decisions | `product/PRODUCT-BRIEF.md` | Apache-2.0; public GitHub; hosted AIKnowHow/Dealoware; MM/DC4 separation |
| Release roadmap | `plans/2026-09-10__pm__plan__release-roadmap-poc-mvp-vn.md` | L1–L3 standing all stages |
| Spec Security checklist (binding) | `verification/2026-09-20__security__verification__poc-l1-l3-posture-spec-checklist.md` | Spec-step points **1–10** |

**Product alignment:** Later CEO decisions — license Apache-2.0; public repo `https://github.com/ioaikh/dealoware`; hosted platform remains AIKnowHow / Dealoware (not free-hosted fork as “the” platform); MotorMarket/DC4 live systems out. Conflicts → escalate PM → Product → CEO.

---

## Locked decisions

| # | Decision | Spec lock |
|---|----------|-----------|
| 1 | L1 | Apache License 2.0 `LICENSE` present and correct at repo root |
| 2 | L2 | Public-repo posture clear in README/docs pointing to `https://github.com/ioaikh/dealoware` |
| 3 | L3 | Document non-goal: free-hosted fork is **not** “the” platform; hosted stays AIKnowHow / Dealoware |
| 4 | Separation | MotorMarket/DC4 live systems, inventory, SFTP, test logins **out** of Dealoware work (noted in docs) |
| 5 | Chore boundary | No domain API work (#3–#7); no marketing claims; no license/ownership change |
| 6 | Spend | PoC **$0** AWS/IdP for this chore; ECS Express sketch remains host-shape note only if docs mention AWS |

---

## 1. L1 — Apache-2.0 LICENSE

| Requirement | Spec lock |
|-------------|-----------|
| File | Repo root `LICENSE` (canonical) |
| Text | Apache License 2.0 — present and correct (standard Apache-2.0 text; no proprietary substitute) |
| Evidence | Spec QA / SD verify file exists and is Apache-2.0 |
| Forbidden | Inventing commercial/proprietary license claims for the public OSS repo |

---

## 2. L2 — Public GitHub posture

| Requirement | Spec lock |
|-------------|-----------|
| Canonical URL | `https://github.com/ioaikh/dealoware` |
| Docs | README and/or `docs/` clearly state public GitHub posture |
| Forbidden | Conflicting “private-only” or invented dual-canonical-repo claims |
| Evidence | Spec QA / SD verify README/docs wording + URL |

---

## 3. L3 — Hosted-platform non-goal

| Requirement | Spec lock |
|-------------|-----------|
| Wording | Document that a **free-hosted fork is not “the” Dealoware platform** |
| Hosted | Hosted platform remains **AIKnowHow / Dealoware** |
| Location | README and/or product/docs posture section (align with PRODUCT-BRIEF; do not overwrite CEO original) |
| Forbidden | Inventing new hosted ownership; requiring Cognito/SSO or paid AWS host as #8 deliverable |

---

## 4. MotorMarket / DC4 separation

| Requirement | Spec lock |
|-------------|-----------|
| Standing non-goal | Keep MotorMarket / DC4 **live systems**, inventory, SFTP feeds, and test logins **out** of Dealoware Spec/docs/code for this chore |
| Docs | Explicit separation note in README/docs (cite Product Later CEO decision) |
| Secrets | Never commit MM/DC4 credentials, inventory dumps, or SFTP secrets into the public repo |

---

## 5. Security Spec checklist binding (points 1–10)

**Binding checklist:** `verification/2026-09-20__security__verification__poc-l1-l3-posture-spec-checklist.md`  
Spec QA must not PASS until **Security QA** confirms.

| # | Security point | Spec requirement | Spec section cites |
|---|----------------|-------------------|--------------------|
| 1 | L1 LICENSE correctness | Apache-2.0 LICENSE present/correct; no invented proprietary claims | §1 L1; Locked #1 |
| 2 | L2 Public-repo posture | Canonical public GitHub URL in README/docs; no private-only inventing | §2 L2; Locked #2 |
| 3 | Public-repo secrets hygiene | Docs/artifacts for this chore never commit secrets, API keys, cloud creds, MM/DC4 logins/SFTP/inventory dumps | §4 Separation; Locked #4; §6 OUT |
| 4 | L3 Hosted non-goal | Free-hosted fork ≠ “the” platform; hosted = AIKnowHow/Dealoware; no ownership inventing / no IdP provision required | §3 L3; Locked #3 |
| 5 | No PoC hosted-platform inventing | No Cognito/SSO, prod App Runner/ECS spend, or “official hosted SaaS” as #8 deliverables; runtime PoC local/$0 | Locked #6; §6 OUT |
| 6 | MotorMarket / DC4 separation | Explicit OUT of live systems/inventory/SFTP/test logins | §4; Locked #4 |
| 7 | No inventing product features | Cite #8 AC+OUT only; no marketing claims; no new domain APIs; no Strategy/AI/settlement/identity-seal under this chore | Locked #5; §6 OUT |
| 8 | Cross-story non-merge | Keep L1–L3 separate from #3–#7 feature Specs except cross-refs; do not rewrite those AC | Constraints; Locked #5 |
| 9 | Cost / spend guardrail | PoC **$0** AWS/IdP for this chore; cost/critical → COO → CEO | Locked #6; Constraints |
| 10 | Traceability + handshake | Cites issue #8 AC+OUT only; Spec QA PASS only after Security QA | Sources; this §5; Done-list |

---

## 6. Explicit OUT

| OUT | Note |
|-----|------|
| Marketing claims | Issue OUT |
| Inventing product features / domain APIs | #3–#7 stay separate |
| Changing license or hosted-platform ownership | Not in scope |
| Cognito/SSO / paid AWS host as #8 deliverable | $0 PoC |
| Strategy / AI / settlement / identity-seal inventing | Wrong Story |
| MotorMarket/DC4 live coupling or secrets in repo | Standing separation |
| Rewriting #3–#7 Specs | Cross-ref only |

---

## 7. Acceptance mapping (issue #8 AC → Spec)

| Issue AC | Spec section |
|----------|--------------|
| L1 LICENSE Apache-2.0 present/correct | §1 |
| L2 Public repo posture in README/docs | §2 |
| L3 Free-hosted fork non-goal; hosted AIKnowHow/Dealoware | §3 |
| Separation MotorMarket/DC4 noted | §4 |

---

## Done-list (Spec QA / Security QA / Dev Plan / SD)

### Spec QA

- [ ] DOC-FLOW: `specs/2026-09-20__spec__spec__poc-standing-l1-l3-posture.md`
- [ ] Binding sources only (issue #8, Product Later CEO, roadmap L1–L3, Security checklist)
- [ ] L1/L2/L3 + MM/DC4 separation requirements with file/wording constraints
- [ ] **Security points 1–10** bound with cites (§5)
- [ ] Chore scope only — no feature inventing; no #3–#7 rewrite
- [ ] Explicit OUT + AC mapping; PoC $0
- [ ] **Ask Security QA** confirm Spec-step 1–10 **before** PASS to Chief Spec

### Dev Plan / SD (after Spec QA PASS + Security QA PASS + Chief Spec)

- [ ] Ensure root `LICENSE` is Apache-2.0 (L1)
- [ ] README/docs state public GitHub URL (L2)
- [ ] README/docs state hosted non-goal + AIKnowHow/Dealoware (L3)
- [ ] README/docs note MM/DC4 separation; no secrets committed
- [ ] Do not implement §6 OUT

**Next:** Spec QA → Security QA confirm → Spec QA confirm to **Chief Spec only**.
