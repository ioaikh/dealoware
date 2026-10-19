# Dev Plan — Core admin dashboard (A5)

**Status:** Amended 2026-10-05 ~8:45pm ET on base tip `21535e66…` (prior tip `3385872a…` void): **Steps 7 and 9 scope amend** plus the **CPM 8:41pm ET holds refresh**. Step 7 locks `OfferStatus.Withdrawn = 5` with filter and badge, and a maker-only domain method, but no writer: no endpoint or admin path sets Withdrawn until the backlog maker-withdraw Story ships on `api.core.dealoware.com`. Step 9 locks the SA UX1 redlines note §4 edit paths (no narrowing) through Chief Developer coverage items E1–E12. Step 7 cites coverage items W1–W5. Sources add the Product decision, Product QA PASS, SA redlines note, Chief Developer coverage list, Test design PASS, H4 live retest PASS (primary), H4 Debug-off redeploy DevOps QA PASS (separate) and the maker-withdraw Story. Holds refresh: the A6 Test design gate is closed (design tip `8df39255…` PASS), and the A7 gate on H4 live is closed (primary evidence: Chief QA H4 live retest PASS `4911b08…`, token and register limiters return 429 with Retry-After; the Debug-off redeploy `11c35ee7…` is cited separately and does not by itself close H4). **H4 cite bounce fix (Dev Plan QA on `bd814d40…`):** primary A7-on-H4 evidence is now the live retest `4911b08`; `11c35ee7…` stays only as the Debug-off redeploy cite. UI lanes, deploy, secrets, H1 and the flood limiter holds stay. The retired hold phrase scrub (CEO 2:17pm ET / ORG-OPS) remains in force; its count stays 0. Tip sha256 changes with this amend; new hash reported on READY. Dev Plan QA delta and Security QA reconfirm still required on the new tip. PoC **$0**. Not a build unlock. See **Active holds** below.  
**Status history:** Wording-only scrub 2026-10-05 retiring the old hold phrase (CEO 2:17pm ET / ORG-OPS): every former hold label is now plain English; steps, scope, mappings, binds and tip cites unchanged; tip sha256 changes; not a build unlock. Earlier: amended 2026-10-05 for UX1-A11/A14 (15 steps), then Security QA bounce fix on tip `abfa672a…`: Step 14 binds UX1-A16 (two-step conditions 1–8), UX1-A03 generic lockout copy, and password answers tip `a87e293b…` (plus password-rules / lockout-window Spec notes); Step 15 wording aligned to **HMAC IP prefix**. **CPM Spec tip refresh:** lockout-window v5.5 `8a194eb9…` and password-rules v2.3 `1a7b342c…` are binding (v5.4 / v5.3 void; v2.2 superseded); sign-in steps Spec note v1 `0a3db5f4…` cited on Step 14. **Dev Plan QA pre-diff nits:** Step 5 A16 cross-cite; Step 8 `signin.second_factor_failed`; answers tip `a87e293b…` explicit (`f03a82c9…` void). Dev Plan QA / Security QA reconfirm was required at `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` on each tip.  
**Date:** 2026-10-05  
**Author:** Dealoware Senior Dev Planner  
**Brief:** Chief Dev Planner → Senior — DRAFT A5 Core admin dashboard Dev Plan (CPM formal unlock confirms A5). Executable plan only. As briefed then: Stories/code waited for Dev Plan QA + Test design PASS, and A7 waited for H4 live PASS (both of those gates have since closed; see Active holds). Do not invent password / AWS account IDs. Deploy waits for Ivan's OK. H1 harden redeploy is a separate track. PoC **$0**. App target .NET 10 — plan assumes TFM `net10.0`; do **not** schedule .NET 10 retarget as this Story's work.  
**DOC-FLOW:** `plans/2026-10-05__devplan__plan__core-admin-dashboard.md`  
**Host:** `admin.core.dealoware.com` only (Core admin — **not** `admin.platform.dealoware.com`)  
**Principal:** **CoreOwner** = single system superadmin `io@aiknowhow.com`  
**Constraints:** Do not invent Stories/build from this plan until Dev Plan QA PASS on this tip (A6 Test design gate closed: design tip `8df39255…` PASS) · A7 gate on H4 live closed (Chief QA H4 live retest PASS `4911b08…`; Debug-off redeploy `11c35ee7…` cited separately) · Do not invent password / AWS account IDs · Deploy waits for Ivan's OK · Do not invent process-global auth flood limiter until Chief Security explicitly approves · H1 harden redeploy is a separate track · PoC **$0** · No Cognito invent · No MM/DC4 · No password values anywhere · No AWS account / CDK / bot-platform internals in this plan · Cost/critical (LLM if any) → COO → CEO (do not provision)

**Active holds** (refreshed per CPM 8:41pm ET; plain English):

- No Stories or code from this plan alone until Dev Plan QA PASS on this tip.
- UI lanes (UI parts of Steps 2–5, Steps 7–11 UI cases, Steps 14 and 15) wait for Chief UI/UX approval of UX1 plus a UI/UX QA PASS file, per the CPM decision gate order.
- Deploy waits for Ivan's OK.
- H1 harden redeploy is a separate track.
- The process-global auth flood limiter stays paused until Chief Security approves it.
- Nothing writes `OfferStatus.Withdrawn` from this plan. Its only writer is the backlog maker-withdraw Story on `api.core.dealoware.com`, which is outside admin scope and does not block UX1.
- No password values, AWS account IDs, or keys are written anywhere, including UI copy, fixtures and examples.
- Binding Spec notes: lockout-window v5.5 `8a194eb9…`, password-rules v2.3 `1a7b342c…`, sign-in steps v1 `0a3db5f4…`. Spec QA PASS files on disk cover exactly these tips (lockout ~8:30pm ET, password-rules ~8:31pm ET, sign-in steps ~8:29pm ET). Any newer note hash voids that PASS and needs a fresh Spec QA PASS before this plan cites it.
- PoC **$0**. This plan is not a build unlock.

**Closed gates (no longer holds):** A6 Test design: design `qa/2026-10-05__qa__test-design__core-admin-dashboard.md` tip `8df392554ed887322bd7dc3f6633933bca26209c3bcfbb29321bf055f89814c9` PASS, Product QA verify `/workspace/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-ux1-a16-final-cites.md` (`1d086eb0…`). A7 on H4 live: Chief QA H4 live retest PASS `/workspace/qa/2026-10-05__qa__PASS__h4-live-retest-4911b08.path` (`331d2c63…`) → report `/workspace/qa/2026-10-05__qa__qa-report__h4-live-retest-4911b08.md` (`521e3bcc…`) + register addendum `/workspace/qa/2026-10-05__qa__qa-report__h4-live-retest-4911b08-addendum-register.md` (`71bde564…`); overall PASS, 429 with Retry-After on `/auth/token` and `/auth/register`, H4 live gate cleared. Separate cite only: Debug-off redeploy (PR #10, tip `11c35ee7…`) DevOps QA PASS `/workspace/qa/2026-10-05__qa__devops-qa-PASS__h4-redeploy-11c35ee7.path`; that redeploy alone does not close H4. Only the H4 test-participant cleanup is left, and it does not gate A7.

---

## 1. Sources (cite only; no invented requirements)

| Source | Path | Role |
|--------|------|------|
| Spec v2.2 (binding) | `specs/2026-10-05__spec__spec__core-admin-dashboard.md` | Locked decisions; §§3–12; AC map §14; Security Spec §13 (v2 pts 1–15). Tip sha256 `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51` |
| Spec QA formal PASS | `verification/2026-10-05__spec__verification__core-admin-dashboard.md` | Spec gate PASS on v2.2; sha256 MATCH; holds remaining cited |
| Spec Security SoR qa-confirm PASS 15/15 | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` | Spec Security SoR CLEAR (checklist v2) |
| Spec Security checklist (cite) | `verification/2026-10-05__security__verification__core-admin-dashboard-spec-checklist.md` | Spec-step v2 pts 1–15 — upstream; not a substitute for Dev Plan-step |
| SA SoR (amended) | `architecture/2026-10-05__sa__architecture__core-admin-dashboard.md` | Option A; CoreOwner; §§3.1–3.9 mechanism picks; IN/OUT/HOLD |
| SA Security SoR qa-confirm PASS 14/14 | `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` | SA Security SoR CLEAR |
| Arch QA formal PASS (CEO amend) | `verification/2026-10-05__sa__verification__core-admin-dashboard-ceo-amend-review.md` | Formal Architecture QA PASS on amended SoR |
| Product scope (binding 13:31) | `product/2026-10-05__product__note__core-admin-dashboard-scope.md` | IN 1–9; AC1–AC9; Product OUT pack; CEO host standing rule |
| Product QA PASS | `verification/2026-10-05__product__verification__core-admin-dashboard-scope.md` | Product requirements check PASS |
| CFO estimate | `finance/2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md` (also `2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md`) | Turnstile **$0/mo**; SES **under $0.01/mo** assumed; estimate only — not spend approval |
| Finance QA PASS | `verification/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md` | Finance QA PASS — estimate only; do not invent spend |
| Dev Plan Security checklist (binding) | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` | Dev Plan-step handshake pts **1–14** — woven in §6 |
| Expected SoR (qa-confirm only) | `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` | Handshake SoR — **do not invent points-review SoR** |
| FieldPolicy dual wall (cite) | `architecture/2026-09-21__sa__architecture__mvp-participant-secrets-acl-integration.md` §3a/§3b | Cite; do not rewrite; no parallel admin ACL |
| CEO host standing rule | COO / Product / Spec Sources | `admin.core.dealoware.com` = Core admin; platform hosts ≠ Core; Core may stay public / non-AWS; never put AWS account info in public docs/issues/PRs/messages |
| UX1 findings (rows UX1-A11, UX1-A14) | `ux/2026-10-05__ux__findings__ux1-admin-requirements.md` (sha256 `8efb1d30f9107844b635494718c8f2ba06d5f061d6c056191dc51d9637bfcfe4`) | A11: Dev Plan Steps 2–5 had no UI deliverables. A14: auth screen ownership conflict. UX1-A01–A10 = auth UI AC used in the Story DoD |
| UX1 UI requirements addendum | `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` (sha256 `599d6a182fe1f894352a91afe1774f9f6334599bb5c10eb10e07d0ac0509b799`; UI/UX QA PASS on this tip: `verification/2026-10-05__ux-qa__verification__ux1-admin-requirements.md` sha256 `ca16d61e6ac785674958529d8dc2f7d59641ab34f7270dfd60f403219901759a`) | Shared UI reference. Plan cites UXR IDs and screen IDs (S-A*, S-D*) only; UI detail is not copied here. |
| UX1 redline routing r2 | `ux/2026-10-05__ux__redlines__ux1-routing.md` (sha256 `fde55fbae19ee3a0f97d27734f902ba3694cb2d6e4f7cd42a2742e0ec0854b4d`) | Routes A11 + A14 to Dev Plan (A5). UI build lanes held until Chief UI/UX approves UX1 |
| CPM decision: A7 UI gates | `ops/2026-10-05__cpm__decision__core-admin-dashboard-a7-ui-gates.md` (sha256 `322cfa0e2b2b1f1258b0ff0b150f4e4d328381b9549dd4060b3f4a95f80311ec`) | Adds Step 14 (auth screens UI) and Step 15 (audit log viewer); Chief Developer accountable, Senior Developer implements. UI-gated Steps 2, 3, 4, 5, 7, 8, 9, 10, 11, 14, 15; Phase A / Phase B gate order |
| UX1-A16 sign-in step shape (Chief Security) | `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` (tip sha256 `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34`) | Two-step sign-in; binding conditions **1–8** for Step 14 |
| UX1-A03 lockout message (Chief Security) | `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md` (tip sha256 `0817b7676a0103d606197c07fb6ac48782c12fbd363304637e5340b86af6d3f0`) | Generic lockout/failure copy; no Retry-After; UI shows server text as returned |
| Admin.core auth UI Security answers | `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` (tip sha256 `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15`; `f03a82c9…` void) | Password helper / length / server reason inline; items 1–4 |
| Auth UI Security answers confirm | `verification/2026-10-05__security__verification__admin-core-auth-ui-security-answers-confirm.md` | Security QA confirm path (cite with answers tip `a87e293b…`) |
| Password-rules Spec note (binding) | `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` — **v2.3** sha256 `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` (CPM: v2.2 superseded) | UX1-A17 rules; cites answers `a87e293b…`. Spec QA PASS on v2.3: `verification/2026-10-05__spec__verification__core-admin-password-rules-note.md`; Security QA: `verification/2026-10-05__security__verification__core-admin-password-rules-note-confirm.md` |
| Lockout-window Spec note (binding) | `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` — **v5.5** sha256 `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` (CPM: v5.4 and v5.3 void) | OQ1/OQ3 + A16 C4 carry. Spec QA PASS on v5.5: `verification/2026-10-05__spec__verification__core-admin-lockout-window-note.md`; Security QA pt 5: `verification/2026-10-05__security__verification__core-admin-lockout-window-note-pt5-confirm.md` |
| Sign-in steps Spec note v1 (binding Spec bind for A16) | `specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` — **v1** sha256 `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4` | Spec bind for UX1-A16 conditions 1–8 (A16 decision `bb0bcaa2…` stays the Security decision cite). Security QA PASS: `verification/2026-10-05__security__verification__core-admin-signin-steps-note-confirm.md`; Spec QA PASS: `verification/2026-10-05__spec__verification__core-admin-signin-steps-note.md` |
| Product decision: UX1 Withdrawn + edit paths (binding) | `product/2026-10-05__product__decision__ux1-withdrawn-and-edit-paths.md` (sha256 `6ea05ddb799c44bddb52273214b9ccb3e4865e521b1a26d9edbb302ee3819d2e`) | Outcome 1: keep offer Withdrawn; enum in the Step 7 PR; admin cannot set it. Outcome 2: keep the SA §4 editable set; add missing write paths; do not narrow the form |
| Product QA PASS on that decision | `verification/2026-10-05__product__verification__ux1-withdrawn-and-edit-paths.md` (sha256 `4926813ff411bc7a58d2966ede8378620bd89ffa7343ca75884539a21d450e65`) | Requirements check PASS (~8:42pm ET); not a build PASS |
| SA UX1 redlines note r3 (§4 editable set) | `architecture/2026-10-05__sa__architecture__core-admin-ux1-redlines-note.md` (sha256 `0286c68eb3ab372db91a6afe7e2fb359c063447d5ba3e5d6786cfab82ee5f72b`) | §4 editable fields and allowed status changes (UX1-B10); §4 domain gap: `Withdrawn` missing from `OfferStatus`; §5 `UpdatedAt` in the Step 7 PR; §8 row 5 server-enforced transitions |
| Status-list note (Product AGREED 8:00pm ET) | `specs/2026-10-05__spec__spec__core-admin-status-list-note.md` (sha256 `0a5ff57b2c55059373494e8452ad2d0931f055aae830c1704336859bbcfaa910`) | Offer Withdrawn is a first-class Core value, offer only; negotiations stay Open / Closed / Expired |
| Chief Developer coverage list (Withdrawn + edit paths) | `/workspace/a7/2026-10-05__dev__coverage__withdrawn-and-edit-paths.md` (sha256 `6026d046d5251b7c969388b9f6a2f467ccf9b4c173c2d2f2b1f254f5f0410895`; code read at PR #20 tip `1f315ef`) | W1–W5 carried by the Step 7 PR; E1–E12 carried by the Step 9 PR; migrations none except the related `UpdatedAt` change in the Step 7 PR |
| Maker-withdraw backlog Story | `product/2026-10-05__product__story__maker-withdraw-open-offer.md` (sha256 `8a11faa841d92db70cde868d33c55a6c1043a4677e50a9a6564fef17032799e0`); Product QA PASS `verification/2026-10-05__product__verification__maker-withdraw-open-offer.md` | Only writer of offer Withdrawn; on `api.core.dealoware.com`; backlog, outside admin scope; does not block UX1. Cited so Step 7 does not invent a writer |
| A6 Test design PASS | `qa/2026-10-05__qa__test-design__core-admin-dashboard.md` (design tip `8df392554ed887322bd7dc3f6633933bca26209c3bcfbb29321bf055f89814c9`); Product QA verify `/workspace/qa/2026-10-05__qa__productqa-verify__test-design-core-admin-ux1-a16-final-cites.md` (sha256 `1d086eb000ab596125d07716465f57e4124dd6c80fea06c9a4659ef5ceb42548`) | A6 gate closed (CPM 8:41pm ET) |
| H4 live retest PASS (primary) | `/workspace/qa/2026-10-05__qa__PASS__h4-live-retest-4911b08.path` (sha256 `331d2c634b322185bc3bcaebcd45e8fc8af13fc53190e97c742e441f75108512`) → report `/workspace/qa/2026-10-05__qa__qa-report__h4-live-retest-4911b08.md` (sha256 `521e3bccff9e1ff4ddb8573f287966558db105b2c3b35a977b80eb9947983601`) + register addendum `/workspace/qa/2026-10-05__qa__qa-report__h4-live-retest-4911b08-addendum-register.md` (sha256 `71bde564610390adea474edb7858bf6d2be8b2d3f15c43edb58d5e2dc7a309a4`) | Primary evidence that the A7 gate on H4 live is closed: overall PASS; 429 with Retry-After on `/auth/token` and `/auth/register`; H4 live gate cleared. Test-participant cleanup left, does not gate A7 |
| H4 Debug-off redeploy DevOps QA PASS (separate) | `/workspace/qa/2026-10-05__qa__devops-qa-PASS__h4-redeploy-11c35ee7.path` (PR #10, tip `11c35ee7…`) | Debug-off redeploy cite only; does not by itself close H4 |

**Product alignment:** Binding Product **13:31** (includes 1:27pm patch + 1:30pm CEO final). Conflicts → PM → Product → CEO. Cost/critical → **COO → CEO**. PoC **$0**.

**Distinct tracks (cross-ref only — do not merge):** Platform admin (`admin.platform.dealoware.com`); Stage C bot-isolation Stories (#66/#67/#68/#69/#18); inbound bot connector (after this); H1–H6 harden track; A6 Test design; A7 admin build; maker-withdraw backlog Story on `api.core.dealoware.com`.

---

## 2. Restated understanding (short)

Executable plan for **SD only** (after holds lift): deliver Core owner admin dashboard at **`admin.core.dealoware.com`** inside the same Negotiation Core modular monolith (separate admin route surface; Option A). Single system superadmin **`io@aiknowhow.com`** → principal **`CoreOwner`**; auth = email + password + required TOTP; **Cloudflare Turnstile only**; bootstrap/reset mail via **SES behind mail interface** (no AWS SDK in Core app). Entities: **Participants / Artifacts / negotiations / offers** — list/view/edit/delete; all-status lists; sort/filter; server-side paging; name search; stats; confirm-before-delete; soft-delete + cascades; append-only audit (auth + edit/delete; hashed IP). FieldPolicy dual wall; **no** parallel admin ACL; **no** human users list. App assumes TFM **`net10.0`** — **do not** schedule .NET 10 retarget as this Story's work.

**This artifact is design planning only.** Do not invent Stories/code from this plan until Dev Plan QA PASS on this tip (A6 Test design gate closed). The A7 gate on H4 live is closed. Deploy waits for Ivan's OK. Do not invent password values and AWS account IDs. Do not invent process-global auth flood limiter until Chief Security explicitly approves. H1 harden redeploy is a separate track. PoC **$0**. Not a build unlock.

---

## 3. Locked decisions (Spec / SA / Product / Security → plan steps)

| # | Decision | Binding lock | Plan steps |
|---|----------|--------------|------------|
| 1 | Host | Core admin **only** at `admin.core.dealoware.com`. Platform admin / platform app hosts OUT | Steps 1, 12; Explicit OUT; Sec pt 1 |
| 2 | Principal | One CoreOwner superadmin `io@aiknowhow.com`; email+password+TOTP; no multi-admin ACL | Steps 2–5, 12; Sec pts 1–5 |
| 3 | CAPTCHA | **Turnstile only**; WAF CAPTCHA / reCAPTCHA / other CAPTCHA OUT | Steps 2, 5; Sec pt 5 |
| 4 | Mail | SES via **mail interface**; no AWS SDK in Core app; do not invent AWS account IDs | Steps 2, 6; Sec pt 7 |
| 5 | IP / audit | Raw IP only in short-lived rate-limit counters; auth audit IP = keyed **HMAC-SHA256** only | Steps 5, 8; Sec pt 6 |
| 6 | Flood limiter | Do not invent **process-global** auth flood limiter until Chief Security explicitly approves — per-IP + per-account only this slice | Step 5; Sec pts 5, 13 |
| 7 | Entities | Participants / Artifacts / negotiations / offers — list/view/edit/delete; all-status; sort/filter/paging; name search; stats | Steps 7–10; Sec pts 10–12 |
| 8 | FieldPolicy | Option A dual wall; no parallel admin ACL; no human users list | Steps 1, 7–9; Sec pt 9 |
| 9 | Confirm / soft-delete | Modal + API confirm token; soft-delete `DeletedAt`; hard delete deferred; cascades per Spec §11 / SA §3.6 | Steps 9–10; Sec pt 10 |
| 10 | Audit | Append-only; auth + edit/delete; soft-deleted toggle not audited; same-txn pairing | Step 8; Sec pt 8 |
| 11 | TFM | Assume `net10.0`; **do not** schedule .NET 10 retarget as this Story's work | Step 1; Explicit OUT |
| 12 | Gates | Do not invent Stories/build from this plan until Dev Plan QA PASS on this tip; A6 Test design gate closed (`8df39255…` PASS); A7 gate on H4 live closed (Chief QA H4 live retest PASS `4911b08…`; Debug-off redeploy `11c35ee7…` separate); deploy waits for Ivan's OK; H1 harden redeploy is a separate track | Steps 12–13; Explicit OUT; Sec pts 13–14 |
| 13 | Cost | PoC **$0**; CFO cost cite only; do not invent spend; LLM if any → COO → CEO | Cost/critical; Sec pt 13 |
| 14 | Secrets | Do not invent password values; do not invent AWS account IDs; do not invent HMAC/Turnstile/SES secrets in plan/docs/chat | All steps; Sec pts 2, 6, 7, 13 |
| 15 | UI deliverables + auth UI ownership (UX1-A11/A14) | Steps 2–5 own auth machinery and cite addendum UXR / S-A IDs for their screens. Step 14 builds the auth screens UI; Step 15 builds the audit log viewer. Chief Developer accountable, Senior Developer implements (CPM decision). UI lanes wait for Chief UI/UX approval of UX1 | Steps 2–5, 13, 14, 15; Sec pts 1–6, 8, 9, 11, 13, 14 (existing; no new points) |
| 16 | Offer Withdrawn (Product decision outcome 1; coverage W1–W5) | `OfferStatus.Withdrawn = 5` plus filter and badge in the Step 7 PR under Dev Code QA PASS; maker-only from Open, final; admin cannot set it (Open → Cancelled only); no writer until the backlog maker-withdraw Story ships on `api.core.dealoware.com`; no migration | Step 7; Explicit OUT; Sec pts 8, 9, 11, 12 |
| 17 | SA §4 edit paths (Product decision outcome 2; coverage E1–E12) | Every field the edit form shows as editable has a working FieldPolicy Write path; do not narrow the form; no fabricated Accepted / Declined / Withdrawn / Superseded; 409 concurrency plus before/after audit kept; no migration for E1–E12 | Step 9; Sec pts 8, 9, 12 |

---

## 4. Itemized SD execution steps (numbered, runnable)

**Prerequisite holds (do not skip):** Dev Plan QA PASS on this tip + SoR qa-confirm CLEAR + Chief unlock · UI lanes wait for Chief UI/UX UX1 approval plus UI/UX QA PASS · Deploy waits for Ivan's OK · H1 harden redeploy is a separate track · Do not invent password / AWS account IDs · Do not invent process-global auth flood limiter until Chief Security explicitly approves. (Closed, no longer prerequisites: A6 Test design PASS `8df39255…`; A7 gate on H4 live, Chief QA H4 live retest PASS `4911b08…`; Debug-off redeploy `11c35ee7…` cited separately.)

**Step count:** 15 steps. Steps 1–13 are the original plan; Steps 14 and 15 were added by the CPM decision (UX1-A14). A7 Story files, Brief 4 and the PM checklist are owned by CPM / Senior PM; this plan does not create them.

**Repo / host:** Extend Negotiation Core modular monolith; admin UI + admin API only at `admin.core.dealoware.com`. Assume TFM `net10.0` — do **not** schedule retarget work under this plan.

**Sibling HOLD:** Platform admin; Stage C bot Stories; inbound bot connector; settlement; SSO/IdP; charts/warehouse — **cross-ref only**.

### Step 1 — Host, route surface, CoreOwner principal, FieldPolicy bind, TFM assume

- Serve admin UI/API **only** as Core admin at `admin.core.dealoware.com` (Spec §3; SA §3.1).
- Do not invent tasks for `admin.platform.dealoware.com`, `platform.dealoware.com`, or their APIs.
- Same Core modular monolith; **separate** admin route surface (Option A — SA §2 pick A).
- Bind principal **`CoreOwner`** to single superadmin login email `io@aiknowhow.com` (SA §3.2). Do not invent multi-admin / operator ACL / human-user list on Core / Participant UI-as-admin.
- All admin reads/writes bind Domain FieldPolicy for CoreOwner (Option A §3a/§3b — cite; do not rewrite). Do not invent parallel admin ACL / second permission matrix.
- Assume app TFM **`net10.0`**. Do not invent .NET 10 retarget Stories/tasks under this plan.
- Do not invent AWS account IDs, CDK, bot-platform internals, App Runner, Cognito, MM/DC4.

**Verify checklist:**

- [ ] Admin surface documented/served only for `admin.core.dealoware.com` (Sec pt 1)
- [ ] No platform-admin host tasks in this plan's delivery path (Sec pts 1, 13)
- [ ] CoreOwner ≠ Participant UI ≠ platform human-user admin (Sec pt 1)
- [ ] FieldPolicy dual wall cited; no parallel ACL invent (Sec pt 9)
- [ ] TFM assumed `net10.0`; no retarget Story scheduled here
- [ ] No AWS account / CDK / bot-platform internals in plan or delivery notes (Sec pts 7, 13)

### Step 2 — Bootstrap + email/password (no invent password) + Turnstile on bootstrap page

Per Spec §8.1–§8.2, §8.5; SA §3.3; Sec pts 2, 5:

| Item | Plan lock |
|------|-----------|
| Identity | One superadmin `io@aiknowhow.com`; email + password |
| Bootstrap | Single-use timed out-of-band bootstrap link (≤ **24h**; SA may tighten); first password set **only** via that link |
| TOTP gate | TOTP enrollment **required before** first interactive admin session |
| Secrets | Do not invent password values in Stories, fixtures, README, chat, or commit history — env/placeholders for secrets only |
| CAPTCHA | **Turnstile only** on bootstrap password-set page; do not invent Turnstile account/site keys in plan |
| OUT | Do not invent SSO/IdP as delivered |

**Verify checklist:**

- [ ] Bootstrap is single-use + time-limited; password set only via link (Sec pt 2)
- [ ] No password value appears in code/docs/plan/chat/commit history (Sec pts 2, 13)
- [ ] First session blocked until TOTP enrolled (Sec pts 2, 3)
- [ ] Turnstile present on bootstrap password-set; no other CAPTCHA provider (Sec pt 5)
- [ ] No SSO/IdP-as-delivered tasks (Sec pts 2, 13)

**UI deliverables (UX1-A11; cite only, built in Step 14):**

- Screens S-A4 (bootstrap: set first password) and S-A5 (link no longer works); requirements UXR-A21–A26; Turnstile states UXR-A03.
- Password rules UXR-A22 follow password-rules Spec note **v2.3** `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` sha256 `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` (**binding**; Spec QA + Security QA PASS; matches Step 14 / Sources).
- Step 2 supplies the server behaviour these screens call (single-use timed link, one message for expired or used link, server-side password checks). No password value appears in UI copy, examples or fixtures (Sec pt 2).

- [ ] Story for Step 2 cites S-A4, S-A5, UXR-A21–A26, UXR-A03 and maps them to UX1-A01–A10 UI AC (cases green)

### Step 3 — TOTP required; no email OTP; hashed recovery codes

Per Spec §8.3; SA §3.3; Sec pt 3:

| Item | Plan lock |
|------|-----------|
| TOTP | Authenticator-app TOTP required on **every** login after enrollment |
| Email OTP | Do not invent email OTP fallback tasks — **NOT allowed** |
| Recovery | One-time recovery codes shown **once** at enrollment; stored **hashed**; each single-use |

**Verify checklist:**

- [ ] TOTP required on every post-enrollment login (Sec pt 3)
- [ ] Verify step proves **email OTP path absent** (Sec pt 3)
- [ ] Recovery codes hashed at rest; shown once; single-use (Sec pt 3)
- [ ] No plaintext recovery codes / TOTP secrets in logs, UI dump, audit, or plan (Sec pts 3, 8)

**UI deliverables (UX1-A11; cite only, built in Step 14):**

- Screens S-A6 (set up the authenticator app) and S-A7 (save your recovery codes); requirements UXR-A27–A32.
- Handoff: signed-in recovery-code banner UXR-A40 (after S-A3 sign-in) shows the server-supplied remaining count; it lands on the first signed-in page and is built with Step 14.
- TOTP digits/period and recovery-code count follow the SA decision (UXR-A30, UXR-A31); the plan does not set them.
- The TOTP secret shows only on S-A6 and never in errors, audit, titles, URLs or logs (UXR-A29; Sec pts 3, 8).

- [ ] Story for Step 3 cites S-A6, S-A7, UXR-A27–A32 and the UXR-A40 handoff, mapped to UX1-A01–A10 UI AC (cases green)

### Step 4 — Password reset = email link + 2FA

Per Spec §8.4; SA §3.3; Sec pt 4:

| Item | Plan lock |
|------|-----------|
| Factors | Reset requires **both** single-use short-lived email link (**1 hour**) **and** 2FA (TOTP or unused recovery code) |
| Mail body | Reset mail carries **link only** — never a password value |
| OUT | Do not invent reset that skips 2FA |

**Verify checklist:**

- [ ] Reset requires email link **and** 2FA (Sec pt 4)
- [ ] Blind / password-only reset path absent (Sec pt 4)
- [ ] Reset email contains no password value (Sec pts 4, 2)
- [ ] Reset completion audited (ties Step 8; Sec pt 8)

**UI deliverables (UX1-A11; cite only, built in Step 14):**

- Screens S-A8 (reset request), S-A9 (check your email) and S-A10 (set a new password); requirements UXR-A33–A37. Expired or used reset link uses S-A5 (UXR-A25–A26).
- Wording: this is an **email-link reset** (CPM decision §4; Spec §8.3–§8.4). The email carries a link only; emailed codes are not a factor.
- Reset request gives the same response for any well-formed email and never shows lock state (UXR-A34). Reset success does not say a lock has ended (UXR-A37).

- [ ] Story for Step 4 cites S-A8–S-A10, UXR-A33–A37 with "email-link reset" wording, mapped to UX1-A01–A10 UI AC (cases green)

### Step 5 — Turnstile only + lockout/rate limit + session (no process-global flood limiter)

Per Spec §8.5–§8.7; SA §3.3; Sec pts 5, 6, 13:

| Control | Plan lock |
|---------|-----------|
| CAPTCHA | **Cloudflare Turnstile only** on login, password-reset, and bootstrap password-set. Do not invent Turnstile account/site keys. Do not invent AWS WAF CAPTCHA / reCAPTCHA / any other CAPTCHA |
| Per-account | **5** failed password or 2FA attempts / **15 min** → lock **30 min** (or unlock via completed reset) |
| Per-IP | **20** failed logins / **15 min** → throttle **30 min** (HTTP 429); same per-IP window on reset-request |
| Process-global | Do not invent **process-global auth flood limiter** (shared budget across all IPs) until Chief Security **explicitly** approves — **per-IP + per-account only** this slice |
| Session store | Server-side session row in Core Postgres; **HttpOnly Secure SameSite=Strict** cookie on `admin.core.dealoware.com` |
| Idle / absolute | Idle **30 min** (sliding renewal); absolute **8 h** from login; after expiry full re-auth (password + TOTP) |
| Raw IP | Raw client IP lives **only** in short-lived rate-limit counters (expire with 15-/30-minute windows); **never** written to audit, other logs, or metrics (Sec pt 6) |

**Verify checklist:**

- [ ] Turnstile only on login / reset / bootstrap password-set; no WAF CAPTCHA / reCAPTCHA tasks (Sec pts 5, 13)
- [ ] Lockout numbers match Spec (5/15→30 account; 20/15→30 IP) (Sec pt 5)
- [ ] No process-global auth flood limiter scheduled without Chief Security approval (Sec pts 5, 13)
- [ ] Session idle 30m / absolute 8h; HttpOnly Secure SameSite=Strict (Sec pt 5)
- [ ] Raw IP absent from audit/logs/metrics; present only in short-lived counters (Sec pt 6)
- [ ] Do not invent Turnstile secrets in plan/docs/chat (Sec pts 5, 13)

**UI deliverables (UX1-A11; cite only, built in Step 14):**

- Screens S-A1 (sign in, step 1), S-A2 (sign in, step 2) and S-A3 (recovery code sign-in); requirements UXR-A01–A18 (S-A3 detail also UXR-A19–A20); Turnstile states UXR-A03.
- Session screens: S-A11 (session ended); idle warning and post-expiry message UXR-A38–A39.
- Lockout and throttle message: the UI shows the server's generic text as returned, with no duration, countdown, attempt counter or `Retry-After` (UXR-A01–A02; Chief Security UX1-A03 decision). Turnstile failures do not count toward lockout or throttle (UXR-A03).
- Sign-in is **two-step**, settled by UX1-A16 (tip `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34`; Spec bind: sign-in steps note v1 `0a3db5f4…`; UXR-A15, UXR-A18). A16 conditions **1–3, 5, 7** are server machinery built in this step: pending-auth token (1), one shared failure counter (2), step-2 attempt cap (3), no hints in responses (5), session only after step 2 (7). Step 14 builds the screens on top.
- No Turnstile site key or secret appears in UI code or docs (Sec pt 5).

- [ ] Story for Step 5 cites S-A1–S-A3, S-A11, UXR-A01–A18, UXR-A19–A20, UXR-A38–A39, UXR-A03, mapped to UX1-A01–A10 UI AC (cases green)

### Step 6 — SES behind mail interface; do not invent AWS

Per Spec §8.9; SA §3.3; CFO estimate; Sec pt 7:

| Item | Plan lock |
|------|-----------|
| Need | Bootstrap + password-reset email delivery |
| Provider | Amazon SES (chosen) behind a **mail interface** (port/adapter) |
| Core coupling | Do not invent direct AWS SES SDK dependency / SES types in Core app tasks |
| Do not invent | AWS account IDs, region, SES identity, from-address, provision, spend |
| Cost cite | CFO estimate: Turnstile **$0/mo**; SES **under $0.01/mo** assumed — Finance QA PASS; **not** spend approval; do not invent spend |
| PoC | **$0** until separate spend unlock |

**Verify checklist:**

- [ ] Mail sent only through mail interface / adapter (Sec pt 7)
- [ ] No AWS SES SDK PackageReference / types in Core app delivery path (Sec pt 7)
- [ ] No AWS account IDs / region / SES identity / from-address invented in plan or Stories (Sec pts 7, 13)
- [ ] CFO cost estimate cited as estimate only — not spend approval (Sec pt 7)

### Step 7 — Lists, sort/filter, paging, name search, stats (safe)

Per Spec §4.5–§4.6, §6; SA §3.8–§3.9; SA UX1 redlines note §4–§5; status-list note; Product IN 5–7; Product decision outcome 1; Sec pt 11:

| Item | Plan lock |
|------|-----------|
| Entities | Participants, Artifacts, negotiations, offers — list/view surfaces |
| Statuses | All Core statuses on negotiation/offer lists incl. first-class **`Withdrawn`**; soft-deleted via **CoreOwner-only** toggle (toggle use = read; **not** audited) |
| Sort/filter | status; created/updated; participant; artifact; value/price; negotiation id — **server-side** |
| Paging | Server-side **offset/limit**; default **50**; max **200**; do not invent client-only full-table dump |
| Name search | Case-insensitive **contains** per Product definitions on all four tables; combines with filter/paging; parameterized |
| FieldPolicy | Result payloads never include denied fields |
| Stats | Participants; open negotiations (`Status = Open` AND `DeletedAt IS NULL`); offers; accepts; declines — all exclude soft-deleted. Do not invent charts/warehouse |

**Offer Withdrawn lock (Product decision outcome 1; SA redlines note §4; coverage list W1–W5):**

| ID | Item | Plan lock |
|----|------|-----------|
| W1 | Enum value | SD adds `OfferStatus.Withdrawn = 5` in the Step 7 PR under its own Dev Code QA PASS. Current values Open=0, Accepted=1, Declined=2, Superseded=3, Cancelled=4 (coverage list, PR #20 tip `1f315ef`) |
| W2 | Domain rule | `Offer.Withdraw(callerParticipantId)`: only the maker (`FromParticipantId`), only from Open; Withdrawn is final |
| W3 | Admin | The admin cannot set Withdrawn. The server rejects any admin attempt with the generic deny. The admin's only offer change stays Open → Cancelled (Step 9, E12) |
| W4 | Filter and badge | The kept Withdrawn filter and badge are offer-only; negotiations never carry Withdrawn (status-list note) |
| W5 | Tests | Maker withdraw from Open succeeds; non-maker withdraw denied; withdraw from a final state denied; admin attempt gets the generic deny; filter returns Withdrawn rows |
| — | Migration | None for Withdrawn: status is stored as an int with no check constraint (coverage list). Do not invent any other schema, AWS or password change for this value |
| — | No writer in Step 7 | No public maker endpoint calls `Withdraw` yet. The writer is the backlog Story `product/2026-10-05__product__story__maker-withdraw-open-offer.md` (`8a11faa8…`) on `api.core.dealoware.com`, outside admin scope, and it does not block UX1. Step 7 must not invent that endpoint or any admin or Core writer. Until it ships, Withdrawn shows only through the filter and badge |
| — | Related schema change only | `UpdatedAt` on the four admin entities goes in the Step 7 PR with an EF migration ordered after the H3 baseline `20261005000000_Baseline` and PR #20's `20261006000100` (coverage list; SA redlines note §5). No other order is invented here |

**Verify checklist:**

- [ ] All-status lists + CoreOwner-only soft-deleted toggle; toggle not audited (Sec pts 8, 11)
- [ ] W1–W5 delivered in the Step 7 PR under Dev Code QA PASS; `Withdrawn = 5`; filter and badge offer-only (Sec pt 11)
- [ ] Admin attempt to set Withdrawn gets the generic deny; no endpoint or admin control writes Withdrawn in Step 7 (Sec pts 9, 12)
- [ ] No migration for Withdrawn; only the cited `UpdatedAt` migration in this PR
- [ ] Server-side paging 50/200; no client full-table dump (Sec pt 11)
- [ ] Name search per Product on all four tables; parameterized; FieldPolicy on hits (Sec pt 11)
- [ ] Stats predicates exclude soft-deleted; no charts/warehouse (Sec pt 11)
- [ ] No human-user list on Core (Sec pts 1, 13)

### Step 8 — Audit log (auth + edit/delete); IP HMAC; same-txn pairing

Per Spec §8.8, §10; SA §3.5; Sec pts 6, 8:

| Item | Plan lock |
|------|-----------|
| Storage | Append-only audit table in Core Postgres; insert-only; no update/delete from admin UI/API |
| Auth events | login_success / login_failure (reason class) / reset_request / reset_complete / totp_enroll / totp_change / recovery_code_use / `signin.second_factor_failed` (code failed after correct password; reason class bad 2FA; audit only, HMAC IP prefix; UX1-A16 condition 6 per sign-in steps note v1 `0a3db5f4…` and lockout-window note v5.5 `8a194eb9…` §8.8) |
| Entity events | Every successful admin edit/delete (+ cascade rows with correlation id) |
| Soft-deleted toggle | **Not** audited; CoreOwner-only |
| IP in auth audit | Keyed **HMAC-SHA256** of client IP (server-side rotatable secret) + failure reason class only. Do not invent unkeyed hash or raw IP in audit/logs/metrics. Do not invent HMAC key value in plan/docs/chat |
| Pairing | Same DB transaction as mutation / auth-state change; audit insert fail → **roll back** |
| Snapshots | FieldPolicy-allowed fields only; no secrets; bodies >4 KiB truncated + length + hash |
| Retention | Retain indefinitely this slice (purge later) |

**Verify checklist:**

- [ ] Auth + edit/delete (+ cascades) audited; soft-deleted toggle not audited (Sec pt 8)
- [ ] `signin.second_factor_failed` written when TOTP or recovery code fails after a correct password; public client never names the factor (A16 condition 6; Sec pts 6, 8)
- [ ] Audit not editable/deletable from admin UI/API (Sec pt 8)
- [ ] Same-txn pairing: audit fail → roll back (Sec pt 8)
- [ ] Auth audit IP = keyed HMAC-SHA256 only; raw IP not in audit/logs/metrics (Sec pt 6)
- [ ] No password / TOTP secret / recovery plaintext / HMAC key in snapshots or plan (Sec pts 6, 8, 13)

### Step 9 — Edit write surface + concurrency

Per Spec §5; SA §3.7–§3.8; SA UX1 redlines note §4; Product decision outcome 2; Sec pt 12:

| Item | Plan lock |
|------|-----------|
| Writable | Only FieldPolicy-allowed fields for CoreOwner; no second permission matrix |
| Audit | Every successful edit: before/after FieldPolicy-allowed only |
| Concurrency | Optimistic via row `Version` (or `UpdatedAt` ETag); stale → **409 Conflict** safe message; no silent overwrite |
| Secrets | Do not invent password/secret fields in edit forms, errors, or audit snapshots |
| Fail-closed | Missing CoreOwner → unauthorized; denied FieldClass → no write and no dump |

**SA §4 edit paths (Product decision outcome 2; SA redlines note §4 `0286c68e…`; coverage list E1–E12 `6026d046…`). Do not narrow.** Every field the edit form shows as editable must have a working FieldPolicy Write path. FieldPolicy Write for CoreOwner is already registered in PR #20 for each field below; the Step 9 PR adds the missing domain mutators and wires each one into the edit API. A field renders as an input only when FieldPolicy allows Write for CoreOwner.

| ID | Entity | Field or change | Path in the Step 9 PR | Rule (SA §4) |
|----|--------|-----------------|-----------------------|--------------|
| E1 | Participant | `IsActive` Suspended → Active | add `Activate()` (today only `Deactivate()`) | Active ↔ Suspended |
| E2 | Participant | `DisplayName` | wire existing `UpdateDisplayName()` | optional, 256 characters |
| E3 | Artifact | subject entity `Name` | add `SubjectEntity.UpdateName()` | required, 4096 characters |
| E4 | Artifact | subject entity `Description` | add `SubjectEntity.UpdateDescription()` | optional, may be empty, 4096 characters |
| E5 | Artifact | `OwnerParticipantId` | add `Artifact.ReassignOwner()` | existing Participant that is not soft-deleted |
| E6 | Negotiation | `EndsAt` | add `Negotiation.UpdateEndsAt()` | while Open; later than `StartsAt` when both are set |
| E7 | Negotiation | Open → Closed | wire existing `Close()`; verify it cancels open child offers in the same transaction | final, no reopen |
| E8 | Negotiation | Open → Expired | add explicit `Expire()` (today clock-only via `CheckAndApplyExpiration`) that cancels open child offers in the same transaction | final, no reopen |
| E9 | Offer | `Amount` | add `Offer.UpdateTerms(amount, currency, terms)` | while Open; decimal(18,2), 0 or more |
| E10 | Offer | `Currency` | same method as E9 | 3 characters; required when `Amount` is set |
| E11 | Offer | `Terms` | same method as E9 | while Open; 2000 characters; at least one of `Amount` or `Terms`; no contact details or other personal data added by admin |
| E12 | Offer | Open → Cancelled | wire existing `Cancel()` | the admin's only offer change |

- **No fabricated outcomes:** the admin never sets Accepted, Declined, Withdrawn or Superseded. Disallowed changes are not offered in the form, and the server rejects them with the generic deny and no state detail (SA §8 row 5).
- **Read-only by design:** Participant `Id`, `Sub`, `LoginEmail` (separate rotation flow), `ContactEmail`, `CreatedAt`; Artifact `Id`, `Intent`, values, properties, time periods, `CreatedAt`; Negotiation `Id`, `ArtifactId`, parties A and B, party intents, `StartsAt`, `CreatedAt`; Offer `Id`, `NegotiationId`, `FromParticipantId`, `ToParticipantId`, `CreatedAt`.
- **Migrations:** none for E1–E12 (coverage list). Neither Spec nor SA requires a schema change for these paths; the only related schema change is the Step 7 `UpdatedAt` migration.
- **Kept:** If-Match / 409 concurrency (SA §3.8) and the before/after audit row with FieldPolicy-allowed, non-secret fields only, on every E1–E12 write.
- **Tests per edit ID (coverage list):** valid edit succeeds and writes an audit row; rule violation gets the generic deny; stale If-Match returns 409; disallowed change gets the generic deny with no state detail.

**UI cases (cite only; addendum Part B; do not copy UI detail):** UXR-B14 (edit fields + Expire negotiation confirmation dialog) and UXR-B15 (Save/Cancel / discard guard). Cite `ux/2026-10-05__ux__addendum__ux1-admin-ui-requirements.md` tip sha256 `599d6a182fe1f894352a91afe1774f9f6334599bb5c10eb10e07d0ac0509b799`. Aligns with E8 Expire path already locked. Story UI AC / verify checklist must name B14/B15.

**Verify checklist:**

- [ ] E1–E12 each have a working FieldPolicy Write path; no editable field in the form lacks one; form not narrowed (Sec pts 9, 12)
- [ ] E7 and E8 cancel open child offers in the same transaction; Closed and Expired stay final (Sec pts 8, 12)
- [ ] Story UI AC / verify checklist name UXR-B14 and UXR-B15 (addendum Part B tip `599d6a18…`); E8 Expire aligns with UXR-B14 Expire confirmation (Sec pts 8, 12)
- [ ] Offer status change from admin is Open → Cancelled only; no fabricated Accepted / Declined / Withdrawn / Superseded (Sec pts 9, 12)
- [ ] No migration added for E1–E12 (Sec pt 12)
- [ ] Only FieldPolicy-allowed fields writable; no parallel matrix (Sec pts 9, 12)
- [ ] Edit before/after audited FieldPolicy-only (Sec pts 8, 12)
- [ ] Stale write → 409 safe conflict; no silent overwrite (Sec pt 12)
- [ ] No password/secret in edit forms / errors / audit (Sec pts 12, 13)

### Step 10 — Confirm-before-delete + soft-delete cascades

Per Spec §9, §11; SA §3.4, §3.6; Sec pt 10:

| Item | Plan lock |
|------|-----------|
| UI | Modal confirm: entity type + identity + cascade summary; cancel → unchanged + no delete audit |
| API | `POST …/delete-intent` → `confirmToken` + identity + cascade summary; delete must present token; blind DELETE rejected; token single-use **5 min**, bound to actor + entity + cascade set |
| Soft vs hard | Soft-delete default marker **`DeletedAt`**; hard delete **deferred entirely** |
| Cascades | Offer → that offer; Negotiation → negotiation + child offers (open warn); Artifact → **block** while non-deleted negotiation references it; Participant → Participant + negotiations + those offers (confirm counts); **do not** auto-delete Artifacts |
| OUT | Do not invent settlement / escrow cascades |

**Verify checklist:**

- [ ] UI modal + API confirm-token paths; blind DELETE rejected (Sec pt 10)
- [ ] Cancel leaves data unchanged and writes no delete audit (Sec pt 10)
- [ ] Soft-delete `DeletedAt`; hard delete absent this slice (Sec pt 10)
- [ ] Cascades match Spec §11 / SA §3.6; settlement cascades absent (Sec pts 10, 13)
- [ ] Cascade soft-deletes audited with correlation id (Sec pts 8, 10)

### Step 11 — Fail-closed deny for non-CoreOwner / missing TOTP / expired session

Per Spec §3, §5, §8.1; SA §3.2–§3.3; Sec pts 1, 9:

- Missing/invalid/expired session, missing TOTP step, or principal not the single CoreOwner → deny all admin routes (safe unauthorized).
- Denied FieldClass → no write and no dump into UI/errors.
- Do not invent bypass routes that skip FieldPolicy or session/TOTP gates.

**Verify checklist:**

- [ ] Non-CoreOwner / expired / missing TOTP → deny all admin entity/stats/edit/delete routes (Sec pts 1, 9)
- [ ] Denied FieldClass never dumped into UI/errors (Sec pt 9)
- [ ] No parallel ACL or FieldPolicy bypass Story (Sec pts 9, 13)

### Step 12 — Explicit holds / OUT / gate pack (do not implement)

SD must **not** deliver or unlock any of:

| OUT / hold | Note |
|-----------------|------|
| Do not invent Stories / build | From this plan until **Dev Plan QA PASS** on this tip (A6 Test design gate closed, `8df39255…` PASS) |
| A7 gate on H4 live | **Closed** (Chief QA H4 live retest PASS `4911b08…`; Debug-off redeploy `11c35ee7…` separate); test-participant cleanup does not gate A7; do not invent H4 steps here |
| Deploy held | Until **Ivan OK** |
| H1 harden redeploy | **Separate** until H1–H6(+H6b) PASS + CEO OK — do not invent H1 steps here |
| Do not invent password values | Forbidden in plan/docs/chat/code/commit history |
| Do not invent AWS account IDs | No account IDs / region / SES identity / Turnstile keys / provision / spend invent |
| Do not invent process-global auth flood limiter | Until Chief Security **explicitly** approves |
| Platform admin hosts | `admin.platform.dealoware.com` / platform app — OUT |
| Human-user list on Core | OUT |
| Participant UI as admin | OUT |
| Multiple admin humans / operator ACL | OUT |
| Email OTP as 2FA fallback | OUT |
| WAF CAPTCHA / reCAPTCHA / other CAPTCHA | OUT — **Turnstile only** |
| SSO / IdP as delivered | OUT |
| Inbound bot connector | After this track |
| Settlement / escrow / checkout | OUT |
| Charts / warehouse | OUT |
| MotorMarket / DC4 | OUT |
| App Runner / CDK invent / bot-platform internals | OUT |
| .NET 10 retarget as this Story | Held — assume `net10.0`; do not schedule retarget here |
| Hard delete this slice | Deferred |
| Process-global auth flood limiter | Held until Chief Security approves |
| A6 Test design | **Closed**: PASS on design tip `8df39255…` (Product QA verify `1d086eb0…`) |
| Admin or Core writer of offer Withdrawn | OUT of this plan; writer is the backlog maker-withdraw Story on `api.core.dealoware.com` |

**Verify checklist:**

- [ ] Do not invent Stories/build from this plan until Dev Plan QA PASS on this tip stated (Sec pts 13, 14)
- [ ] A6 Test design and the A7 gate on H4 live shown as closed with cites; no invented H4 steps (Sec pts 13, 14)
- [ ] Deploy waits for Ivan's OK; H1 harden redeploy is a separate track (Sec pt 13)
- [ ] Do not invent password / AWS / process-global flood limiter stated (Sec pts 5, 13)
- [ ] Turnstile only; SES behind mail interface; platform admin OUT (Sec pts 5, 7, 13)
- [ ] No Withdrawn writer scheduled in this plan (Sec pt 13)
- [ ] PoC $0; not a build unlock (Sec pts 13, 14)

### Step 13 — Self-verify before any handoff (after holds lift)

Before marking delivery ready for CQ / handoff (only after holds lift + unlocks):

- [ ] Host only `admin.core.dealoware.com`; CoreOwner `io@aiknowhow.com`; FieldPolicy dual wall; no parallel ACL
- [ ] Bootstrap + TOTP + reset=link+2FA + Turnstile only + lockout/session per Spec
- [ ] Raw IP counters only; audit IP keyed HMAC-SHA256; SES via mail interface; no AWS SDK in Core
- [ ] Lists/search/paging/stats/edit/delete/confirm/soft-delete/cascades/audit per Spec
- [ ] No password values; no AWS account IDs; no process-global flood limiter without approval
- [ ] Deploy / H1 / UI-lane gates respected (A6 Test design and the A7 gate on H4 live already closed)
- [ ] Step 7 W1–W5 and Step 9 E1–E12 delivered per coverage list; no Withdrawn writer; no migration beyond the cited `UpdatedAt` change
- [ ] Zero MM/DC4; PoC $0; no Cognito invent; LLM if any → COO → CEO (do not provision)
- [ ] Security Dev Plan-step pts 1–14 evidence ready for SoR qa-confirm
- [ ] UI Definition of Done (UX1-A11): each Story with a UI part includes UI AC mapped to UX1-A01–A10 through the UXR IDs cited in Steps 2–5, and those cases are green. Backend security/API gates in Steps 2–5 still apply unchanged
- [ ] Steps 14 and 15 verify checklists complete; Chief UI/UX approval plus UI/UX QA PASS on the built UI recorded at PR review (CPM decision Phase B)

### Step 14 — Auth screens UI (UX1-A14; CPM decision)

**Owner:** Chief Developer accountable; Senior Developer implements.  
**Scope:** build the auth screens UI: sign-in, 2FA enrol and verify, recovery codes, Turnstile states, email-link reset, and the lockout message. Steps 2–5 remain the auth **machinery** and carry the UI deliverable cites; Step 14 **builds** the UI against them.  
**Cites (do not copy UI detail):** addendum Part A: screen inventory S-A1–S-A11 and UXR-A01–A40 as mapped in Steps 2–5; route inventory parity UXR-A12. S-A12 security settings and UXR-A41–A44 (sign out, settings, activity panel, lock notice) build only once Spec adopts them (addendum A.4, UX1-A21); this plan does not add them on its own.  
**Story files:** the Step 14 Story file, Brief 4 fix and PM checklist rows are owned by CPM / Senior PM. This plan does not create or edit them.

**Security binds (Step 14 — cite only; no invent Spec text):**

1. **UX1-A16 two-step sign-in** — `/workspace/security-out/2026-10-05-ux1-a16-signin-step-shape-decision.md` tip sha256 `bb0bcaa28577189ca7fba1a22a5e95d4fcc77a22b7b6bebebc7bfff694688d34`. UI is **two steps** (step 1: email + password + Turnstile; step 2: TOTP or recovery code). Bind conditions **1–8** by number + short label from that decision:
   1. Pending-auth token
   2. One failure counter
   3. Step 2 attempt cap
   4. Generic failure text on both steps
   5. No hints in responses
   6. Owner notice / audit
   7. Session only after step 2
   8. Host isolation (`admin.core.dealoware.com` only)
2. **UX1-A03 lockout / failure copy** — `/workspace/security-out/2026-10-05-ux1-a03-admin-lockout-message-decision.md` tip sha256 `0817b7676a0103d606197c07fb6ac48782c12fbd363304637e5340b86af6d3f0`. Generic lockout/failure copy; no `Retry-After`; no duration, attempt count, or "locked" wording; UI shows server text as returned (UXR-A01–A02).
3. **Password answers** — `/workspace/security-out/2026-10-05-admin-core-auth-ui-security-answers.md` tip sha256 `a87e293b94bea81738607779ac8f31587cc1a7f26bfe50a55cc263b9d6d58d15` is the only current answers tip; `f03a82c9…` is **void** and must not be cited. Confirm path `verification/2026-10-05__security__verification__admin-core-auth-ui-security-answers-confirm.md`. Helper text, length check, and server reason inline per answers item 1; no invent password values.
4. **Password-rules Spec note (binding)** — `specs/2026-10-05__spec__spec__core-admin-password-rules-note.md` **v2.3** sha256 `1a7b342c46721d7c585623fc5a90371c67b99c2dbce1c733a4f50c376074c521` (v2.2 superseded). Spec QA PASS and Security QA PASS on this tip (see Sources). S-A4 / S-A10 (and any Spec-adopted S-A12 password change) follow this note.
5. **Lockout-window Spec note (binding)** — `specs/2026-10-05__spec__spec__core-admin-lockout-window-note.md` **v5.5** sha256 `8a194eb9d04ccde2f4403e25a1b9c6037bee2490c9fa3304db6d68dea81f87d3` (v5.4 and v5.3 void). Spec QA PASS and Security QA pt 5 PASS on this tip (see Sources).
6. **Sign-in steps Spec note v1** — `specs/2026-10-05__spec__spec__core-admin-signin-steps-note.md` sha256 `0a3db5f489ccb8017e4631193429841ab7c21b994a2ff644485d348c4b5b4da4`. This is the Spec bind for A16 conditions 1–8; A16 decision tip `bb0bcaa2…` (item 1) stays the Security decision cite. Security QA PASS: `verification/2026-10-05__security__verification__core-admin-signin-steps-note-confirm.md`. Spec QA PASS on this tip: `verification/2026-10-05__spec__verification__core-admin-signin-steps-note.md`.

- Every built auth route maps to one S-A row and every row has a route (UXR-A12).
- UI shows server error text as returned; no lockout, throttle or attempt logic in the UI (UXR-A01; UX1-A03 tip `0817b767…`; Sec pt 5).
- No signed-in chrome or entity data before password and 2FA both succeed (UXR-A10; A16 condition 7; Sec pts 1, 9 via Step 11 fail-closed).
- TOTP secret and recovery codes never appear in errors, audit, titles, URLs or logs (UXR-A29, UXR-A32; Sec pts 3, 8).
- No password values, Turnstile keys, HMAC keys or AWS account IDs in UI code, copy, fixtures or examples (Sec pts 2, 5, 6, 7, 13).
- **UXR-A40 recovery banner until Spec adopts S-A12:** show the dismissible banner text and server-supplied remaining count on the first signed-in page; **omit** the "Manage recovery codes" link (do not invent, stub, or route to S-A12). When Spec adopts S-A12, wire the link per UXR-A40.
- Gate order: before code, Chief UI/UX approval plus UI/UX QA PASS on the Story's UI requirements (Phase A); at PR review, Dev Code QA → Security → Chief UI/UX + UI/UX QA PASS on the built UI → Bot Manager merges (Phase B). Deploy waits for Ivan's OK.

**Verify checklist:**

- [ ] Sign-in (S-A1–S-A3), 2FA enrol and verify (S-A2, S-A6), recovery codes (S-A3, S-A7, UXR-A40 banner without S-A12 link until Spec adopts), Turnstile states (UXR-A03), email-link reset (S-A5, S-A8–S-A10), session ended (S-A11), lockout message (UXR-A01–A02) all built against cited UXR IDs (Sec pts 2–5)
- [ ] UX1-A16 tip `bb0bcaa2…` cited; two-step UI; conditions **1–8** bound by number + short label (Sec pts 2, 5, 1)
- [ ] UX1-A03 tip `0817b767…` cited on Step 14; generic copy; no Retry-After / duration / attempt / "locked"; UI shows server text as returned (Sec pt 5)
- [ ] Password answers tip `a87e293b…` + confirm path cited; helper text / length / server reason inline; no invent password values (Sec pts 2, 13)
- [ ] Password-rules note v2.3 `1a7b342c…` cited as binding; v2.2 not cited as current (Sec pt 2)
- [ ] Lockout-window note v5.5 `8a194eb9…` cited as binding; v5.4 / v5.3 not cited as current (Sec pt 5)
- [ ] Sign-in steps Spec note v1 `0a3db5f4…` cited on Step 14 as Spec bind for A16 conditions 1–8, with Security QA and Spec QA confirm paths; A16 `bb0bcaa2…` kept as Security decision cite (Sec pts 1, 2, 5)
- [ ] Story AC maps to UX1-A01–A10 via UXR IDs; addendum A.5 minimum UI cases green (Test design owns the case IDs)
- [ ] Email OTP path absent from UI (Sec pt 3); Turnstile is the only CAPTCHA (Sec pt 5)
- [ ] No secret, password, key or AWS account ID in UI source, copy or fixtures (Sec pts 2, 6, 7, 13)
- [ ] Phase A and Phase B UI/UX gates recorded (CPM decision §3)

### Step 15 — Audit log viewer (UX1-A14; CPM decision)

**Owner:** Chief Developer accountable; Senior Developer implements.  
**Scope:** read-only viewer over the Step 8 append-only audit table.  
**Cites (do not copy UI detail):** addendum Part D: screens S-D1 (audit log) and S-D2 (audit entry detail); requirements UXR-D12–D13; UI/UX gate UXR-D16. Free-text search on the audit log follows the Spec decision (UXR-D12).

- Server-side filter and paging, default 50, max 200 (same rules as Step 7; Sec pt 11).
- Before/after values show FieldPolicy-allowed fields only (Sec pts 8, 9).
- No edit or delete control anywhere in the viewer; audit stays insert-only (Sec pt 8).
- No raw IP shown; auth audit IP exists only as an **HMAC IP prefix** (keyed HMAC-SHA256 of client IP, short prefix only — never raw IP, secrets, or the HMAC key) (Sec pt 6). Viewer is on `admin.core.dealoware.com` only.
- Step 8 UI case TD-ADM-101 step 2 runs through S-D2 per **UXR-D13** (CPM decision §1 secondary).

**Verify checklist:**

- [ ] S-D1 / S-D2 built against UXR-D12–D13; server paging 50/200 (Sec pt 11)
- [ ] Denied fields never shown; no mutating control in the viewer (Sec pts 8, 9)
- [ ] Viewer shows HMAC IP prefix only; no raw IP, secret, password or HMAC key displayed; admin.core host only (Sec pts 6, 8, 13)
- [ ] Phase A and Phase B UI/UX gates recorded (CPM decision §3; UXR-D16)

---

## 5. Spec + AC mapping

| Spec / Product AC | Content | Plan step(s) |
|-------------------|---------|--------------|
| Spec Locked #1 / AC1 | Host `admin.core.dealoware.com` only | Steps 1, 12 |
| Spec Locked #2 / AC6 | CoreOwner single superadmin; FieldPolicy; no parallel ACL | Steps 1, 11 |
| Spec §4 / AC2–AC4 | List/view/edit/delete four entities; all-status; sort/filter/paging; name search | Steps 7, 9–10 |
| Spec §6 / AC5 | Overall stats; no charts/warehouse | Step 7 |
| Spec §8 / AC7 | Auth: bootstrap, TOTP, reset, Turnstile, SES mail interface, lockout, session, auth audit | Steps 2–6, 8 |
| Spec §9 / AC7 | Confirm before delete | Step 10 |
| Spec §10 / AC7 | Audit edits/deletes + auth events; hashed IP | Step 8 |
| Spec §11 / AC7 | Soft-delete + cascades | Step 10 |
| Spec §12 / AC8–AC9 | OUT pack; build/deploy held; do not invent AWS; PoC $0 | Step 12; Cost/critical |
| Spec §13 / Spec Security v2 1–15 | Upstream Spec Security bind (PASS SoR) | Sources; §6 handshake |
| Product 13:31 | Binding Product scope | Sources; Locked table |
| SA SoR | Option A + mechanism picks §§3.1–3.9 | Steps 1–11 |
| CFO estimate + Finance QA PASS | Turnstile $0; SES under $0.01/mo assumed | Step 6; Cost/critical |
| Dev Plan Security checklist 1–14 | Dev Plan-step handshake | §6 woven |
| UX1-A11 | UI deliverables cited in Steps 2–5 (UXR / S-A IDs); Story DoD maps UI AC to UX1-A01–A10 | Steps 2–5, 13 |
| UX1-A14 + CPM decision | Auth screens UI ownership: Step 14; audit log viewer: Step 15 | Steps 14, 15 |
| Product decision outcome 1 + SA §4 offer rule + coverage W1–W5 | Offer Withdrawn enum, filter and badge; maker-only; admin cannot set; no writer in this plan | Step 7 |
| Product decision outcome 2 + SA §4 editable set + coverage E1–E12 | Every editable field has a FieldPolicy Write path; allowed status changes only | Step 9 |
| UX1-A01–A10 (auth UI AC) | Screen inventory, error copy, lockout message, Turnstile states, TOTP enrol, recovery codes, reset, session, control states, WCAG 2.2 AA | Steps 2–5 cites; Step 14 build |

---

## 6. Security Dev Plan-step binding (pts 1–14 woven)

**Binding checklist:** `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-checklist.md` (**ISSUED**, pts **1–14**)  
**Upstream Spec tip sha256:** `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`  
**Upstream Spec Security SoR:** `verification/2026-10-05__security__verification__core-admin-dashboard-spec-qa-confirm.md` (**PASS** 15/15)  
**Upstream SA Security SoR:** `verification/2026-10-05__security__verification__core-admin-dashboard-sa-qa-confirm.md` (**PASS** 14/14)  
**Expected SoR (qa-confirm only):** `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` — **do not invent points-review SoR**  
**Status:** Pts **1–14 woven** below. Dev Plan QA waits until SoR qa-confirm CLEAR. Do not invent Stories/code/deploy from this plan alone. A6 Test design and the A7 gate on H4 live are closed (CPM 8:41pm ET). Do not invent process-global auth flood limiter until Chief Security explicitly approves. H1 harden redeploy is a separate track. Not a build unlock. PoC **$0**.

| # | Security point (Dev Plan checklist) | How plan addresses it | Plan section / SD step |
|---|-------------------------------------|----------------------|------------------------|
| 1 | **Host + single CoreOwner gate** — admin only `admin.core.dealoware.com`; `io@aiknowhow.com` → **CoreOwner**; do not invent platform-admin host / human-user list / Participant UI-as-admin / multi-admin ACL | Step 1 host + CoreOwner + FieldPolicy; Step 11 fail-closed; Step 12 OUT pack | Steps 1, 11, 12; Locked #1–#2; Explicit OUT |
| 2 | **Bootstrap + email/password (no invent password)** — single-use timed OOB bootstrap; password only via link; TOTP before first session; do not invent password values; do not invent SSO/IdP | Step 2 bootstrap + secrets hold; Step 12 password Forbidden | Steps 2, 12; Locked #2, #14 |
| 3 | **TOTP required; no email OTP; hashed recovery** — TOTP every login; do not invent email OTP; recovery show-once hashed single-use; verify email OTP path absent | Step 3 TOTP + recovery; verify checklist includes email OTP absent | Step 3; Locked #2 |
| 4 | **Password reset = email link + 2FA** — both factors; do not invent skip-2FA reset; mail link only | Step 4 reset locks + verify | Step 4 |
| 5 | **Turnstile only + lockout/rate + session** — Turnstile on login/reset/bootstrap; do not invent other CAPTCHA + Turnstile keys; Spec lockout numbers; session 30m/8h; HttpOnly Secure SameSite=Strict; do not invent process-global auth flood limiter until Chief Security approves | Step 5 CAPTCHA + lockout + session + flood-limiter hold; Step 12 OUT | Steps 5, 12; Locked #3, #6 |
| 6 | **Raw IP counters only; audit IP = keyed HMAC-SHA256** — raw IP only short-lived counters; audit IP keyed HMAC only; do not invent unkeyed/raw IP in audit/logs/metrics; do not invent HMAC key value | Steps 5 + 8 IP rules; verify checklists | Steps 5, 8; Locked #5 |
| 7 | **SES behind mail interface; do not invent AWS** — mail interface; do not invent SES SDK in Core; do not invent AWS account IDs / region / identity / from-address / provision / spend; CFO cost cite only | Step 6 mail interface + holds; Cost/critical | Step 6; Locked #4, #13; Cost/critical |
| 8 | **Audit: auth + edit/delete; soft-deleted toggle not audited** — append-only; auth + edit/delete + cascades; toggle CoreOwner-only not audited; same-txn pairing; do not invent secret dump | Step 8 audit; Step 7 toggle note; Steps 9–10 edit/delete | Steps 7, 8, 9, 10; Locked #10 |
| 9 | **Fail-closed FieldPolicy dual wall; no parallel ACL** — all admin I/O via FieldPolicy CoreOwner (Option A §3a/§3b cite); deny missing session/TOTP; do not invent parallel ACL / second matrix | Steps 1, 9, 11 FieldPolicy + fail-closed; Step 12 OUT | Steps 1, 9, 11, 12; Locked #8 |
| 10 | **Confirm-before-delete + soft-delete cascades** — UI modal + API confirm token; soft-delete `DeletedAt`; hard delete deferred; cascades per Spec §11 / SA; do not invent settlement cascades | Step 10 confirm + soft-delete + cascades | Step 10; Locked #9 |
| 11 | **Lists / search / paging safe** — all-status; CoreOwner soft-deleted toggle; server-side sort/filter/paging 50/200; name search; parameterized; FieldPolicy on results; stats exclude soft-deleted; do not invent client dump / charts/warehouse | Step 7 lists/search/paging/stats | Step 7; Locked #7 |
| 12 | **Edit write surface + concurrency** — FieldPolicy-only writes; before/after audit; optimistic concurrency 409; do not invent password/secret in edit/audit | Step 9 edit + concurrency | Step 9; Locked #8 |
| 13 | **OUT / holds / A7 gate pack** — do not invent Stories/code/CDK/spend/provision/**deploy**; A7 gate on **H4 live** (checklist wording; closed per CPM 8:41pm ET); H1 harden redeploy is a separate track; do not invent password / AWS IDs / process-global flood limiter; platform admin / human users / Participant UI-as-admin / multi-admin / email OTP / WAF CAPTCHA / reCAPTCHA / inbound connector / settlement / SSO OUT; PoC $0; Cost/critical → COO → CEO | Step 12 Explicit holds / OUT; Cost/critical; Status. A7 gate on H4 live now closed with cite (Chief QA H4 live retest PASS `4911b08…`; Debug-off redeploy `11c35ee7…` separate); no Withdrawn writer in this plan | Step 12; Locked #12–#14, #16; Explicit OUT; Cost/critical |
| 14 | **Traceability + handshake SoR** — cite Spec v2.2 sha256 + Spec Security SoR PASS 15/15 + SA SoR PASS 14/14 + this Dev Plan checklist; Dev Plan QA must **not** PASS until SoR qa-confirm CLEAR at `…devplan-qa-confirm.md`; handshake = qa-confirm only; do not invent Stories/code/deploy from checklist; A7 gate on H4 (closed per CPM 8:41pm ET); not build unlock; PoC $0 | This §6; Sources; Status; Done-list; A6 Test design gate closed with cite (`8df39255…` PASS) | §6; Sources; Status; Done-list Dev Plan QA; Explicit OUT |

### Handshake note (point 14)

**Dev Plan QA must not PASS** until Senior Security → Security QA SoR qa-confirm CLEAR at `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md`. Handshake SoR = **qa-confirm only** — do **not** invent points-review SoR. Do not invent Stories/code/deploy from this plan alone. A6 Test design PASS and the A7 gate on H4 live are both closed. Do not invent process-global auth flood limiter until Chief Security explicitly approves. H1 harden redeploy is a separate track. Do not skip Chief. Not a build unlock. PoC **$0**.

**UX1 amendment note:** no new Security Dev Plan-step points were added. Steps 14 and 15 and the UI deliverable cites in Steps 2–5 trace to existing pts 1–14 (mainly pts 1–6, 8, 9, 11, 13, 14). Step 14 now cites UX1-A16 / UX1-A03 / password answers under those existing points (no new checklist invent). UI cites contain no passwords, AWS account IDs or keys.

**Steps 7 and 9 amendment note:** no new Security points. The Withdrawn lock (W1–W5) and the SA §4 edit paths (E1–E12) trace to existing pts 8 (edit audit), 9 (FieldPolicy, generic deny), 11 (lists, filter and badge) and 12 (edit surface, 409). No passwords, AWS account IDs or keys added.

---

## 7. Explicit OUT

| OUT / hold | Note |
|-----------------|------|
| Do not invent Stories / build | From this plan until Dev Plan QA PASS on this tip (A6 Test design gate closed) |
| A7 gate on H4 live | Closed (Chief QA H4 live retest PASS `4911b08…`; Debug-off redeploy `11c35ee7…` separate); do not invent H4 steps |
| Deploy held | Until Ivan OK |
| H1 harden redeploy | Separate track — do not invent H1 steps |
| Do not invent password values | Forbidden |
| Do not invent AWS account IDs / CDK / bot-platform internals | Forbidden in plan / public docs / issues / PRs / messages |
| Do not invent process-global auth flood limiter | Until Chief Security explicitly approves |
| Platform admin (`admin.platform.dealoware.com`) | OUT — Core admin host only |
| Human-user list on Core | OUT |
| Participant UI as admin | OUT |
| Multiple admin humans / operator ACL | OUT |
| Email OTP 2FA fallback | OUT |
| CAPTCHA other than Turnstile | OUT |
| Direct AWS SES SDK in Core app | OUT — mail interface only |
| SSO / IdP as delivered | OUT |
| Inbound bot connector | After this |
| Settlement / escrow / checkout | OUT |
| Charts / warehouse / cost views | OUT |
| MotorMarket / DC4 | OUT |
| App Runner | OUT |
| .NET 10 retarget as this Story's work | Held — assume `net10.0` |
| Hard delete this slice | Deferred |
| Stage C bot-isolation Stories merge | Cross-ref only — do not merge |
| A6 Test design | Closed: PASS on design tip `8df39255…` |
| Admin or Core writer of offer Withdrawn; public maker withdraw endpoint | OUT — backlog Story `product/2026-10-05__product__story__maker-withdraw-open-offer.md` on `api.core.dealoware.com` |
| Migrations beyond the Withdrawn enum need and the cited Step 7 `UpdatedAt` change | Not invented (none for Withdrawn or E1–E12) |
| UI lanes (UX1) | Wait for Chief UI/UX approval of UX1 plus UI/UX QA PASS. Stories with a UI part must carry UI AC mapped to UX1-A01–A10 via the cited UXR IDs, with cases green |
| A7 Story files / Brief 4 / PM checklist | Owned by CPM / Senior PM. This plan does not create the Step 14 Story file, the Brief 4 fix or checklist files |
| S-A12 security settings + UXR-A41–A44 | Build only once Spec adopts them (UX1-A21); not added by this plan |

---

## 8. Cost/critical

PoC **$0**. This plan schedules **no** AWS account create, CDK apply, SES provision, Turnstile account invent, spend unlock, or LLM provider provision. Cite CFO estimate (`finance/2026-10-05__finance__estimate__core-admin-soft-hold-ses-turnstile.md`) + Finance QA PASS (`verification/2026-10-05__finance__qa__core-admin-soft-hold-ses-turnstile.md`): Turnstile **$0/mo**; SES **under $0.01/mo** assumed — **estimate only**, do not invent spend, **not** spend/provision approval. Do not invent AWS account IDs. Deploy waits for Ivan's OK. H1 harden redeploy is a separate track. Any named **LLM/API spend** → escalate **COO → CEO** (do **not** provision). Other paid provision → escalate **CPM → COO → CEO**. Do not invent password. Not a build unlock.

---

## 9. Done-list for Dev Plan QA

- [x] Path/name DOC-FLOW: `plans/2026-10-05__devplan__plan__core-admin-dashboard.md`
- [x] Spec v2.2 coverage Locked + §§3–12 + AC1–AC9 mapped; tip sha256 cited `25f2647767efe91b3638a90309e92451e0ae60bc861fce8177d5ed9bc4cf2e51`
- [x] Spec QA formal PASS + Spec Security SoR PASS 15/15 + Arch QA PASS + Product QA PASS + CFO estimate + Finance QA PASS cited
- [x] SA SoR + SA Security SoR PASS 14/14 cited
- [x] Itemized steps executable by SD without inventing requirements (holds explicit)
- [x] Host only `admin.core.dealoware.com`; CoreOwner `io@aiknowhow.com`; Turnstile only; SES mail interface; FieldPolicy dual wall; entities + lists/search/paging/stats/confirm/audit/soft-delete
- [x] Do not invent Stories/build from this plan until Dev Plan QA PASS on this tip; A6 Test design gate and A7 gate on H4 live closed with cites (CPM 8:41pm ET); deploy waits for Ivan's OK; do not invent password / AWS IDs; do not invent process-global flood limiter; H1 harden redeploy is a separate track
- [x] Assume `net10.0`; no retarget steps; no password values; no AWS account IDs; no platform-admin host
- [x] **Security Dev Plan-step points 1–14 all woven** with cites (table §6) — Dev Plan QA waits until SoR qa-confirm CLEAR
- [ ] SoR qa-confirm CLEAR at `verification/2026-10-05__security__verification__core-admin-dashboard-devplan-qa-confirm.md` (handshake = qa-confirm only)
- [ ] **Security QA confirm required** on Dev Plan-step pts 1–14 **before** Dev Plan QA PASS to Chief Dev Planner
- [ ] SD waits for Dev Plan QA + SoR CLEAR + Chief unlock (+ CPM per ops); A6 Test design and the H4 gate already closed
- [ ] No product code in this artifact (SD instructions only)
- [x] UX1-A11: UI deliverables cited in Steps 2–5 (S-A / UXR IDs only); Story DoD note in Step 13 and Explicit OUT
- [x] UX1-A14: Step 14 (auth screens UI) and Step 15 (audit log viewer) added; owners per CPM decision; step count 15
- [x] Security QA bounce fix: Step 14 cites A16 tip `bb0bcaa2…` conditions 1–8, A03 tip `0817b767…`, password answers tip `a87e293b…`, password-rules v2.3 `1a7b342c…`, lockout-window v5.5 `8a194eb9…`; Step 15 HMAC IP prefix wording; Sec pt lists aligned (11 + 14)
- [x] CPM Spec tip refresh: lockout-window v5.5 `8a194eb9…` and password-rules v2.3 `1a7b342c…` binding (v5.4/v5.3 void, v2.2 superseded); sign-in steps note v1 `0a3db5f4…` added to Sources, Step 14 item 6, and verify list
- [x] UX1 sources cited (findings, addendum, routing r2, CPM decision) with sha256
- [x] Steps 7 and 9 scope amend: Step 7 W1–W5 (`OfferStatus.Withdrawn = 5`, maker-only, admin generic deny, filter and badge offer-only, tests, no migration, no writer until the backlog Story); Step 9 E1–E12 SA §4 edit paths, not narrowed, no migrations, 409 + audit kept; Locked #16–#17
- [x] Sources add Product decision `6ea05ddb…`, Product QA PASS `4926813f…`, SA redlines note `0286c68e…`, status-list note `0a5ff57b…`, coverage list `6026d046…`, maker-withdraw Story `8a11faa8…`, Test design PASS `8df39255…` + verify `1d086eb0…`, H4 live retest PASS `331d2c63…` / report `521e3bcc…` / register addendum `71bde564…` (primary), H4 Debug-off redeploy DevOps QA PASS `11c35ee7…` (separate)
- [x] Active holds refreshed (CPM 8:41pm ET): Test design and A7/H4 holds removed with cites; UI lanes, deploy, secrets, H1, flood limiter kept; Withdrawn no-writer line added

**Next:** Dev Plan QA waits until SoR qa-confirm CLEAR → Dev Plan QA verify with evidence → ask Senior Security → Security QA SoR qa-confirm → Chief Security PASS/HOLD to Chief Dev Planner + CPM. **Dev Plan QA must NOT PASS until SoR qa-confirm CLEAR.** A6 Test design PASS and the A7 gate on H4 live are closed. Deploy waits for Ivan's OK. H1 harden redeploy is a separate track. Do not invent password / AWS / process-global flood limiter. Not a build unlock. PoC **$0**.

**Next (UX1):** UI lanes, including Steps 14 and 15, start only after Chief UI/UX approves UX1 and a UI/UX QA PASS file exists. Stories with a UI part must include UI AC mapped to UX1-A01–A10 via the UXR IDs cited in Steps 2–5, with cases green.

---

## 10. Done-list for SD (only after Dev Plan QA PASS on this tip and Chief unlock; A6 Test design and the H4 gate are closed; UI lanes also need Chief UI/UX UX1 approval)

- [ ] Step 1: Host / CoreOwner / FieldPolicy / TFM assume
- [ ] Step 2: Bootstrap + Turnstile on bootstrap; no invent password
- [ ] Step 3: TOTP + hashed recovery; email OTP absent
- [ ] Step 4: Reset = email link + 2FA
- [ ] Step 5: Turnstile + lockout/rate + session; raw IP counters only; no process-global flood limiter without approval
- [ ] Step 6: SES via mail interface; do not invent AWS
- [ ] Step 7: Lists / search / paging / stats; W1–W5 Withdrawn enum, filter and badge (no writer); `UpdatedAt` migration
- [ ] Step 8: Audit + keyed HMAC IP
- [ ] Step 9: Edit + concurrency; E1–E12 SA §4 edit paths (no migrations)
- [ ] Step 10: Confirm-delete + soft-delete cascades
- [ ] Step 11: Fail-closed non-CoreOwner
- [ ] Step 12: Respect Explicit holds / OUT / gates
- [ ] Step 13: Self-verify checklist (incl. UI DoD for UX1-A01–A10)
- [ ] Step 14: Auth screens UI per cited S-A / UXR IDs; Phase A and Phase B UI/UX gates
- [ ] Step 15: Audit log viewer per S-D1 / S-D2, UXR-D12–D13; read-only
- [ ] Hand off to CQ gate (never skip CQ)
