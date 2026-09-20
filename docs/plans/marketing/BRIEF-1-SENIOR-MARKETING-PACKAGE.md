# Marketing Brief #1 — Senior Marketing Package

| Field | Value |
|-------|-------|
| **Brief** | #1 |
| **Author** | Dealoware Senior Marketing |
| **Date** | 2026-09-10 (ET) |
| **Status** | READY FOR QA |
| **Audience** | Marketing QA → Chief Marketing (claims lock via Chief Product before any public ship) |
| **Do not present to** | COO / CEO — QA confirms to Chief Marketing only |

---

## 1. Product restatement (honest, Product-aligned)

Grounded only in KB Product docs and the public GitHub README. No traction, savings %, or "at scale" claims.

1. **Intermediary, not a marketplace settlement layer** — Dealoware sits between Participants; it does not buy/sell/own Artifacts by default, and settlement/checkout/escrow are out of scope unless explicitly added later.  
   *Sources:* `product/PRODUCT-BRIEF.md` ("Claims lock"; "Explicit settlement policy: intermediary only / no escrow"); `product/CEO-ORIGINAL-BRIEF.md` (Later CEO decisions #4 re settlement policy).

2. **Negotiate any Artifact** — Subject can be goods, services, collectibles, exchanges, or collections of entities, with Intent / Value / Location / Time. Domain is deliberately generic, not one vertical.  
   *Sources:* `product/CEO-ORIGINAL-BRIEF.md` (Artifact properties); `product/PRODUCT-BRIEF.md` (Core model → Artifact); GitHub `README.md` ("Negotiate *anything*").

3. **People and AI agents are first-class Participants** — Registered humans and AI Agents use the same negotiation model (backend API + minimal UI).  
   *Sources:* `product/CEO-ORIGINAL-BRIEF.md` ("Platform can be used by real people as well as AI Agents"); `product/PRODUCT-BRIEF.md` (one-liner; Participant).

4. **Strategy-driven AI Assistants** — Assistants follow Participant-defined Strategies (when/how to start, demand, concession, accept/decline/counter/close, guardrails) end-to-end for discovered Artifacts. Participants may bring their own model.  
   *Sources:* `product/CEO-ORIGINAL-BRIEF.md` (Manage Negotiation Strategies; AI Assistant); `product/PRODUCT-BRIEF.md` (Strategy; AI Assistant).

5. **Identity protected until Accept** — AI Negotiation agents protect user identity until successful negotiation; contact exchange happens on accepted offer. On-platform communication is with one's own agent only (no direct human↔human chat).  
   *Sources:* `product/CEO-ORIGINAL-BRIEF.md` (§7 identity; §6 no direct communication); `product/PRODUCT-BRIEF.md` (AI Assistant; PII vault addition).

6. **Apache License 2.0 open-source core** — Code is public on GitHub under Apache-2.0; contributions welcome under that license.  
   *Sources:* `product/PRODUCT-BRIEF.md` (Later CEO decisions; Source control & licensing); `product/CEO-ORIGINAL-BRIEF.md` (Later CEO decisions #1–#2); GitHub license SPDX `Apache-2.0`; repo `LICENSE` / `README.md`.

7. **Hosted platform remains AIKnowHow / Dealoware** — The public repo is the OSS core to read/run/improve; the hosted service is not a free-hosting fork of "the" platform.  
   *Sources:* `product/PRODUCT-BRIEF.md` (Hosted platform); `product/CEO-ORIGINAL-BRIEF.md` (Later CEO decisions #2); GitHub `README.md` / `CONTRIBUTING.md`.

8. **Distribution via agentic systems + basic UI; no escrow implied** — Integration path includes chats/bots (e.g. Claude, Grok Bot), OpenAPI/webhooks/MCP (agreed early additions), plus minimal UI — still intermediary-only.  
   *Sources:* `product/CEO-ORIGINAL-BRIEF.md` (Distribution); `product/PRODUCT-BRIEF.md` (Distribution; early additions #7, #12).

---

## 2. Research findings — channels, angles, hooks

Research date: 2026-09-10 (ET). Focus: high-attention, low/no-spend paths for **OSS + agentic platforms**. Every claim cites a public source.

### 2.1 Channel patterns (trusted public sources)

| Channel | What reputable sources say | Attention vs effort | Paid? |
|---------|---------------------------|---------------------|-------|
| **GitHub README as landing page** | OSS marketing treats the repo README as the primary storefront; install command and clear value prop above the fold drive trial. | High foundation / low cost | **N** (organic) |
| **Show HN (Hacker News)** | Official rules: only for something people can try now; no signup walls preferred; early-stage OK; never ask friends to upvote. Timing guides suggest weekday morning ET + author presence 2–3h. | Very high spike / medium–high effort when demo-ready | **N** |
| **Product Hunt** | Official launch guide: weekday 12:01 AM PT common; engage comments; do not ask for upvotes. Awesome-PH / Lenny guides emphasize prep assets + warm community. | High spike / high prep effort | **N** organic; **FLAG paid** hunters/boosts |
| **Reddit** (`r/opensource`, niche tech) | Contribution-first; most subs ban raw promo. `r/LocalLLaMA` enforces self-promo limits (~10% of content), karma gates, anti-slop — poor fit for pure hosted SaaS pitches; better for local/BYO-model angles only if relevant. | Medium / high trust cost if done wrong | **N** |
| **Dev.to / Hashnode** | "How I built X" / architecture posts 1–2 days before launch; use `canonical_url`; avoid pure promo. | Medium sustained / medium effort | **N** |
| **X/Twitter build-in-public** | Threads with problem hook, demo GIF, honest limitation, link late; Tue–Thu ~9–11 AM ET; reply hard first 2h. | Medium / ongoing effort | **N** organic; **FLAG paid** creator blocks |
| **Awesome lists / MCP directories** | PR into curated lists and MCP registries once a real MCP connector exists; free discovery compound. | Medium long-tail / low ongoing cost | **N** |
| **Paid ads / sponsored newsletters / paid creators** | Not recommended as primary path for early OSS trust. | Variable / cash cost | **Y — FLAG** |

**Citations (channels):**
- [Show HN Guidelines](https://news.ycombinator.com/showhn.html) — official tryable-product rules.
- [Hacker News Guidelines](https://news.ycombinator.com/newsguidelines.html) — no vote solicitation; no hype titles.
- [Show HN: How to Launch on Hacker News (2026 Guide) · Favors.dev](https://favors.dev/blog/show-hn-launch-guide) — presence > timing charts.
- [How to Launch on Hacker News: The Show HN Guide (Real Data) - DEV Community](https://dev.to/iris1031/how-to-launch-on-hacker-news-the-show-hn-guide-real-data-461e) — OSS + runnable demo resonance; architecture honesty.
- [Product Hunt Launch Guide](https://www.producthunt.com/launch) — official PH launch basics.
- [product-hunt-launch-guide.md (awesome-product-hunt)](https://github.com/fmerian/awesome-product-hunt/blob/main/product-hunt-launch-guide.md) — tagline ≤60 chars; real screenshots; 12:01 AM PT pattern.
- [How to successfully launch on Product Hunt (Lenny's Newsletter)](https://www.lennysnewsletter.com/p/how-to-successfully-launch-on-product) — warm audience; engagement quality over vanity rank.
- [Open Source Marketing: The Complete 2026 Guide](https://gingiris.tools/blog/2026/04/03/open-source-marketing-the-complete-guide/) — README as landing page; trust > ad spend.
- [Developer Marketing for OSS: 7 Channels Behind 60k+ Stars (2026)](https://gingiris.tools/blog/2026/03/25/developer-marketing-101-how-to-grow-your-open-source-project/) — HN/Reddit/Dev.to/X/GitHub mix; engage before promo.
- [Open Source Marketing: How to Market an Open Source Project (Stackmatix)](https://www.stackmatix.com/blog/open-source-marketing) — stars ≠ adoption; technical content + repo experience.
- [Open Source Marketing Strategy (Infrasity)](https://www.infrasity.com/blog/open-source-marketing-strategy) — Reddit contribution not spam; Awesome lists in the flywheel.
- [Content Distribution for Open Source DevTools (Infrasity)](https://www.infrasity.com/blog/content-distribution-for-open-source-devtools) — Dev.to canonical URLs; awesome lists; comparison content.
- [Editor Guide - DEV Community](https://dev.to/p/editor_guide) — markdown/`canonical_url` mechanics.
- [r/LocalLLaMA Rule Updates](https://reddit.com/r/LocalLLaMA/comments/1su3ao4/rlocalllama_rule_updates/) — karma gates; anti-slop; self-promo enforcement.
- [Submit Your MCP Server | MCP Find](https://mcpfind.org/submit) / [MCPFind CONTRIBUTING](https://github.com/MCPFind/mcp-find/blob/main/CONTRIBUTING.md) — free OSS MCP listing requirements.
- [tianzhou/awesome-mcp-servers](https://github.com/tianzhou/awesome-mcp-servers) — curated MCP discovery via PR.
- [How to Write an X Post for Open Source Launches 2026](https://postinstantly.com/guides/how-to-write-an-x-post-announcing-you-open-sourced-your-side-project) — problem→solution hooks; repo readiness.
- [The Twitter/X Launch Playbook for 2026 (welaunch.sh)](https://www.welaunch.sh/blog/the-twitter-x-launch-playbook-for-2026-threads-timing-and-templates-that-actuall) — thread structure; honest limitation.
- [Launch Day Twitter Strategy 2026 (Flowjam)](https://www.flowjam.com/blog/launch-day-twitter-strategy-2026) — documents paid creator block rates ($50–$3,000) → **FLAG paid**.

### 2.2 Positioning angles (Product-safe)

Use only language that survives Claims lock (`product/PRODUCT-BRIEF.md`):

| Angle | Hook sketch (draft; claims-lock before ship) | Avoid |
|-------|-----------------------------------------------|-------|
| **Universal Artifact negotiation** | "OSS intermediary to negotiate any Artifact — goods, services, collectibles — via API." | Vertical lock-in; "replaces eBay/Airbnb" |
| **People + agents, same model** | "Humans and AI agents share one negotiation protocol." | Unproven multi-agent "at scale" |
| **Strategy-driven assistants** | "Encode when to start, concede, accept — assistants execute; you set guardrails." | Savings % / win-rate claims |
| **Identity until Accept** | "Contact sealed until Accept — negotiate without exposing identity early." | "Bank-grade" / unverified security superlatives |
| **Apache-2.0 + hosted split** | "Read/run the core under Apache-2.0; hosted remains AIKnowHow." | "Free hosted forever" / fork-as-platform |
| **Intermediary-only honesty** | "We broker the negotiation path — not settlement/escrow (unless added later)." | Marketplace GMV / escrow trust theater |
| **MCP / bot distribution** | "Built for agentic clients (OpenAPI, webhooks, MCP) plus minimal UI." | Claim MCP shipped before it exists |

### 2.3 Launch hooks (timing gated by Product readiness)

Per Show HN official rules, **do not Show HN until strangers can run or try something**. Current public repo is early PoC (health/scaffold era per GitHub README structure) — so hooks below are sequenced.

1. **Repo-ready hook** — Fill GitHub About (description, topics, homepage), tighten README first screen to Product one-liner + `dotnet run` + link to `docs/product/PRODUCT-BRIEF.md`.
2. **Build-in-public hook** — Short posts on Strategy model / identity-until-accept / intermediary policy (no metrics fiction).
3. **Technical essay hook** — Dev.to: "Why an intermediary negotiation API for agents (and why no escrow yet)."
4. **Show HN hook** — Only when a runnable slice lets a visitor create Artifact / start negotiation or a clear demo path without signup theater.
5. **MCP/directory hook** — When MCP connector ships: submit to MCP Find / awesome-mcp lists (free PRs).
6. **Product Hunt hook** — After a tryable surface + screenshots; organic only (no paid upvote/hunter packages as plan).

### 2.4 Repo snapshot (as of research)

- **URL:** https://github.com/ioaikh/dealoware — **public**, license **Apache-2.0** (`gh api` SPDX).
- **README positioning:** Aligns with Product (Universal Negotiation Platform; people + AI; identity until accept; Apache-2.0; hosted = AIKnowHow).
- **Gaps:** GitHub `description` / `homepage` / `topics` empty; star/fork count at zero (expected for early PoC — **do not invent traction**); docs mirror present under `docs/product/`.

---

## 3. Ranked recommendations — TOP 5

Ranked by **expected attention ÷ effort/cost**, with **organic / $0 as the plan**. Anything that costs money is flagged and is **not** the primary path.

### #1 — Make GitHub the marketing homepage (topics, About, README first screen)

- **Why:** Multiple OSS guides treat README + repo metadata as the conversion surface for every other channel's traffic. Current About fields are empty — free win before any launch post.
- **Expected attention vs effort/cost:** High leverage / **low effort / $0**.
- **Sources:** [Open Source Marketing 2026 Guide (Gingiris)](https://gingiris.tools/blog/2026/04/03/open-source-marketing-the-complete-guide/); [Stackmatix OSS marketing](https://www.stackmatix.com/blog/open-source-marketing); GitHub README already Product-aligned.
- **Paid?** **N**
- **Notes:** Suggested topics only after Product OK (examples for lock review: `negotiation`, `agents`, `dotnet`, `apache-2.0`, `mcp` when true). Do not claim features not in repo.

### #2 — Organic technical content: Dev.to / Hashnode "How we're building Dealoware" (intermediary + Strategy + identity)

- **Why:** Long-form indexes for search and gives Show HN / Reddit something substantive to link; canonical URL keeps ownership.
- **Expected attention vs effort/cost:** Medium sustained / **medium effort / $0**.
- **Sources:** [Content Distribution for Open Source DevTools](https://www.infrasity.com/blog/content-distribution-for-open-source-devtools); [DEV Editor Guide](https://dev.to/p/editor_guide); [daily.dev OSS launch guide](https://business.daily.dev/resources/promote-open-source-project-step-by-step-launch-guide/).
- **Paid?** **N**
- **Claims hygiene:** Quote Product claims lock; no savings %, GMV, or "at scale."

### #3 — Build-in-public on X (organic threads only) + optional LinkedIn cross-post

- **Why:** Continuous attention while PoC matures; thread formula (problem → demo → honest limitation → link last) matches developer norms.
- **Expected attention vs effort/cost:** Medium / **ongoing effort / $0** if organic.
- **Sources:** [X OSS launch post guide 2026](https://postinstantly.com/guides/how-to-write-an-x-post-announcing-you-open-sourced-your-side-project); [welaunch.sh X playbook 2026](https://www.welaunch.sh/blog/the-twitter-x-launch-playbook-for-2026-threads-timing-and-templates-that-actuall).
- **Paid?** **N** for organic. **FLAG:** paid creator amplification blocks documented at ~$50–$3,000 ([Flowjam](https://www.flowjam.com/blog/launch-day-twitter-strategy-2026)) — **not recommended**; would need COO→CEO if ever proposed.

### #4 — Show HN when a stranger-tryable PoC exists (not before)

- **Why:** Highest single-day developer attention for OSS — but official rules forbid Show HN without something to try; early scaffold-only posts burn the channel.
- **Expected attention vs effort/cost:** **Very high** spike / medium effort **after** demo readiness / $0.
- **Sources:** [Show HN Guidelines](https://news.ycombinator.com/showhn.html); [Favors.dev 2026 Show HN guide](https://favors.dev/blog/show-hn-launch-guide); [DEV Community Show HN guide](https://dev.to/iris1031/how-to-launch-on-hacker-news-the-show-hn-guide-real-data-461e).
- **Paid?** **N**
- **Gate:** Product/PM confirm runnable path (local `dotnet run` demo or sandbox) + claims-locked title/first comment. No upvote brigades (HN Guidelines).

### #5 — Community presence + directories: Reddit contribution-first; Awesome/MCP listings when connectors exist

- **Why:** Compounds discovery without spend; MCP directories match Product distribution (bots/MCP) once an MCP surface ships. Reddit is feedback-rich if non-spammy.
- **Expected attention vs effort/cost:** Medium long-tail / **low–medium effort / $0**; high ban risk if promotional.
- **Sources:** [Infrasity OSS strategy](https://www.infrasity.com/blog/open-source-marketing-strategy); [Gingiris developer marketing](https://gingiris.tools/blog/2026/03/25/developer-marketing-101-how-to-grow-your-open-source-project/); [MCP Find submit](https://mcpfind.org/submit); [awesome-mcp-servers](https://github.com/tianzhou/awesome-mcp-servers); [r/LocalLLaMA rules](https://reddit.com/r/LocalLLaMA/comments/1su3ao4/rlocalllama_rule_updates/).
- **Paid?** **N**
- **Subs (engage first):** `r/opensource`, agent/MCP-oriented communities; treat `r/LocalLLaMA` as optional and only for BYO-local-model angles with strict self-promo limits. Prefer discussion norms over drive-by links in ML communities.

### Explicitly deferred / flagged (not in TOP 5 primary plan)

| Option | Why deferred | Paid? |
|--------|--------------|-------|
| Product Hunt launch | High prep; better after tryable UI + assets; organic only | **N** organic; **Y** if boosted/hunted-for-pay — **FLAG**, not plan |
| Paid X/LinkedIn/ads | Breaks OSS trust-first playbook; cost | **Y — FLAG** |
| Sponsored newsletters | Cost; needs COO→CEO | **Y — FLAG** |
| Paid PH hunters / upvote packages | Violates community norms + spend | **Y — FLAG** |

**Ops alignment:** Marketing Team standing work allows research + recommendations; **no paid promo without COO→CEO confirm** (`ops/ORG-OPS.md` § Marketing Team).

---

## 4. Done-list for Marketing QA

### Execution done
- [x] Read `product/CEO-ORIGINAL-BRIEF.md` fully
- [x] Read `product/PRODUCT-BRIEF.md` fully
- [x] Read `product/README.md`, root `PRODUCT-BRIEF.md` stub, `INDEX.md`, `README.md`, `ops/ORG-OPS.md` (Marketing + claims alignment)
- [x] Fetched public GitHub https://github.com/ioaikh/dealoware (README, CONTRIBUTING, license, docs/product tree) via `gh` + WebFetch
- [x] Wrote 8 Product restatement bullets with source paths
- [x] Conducted cited marketing research (HN, PH, Reddit, Dev.to, X, Awesome/MCP, OSS marketing guides)
- [x] Ranked TOP 5 organic recommendations; flagged paid options
- [x] Packaged this file at `plans/marketing/BRIEF-1-SENIOR-MARKETING-PACKAGE.md`
- [x] Status set READY FOR QA; routing note: Marketing QA → Chief Marketing only (not COO/CEO)

### Claims hygiene checklist
- [x] **No invented traction** (stars/users/GMV) — noted repo is early/public with empty About fields only
- [x] **No savings %** claims
- [x] **No "at scale"** claims
- [x] **No settlement/escrow** presented as current capability
- [x] **Intermediary** framing explicit
- [x] **Apache-2.0** + **hosted = AIKnowHow** preserved
- [x] **People + AI agents**, **Artifact**, **strategy-driven**, **identity until Accept** emphasized from Product
- [x] **No paid promo as primary path**; paid items flagged with **Paid? Y** and deferred
- [x] Citations include **URL + title** for research claims

### QA bounce criteria (suggested)
- Bounce if any metric/savings/at-scale claim lacks Product evidence
- Bounce if a paid channel is presented as the plan without FLAG + COO→CEO path
- Bounce if wording contradicts Claims lock or invents MCP/settlement as shipped
- Bounce if routing skips Marketing QA → Chief Marketing

---

## 5. Explicit claims-framing note

**Claims framing must match Product / Chief Product lock before any public ship.**

- Do **not** present this package to COO or CEO.
- Marketing QA verifies evidence and hygiene, then confirms to **Chief Marketing**.
- Chief Marketing coordinates **claims lock via Chief Product** before any external post, README marketing change beyond what's already Product-aligned, Show HN, Product Hunt, or directory submission copy.
- Cost-affecting ideas (if any ever arise) escalate COO → CEO per `ops/ORG-OPS.md` — none are recommended here as the plan.

---

## Appendix A — Source index (quick)

### Product / ops (internal KB)
- `/workspace/dealoware-kb/product/CEO-ORIGINAL-BRIEF.md`
- `/workspace/dealoware-kb/product/PRODUCT-BRIEF.md`
- `/workspace/dealoware-kb/product/README.md`
- `/workspace/dealoware-kb/PRODUCT-BRIEF.md` (stub → product/)
- `/workspace/dealoware-kb/INDEX.md`
- `/workspace/dealoware-kb/README.md`
- `/workspace/dealoware-kb/ops/ORG-OPS.md`

### GitHub
- https://github.com/ioaikh/dealoware — public Apache-2.0 README + `docs/product/`

### External research (titles + URLs used above)
- Show HN Guidelines — https://news.ycombinator.com/showhn.html
- Hacker News Guidelines — https://news.ycombinator.com/newsguidelines.html
- Favors.dev Show HN 2026 Guide — https://favors.dev/blog/show-hn-launch-guide
- DEV Community Show HN Guide — https://dev.to/iris1031/how-to-launch-on-hacker-news-the-show-hn-guide-real-data-461e
- Product Hunt Launch Guide — https://www.producthunt.com/launch
- awesome-product-hunt launch guide — https://github.com/fmerian/awesome-product-hunt/blob/main/product-hunt-launch-guide.md
- Lenny's Newsletter PH launch — https://www.lennysnewsletter.com/p/how-to-successfully-launch-on-product
- Gingiris OSS Marketing 2026 — https://gingiris.tools/blog/2026/04/03/open-source-marketing-the-complete-guide/
- Gingiris Developer Marketing — https://gingiris.tools/blog/2026/03/25/developer-marketing-101-how-to-grow-your-open-source-project/
- Stackmatix OSS marketing — https://www.stackmatix.com/blog/open-source-marketing
- Infrasity OSS strategy — https://www.infrasity.com/blog/open-source-marketing-strategy
- Infrasity content distribution — https://www.infrasity.com/blog/content-distribution-for-open-source-devtools
- DEV Editor Guide — https://dev.to/p/editor_guide
- MCP Find submit — https://mcpfind.org/submit
- MCPFind CONTRIBUTING — https://github.com/MCPFind/mcp-find/blob/main/CONTRIBUTING.md
- awesome-mcp-servers — https://github.com/tianzhou/awesome-mcp-servers
- r/LocalLLaMA rule updates — https://reddit.com/r/LocalLLaMA/comments/1su3ao4/rlocalllama_rule_updates/
- X OSS launch post guide — https://postinstantly.com/guides/how-to-write-an-x-post-announcing-you-open-sourced-your-side-project
- welaunch.sh X playbook — https://www.welaunch.sh/blog/the-twitter-x-launch-playbook-for-2026-threads-timing-and-templates-that-actuall
- Flowjam paid creator rates (FLAG) — https://www.flowjam.com/blog/launch-day-twitter-strategy-2026

---

*End of Brief #1 — READY FOR QA*
