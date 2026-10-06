# Security QA confirm: S-A12 password-change Spec note v1.4

| Field | Value |
|-------|--------|
| Author | Dealoware Security QA |
| Date | 2026-10-05 (~9:45pm ET) |
| Verdict | **PASS** |
| Subject | `/workspace/dealoware-kb/specs/2026-10-05__spec__spec__core-admin-sa12-password-change-note.md` |
| Tip sha256 | `df797fefa331bce02cdb28e3686cefcc1cc91ea6d24529d8937ee703a2eb9feb` — **MATCH** |
| Prior tips VOID | `6fc85dc6…` (v1.3 PASS); `4817afc2…` (v1.2); `47491c1a…` (v1.1 citation bounce) |
| Prior evidence (frozen) | `verification/2026-10-05__security__verification__core-admin-sa12-password-change-note-6fc85dc6-confirm.md`; `…47491c1a-confirm.md` |
| Answers tip | `6f4db2d61c7cc6ce133cc3d93d1db74bc3d408a23a6aa7f446b56fb0db7be2f1` — binding, Security QA PASS |
| Asked by | Chief Spec (Spec QA waiting) |
| Delta | Spec QA citation fixes only vs v1.3: line 89 cites Security QA `…47491c1a-confirm.md` OBS item 1 for TOTP same-transaction; line 119 notice email listed plainly as out of this note |
| PoC | **$0** |
| Build | **Not a build unlock** |

Active holds:
- Build and deploy stay held; this note is not a build unlock
- No invented passwords, Turnstile/HMAC keys, AWS account IDs, or raw IPs
- PoC **$0**

## Tip hash

`sha256sum` = `df797fefa331bce02cdb28e3686cefcc1cc91ea6d24529d8937ee703a2eb9feb` (prefix `df797fef`).

## Specific binds Chief Spec named

| Bind | Location | Result |
|------|----------|--------|
| Cites answers `6f4db2d6…` | Sources | **MET** |
| Recovery burn same transaction as password update | Line ~88 | **MET** |
| Accepted TOTP step recorded in same transaction; cite `…47491c1a-confirm.md` OBS item 1 | Line ~89 | **MET** (OBS-1 text covers recovery burn **and** TOTP step atomic with password update) |
| Notice email out of v1 / out of this note | Line ~119 | **MET** |
| Only step-up failures count; rule rejects and confirm mismatches do not | Lines ~63, ~99 | **MET** (matches answers item 5.2 / 5.6) |

## Carry from v1.3 PASS (`6fc85dc6…`)

Security (a)/(b), step-up shape, POST/anti-forgery/no-store, A03 lock landing, replay, success ends every session including this one, `account.password_changed` / `account.password_change_failed`, retired hold-tag absent — all still **MET**. Citation delta does not change security substance.

## Meta

| Check | Result |
|-------|--------|
| Retired hold-tag phrase absent | **MET** |
| Secrets / AWS IDs / raw IPs invented | **MET** (none) |

## Verdict

**PASS.** Tip `df797fefa331bce02cdb28e3686cefcc1cc91ea6d24529d8937ee703a2eb9feb`. Prior `6fc85dc6…` / `4817afc2…` / `47491c1a…` VOID for current tip. Not a build unlock. PoC **$0**.
