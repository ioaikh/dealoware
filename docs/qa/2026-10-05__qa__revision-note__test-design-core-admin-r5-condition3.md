# Revision note — Core admin test design r5 (UX1 condition 3 + TD-ADM-062 + redlines v2)

| Field | Value |
|-------|--------|
| Written by | Dealoware Chief QA |
| Date | 2026-10-05 ~9:25pm ET |
| Design | `/workspace/dealoware-kb/qa/2026-10-05__qa__test-design__core-admin-dashboard.md` |
| Design tip sha256 | `7bb1dd3b24a630dfca10c3b96969aa1f7d4815108d83fbc497475e1a1864a26f` |
| Prior tip | `8df392554ed887322bd7dc3f6633933bca26209c3bcfbb29321bf055f89814c9` (archived) |
| PoC | $0 |

## Why

1. **TD-ADM-062** listed Withdrawn as a negotiation status. Status-list note `0a5ff57b…` and Product decision `6ea05ddb…` lock Withdrawn as **offer-only**.
2. **UX1 approval condition 3:** fill Part B, C and X test cases (incl. UXR-B14 Expire and UXR-B15) before Steps 7, 9, 10 and 15 UI PRs merge.
3. **Spec UX1 redlines note v2** `eb63276f…` adds TOTP/recovery/no-store/step-up/sign-out/return-path/transition/S-D3–D4 surface.

## Delta (see design §0 for full tables)

- Case count **127** (was 114, +13): P0 88 / P1 34 / P2 5.
- TD-ADM-062: negotiations Open/Closed/Expired only; TD-ADM-063 keeps Withdrawn offer-only.
- B/C/X stubs filled; new `TD-ADM-UI-na-expire` (UXR-B14); UXR-B15 → `TD-ADM-UI-na-B11`.
- New API cases TD-ADM-170…180; auth-24 (UXR-A45).
- Condition 3 met only after Senior Product QA PASS on this tip.

## Active holds

See design §0 single Active holds list (Test design QA; UI PR gates; S-A12 current-password OQ16; Condition 4 Dev Code QA; X08 CI; deploy Ivan OK).

