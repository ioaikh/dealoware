# AC-06 fixture reports

These files are real Playwright 1.48 JSON reporter output, captured by
`e2e/admin/ci/capture-fixtures.js` from the throwaway specs in
`e2e/admin/ci/fixture-src/`. They are not hand-written.

| File | How it was captured | Guard |
|---|---|---|
| `all-skipped.json` | `test.skip`, `test.fixme`, and `test.skip(true, …)` | red (`executed=0`) |
| `zero-tests-empty-glob.json` | empty testDir / no matching files | red (`executed=0`) |
| `zero-tests-grep.json` | `--grep NO_MATCH_AC06_ZERO_TESTS` | red (`executed=0`) |
| `normal-passed.json` | one passing test | green (`executed=1`) |
| `mixed-skip.json` | one pass + one skip | green (`executed=1`) |

Regenerate:

```bash
cd e2e
node admin/ci/capture-fixtures.js
```

The capture script and `assert-executed.js` print only counts. They never
print env or URL values.
