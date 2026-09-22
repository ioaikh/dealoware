# CQ Assessment — MVP Stage A Field ACL registry #31 / PR #37 — NO REFACTOR

**Author:** Dealoware Senior CQ  
**Date:** 2026-09-21  
**Verdict:** **cq:no-refactor**  
**Issue:** https://github.com/ioaikh/dealoware/issues/31  
**PR:** https://github.com/ioaikh/dealoware/pull/37 · HEAD `2228718eb7d5f7e7e0a95ac984a01532055c398b`  
**Confirm path:** CQ QA → Chief CQ → CPM gate  
**Constraints:** Stage A only; #32 separate; Stage B/C + #24 HOLD; Soft DisplayName non-blocking (CPM); PoC $0; no Cognito/SSO/vault; no MM/DC4.

## Verdict

No refactor requirement spec. Domain `IFieldPolicy` deny-by-default + Profile ACL projection meet Stage A; OUT honored. Soft notes **non-gate**.

## Evidence

1. **Domain FieldAcl:** Extensible `FieldClass`; `FieldAction` / `PrincipalType` / `FieldPrincipal` / `FieldResourceContext`; `IFieldPolicy` + `FieldPolicy` deny-by-default (unknown → deny; ShareOutbound → deny).
2. **Starters:** LoginEmail User R/W/L only (OwnAgent deny); ContactEmail User R/W/L + OwnAgent Read; DisplayName User∪OwnAgent R/W/L + Counterparty Read (soft AC implemented).
3. **Application:** ProfileResponse/UpdateProfileRequest; ProfileMapper omits denied via null + Includes* flags.
4. **API:** GET/PATCH `/profile` AuthHelper fail-closed (401); Write deny → 403 ProblemDetails Title only; MapProfileEndpoints wired.
5. **Infra:** `AddSingleton<IFieldPolicy, FieldPolicy>`; Participant LoginEmail/ContactEmail columns.
6. **Tests:** FieldAclTests 29 cases — owner OK, OwnAgent LoginEmail deny / ContactEmail Read, stranger/counterparty deny, unauth 401, unknown deny, ShareOutbound deny, health open.
7. **#7/#32:** IdentitySealTests + StageAFailClosedTests untouched (not in PR file set).
8. **OUT:** No Stage B Strategy ACL / ShareOutbound-after-Accept behavior; no Stage C agent hard-wall / HTTP OwnAgent; no #24; no Cognito/SSO/vault/MM/DC4.
9. **Peer PASS (KB):** SD verify + Security QA 10/10 matching HEAD; CI SUCCESS; Spec/plan present.

## Soft notes (explicitly non-gate)

| Note | Why non-gate |
|------|----------------|
| Projection uses null + Includes* (not JsonIgnore/WhenWritingNull) | SD/Security soft; omit-by-null acceptable Stage A |
| OwnAgent path unit-tested only (HTTP always User principal) | Stage A API half; OwnAgent gateway = later |
| `HasAcceptedShareGrant` unused; ShareOutbound always deny | Stage B prep stub — not inventing ShareOutbound-after-Accept |
| DisplayName fully implemented though Spec soft | CPM non-blocking; tests cover Counterparty Read |
| Repeated 403 blocks in ProfileEndpoints | Small duplication |
| Domain Update* setters without ACL (enforced at API) | Same pattern as prior Stories; API is evaluate plane |

## Affected functionality (QA coordination)

1. `GET /profile` — ACL-projected fields; unauth 401  
2. `PATCH /profile` — Write ACL; deny → 403 without private fields  
3. FieldPolicy Evaluate deny-by-default (LoginEmail/ContactEmail/DisplayName + unknown + ShareOutbound)  
4. OwnAgent LoginEmail deny / ContactEmail Read (unit)  
5. `/health` still open  
6. Zero Stage B/C / #24 / Cognito / MM/DC4 invent; #7/#32 suites unchanged  

## Done-list for CQ QA

- [ ] Domain policy deny-by-default; Profile projection; fail-closed auth
- [ ] Soft notes non-gate (incl. DisplayName soft; null+Includes*; OwnAgent HTTP not wired)
- [ ] Affected-functionality complete
- [ ] No Stage B/C / #24 / Cognito / MM/DC4 invent
- [ ] Confirm PASS to Chief CQ only

