# Architecture Roadmap

Status: working draft

Last updated: 2026-04-19

## Goal

Take the project from foundation work to a maintainable playable remake by locking boundaries first, proving one vertical slice second, and scaling content only after that slice is stable.

## North-star architecture

```text
Local ROM / local source data
  -> raw exports
  -> normalized contracts (versioned)
  -> Editor-only validation/import seam
  -> imported Unity definition assets
  -> runtime indexes / manifests
  -> pure C# gameplay state + rules
  -> Unity presentation (world, battle, UI, audio, scenes)
```

## Operating principles

1. Runtime never consumes ROMs or raw exports directly.
2. Content import is deterministic and editor-only.
3. Gameplay truth lives in pure C# state and rules.
4. Scenes, prefabs, and MonoBehaviours are presentation and orchestration.
5. Generated content is reproducible, not hand-maintained.
6. The team ships one narrow vertical slice before widening scope.

## What success looks like

A new machine should be able to do two things:

1. run fixture-based tests and import smoke without any ROM present;
2. with local source data available, regenerate content via the supported pipeline and launch the same vertical slice.

## Milestone overview

| Phase | Focus | Exit criteria |
|---|---|---|
| 0 | Boundary freeze | identity ADR, asmdefs, manifest/versioning, fixture import, CI, contribution rules in place |
| 1 | First vertical slice | bootstrap -> map -> movement -> dialogue -> encounter -> battle -> save/load all work |
| 2 | World loop | trainer battles, healing, bag/party loop, multi-map traversal |
| 3 | Battle correctness | deterministic battle rules cover core systems and regression tests |
| 4 | Scale and polish | more content, better loading strategy, performance, release pipeline |

## Phase 0 - Boundary freeze

### Objective

Make the repository safe for parallel human plus AI work.

### Deliverables

- `AGENTS.md` merged and followed.
- `ADR-0001-canonical-product-identity.md` merged.
- asmdef skeleton landed with no behavior moves.
- `ContentManifest` introduced.
- schema/content version checks introduced.
- a minimal fixture content dataset committed.
- CI runs EditMode smoke, PlayMode smoke, and manifest/version validation.

### Strict PR order

1. `docs(repo): add adr for canonical product identity`
2. `chore(repo): add asmdef skeleton and dependency map`
3. `feat(content): add content manifest and schema version checks`
4. `test(content): add fixture import smoke dataset`
5. `ci(repo): add editmode/playmode/manifest validation workflows`

Every Phase 0 PR must stay inside one bounded context.

### Exit criteria

- a fresh checkout can run tests without a ROM;
- a developer can identify where core rules, import code, and presentation code belong without guessing;
- no runtime assembly depends on editor-only code.

## Phase 1 - First vertical slice

### Objective

Prove the entire stack once with the smallest fun path.

### Target slice

- bootstrap into game;
- load one map;
- player movement;
- collision and blocked tiles;
- one NPC interaction with dialogue;
- one encounter trigger;
- one minimal battle;
- exit battle back to world;
- save/load returns the player to the correct state.

### Required PR sequence

Split the first vertical slice into bounded PRs in this order:

1. `feat(bootstrap): add startup scene and composition root`
2. `feat(world): add one map shell`
3. `feat(world): add grid movement shell`
4. `feat(world): add npc interaction and dialogue shell`
5. `feat(world): add encounter trigger flow`
6. `feat(battle): add minimal battle entry/exit loop`
7. `feat(save): add save snapshot and reload path`
8. `test(vertical-slice): add end-to-end playmode smoke`

### Deliberate constraints

- one map only;
- one encounter table only;
- minimal battle content only;
- no broad content expansion yet;
- no large-scale UI beautification;
- no generalized editor tool suite beyond what the slice needs.

### Exit criteria

A testable flow exists where a user can:

1. start the game;
2. walk around;
3. talk to one NPC;
4. enter a battle;
5. leave the battle;
6. save and reload successfully.

## Things explicitly out of scope until Phase 1 is done

Do not prioritize these before the first slice is playable:

- importing many maps just because the pipeline can;
- large UI beautification passes;
- full battle feature parity;
- large asset-loading refactors;
- broad animation polish;
- optimization work with no measured bottleneck.

## Phase 2 - World loop

### Objective

Turn the slice into a repeatable core RPG loop.

### Scope

- multi-map traversal;
- encounter tables by area;
- trainer battle trigger;
- heal/blackout loop;
- bag/party/PC shell;
- basic event scripting;
- map-level state persistence.

## Phase 3 - Battle correctness

### Objective

Make battle rules trustworthy and regression-safe.

### Scope

- turn order and priority;
- accuracy and evasion;
- type effectiveness;
- status conditions;
- abilities;
- held items;
- experience gain;
- switching and faint resolution;
- battle event log and replay fixtures.

## Phase 4 - Scale and polish

### Objective

Scale up content while reducing operational risk.

### Scope

- broader content import coverage;
- better authoring and debug tools;
- loading strategy improvements;
- reduced reliance on `Resources` to a minimal bootstrap layer;
- stronger asset loading for large content sets;
- performance profiling;
- release automation.

## Top risks and mitigations

### Risk 1: naming drift

Symptoms:

- repo, assemblies, prompts, docs, and exported data use different product names.

Mitigation:

- land `ADR-0001` first;
- batch-rename once, not gradually.

### Risk 2: importer logic leaks into runtime

Symptoms:

- runtime parses normalized contracts directly;
- scenes depend on export folder layout.

Mitigation:

- keep all import in `*.Editor` assemblies;
- add tests that fail if runtime assemblies reference editor code.

### Risk 3: gameplay truth spreads into MonoBehaviours

Symptoms:

- save logic hidden in scene objects;
- battle rules embedded in UI scripts.

Mitigation:

- push mutable state into `Core`;
- add pure-logic tests first.

### Risk 4: AI creates broad PRs

Symptoms:

- one PR changes importer, UI, CI, and battle rules.

Mitigation:

- enforce one-bounded-context PR rule;
- reject oversized mixed PRs.

### Risk 5: content scale outruns architecture

Symptoms:

- more assets are imported before the first playable path is stable.

Mitigation:

- freeze expansion until the Phase 1 exit criteria are met.

## 30-day execution order for the AI agent

### Week 1

- write and merge `ADR-0001-canonical-product-identity.md`;
- add asmdef skeleton only;
- add docs for assembly references and folder ownership;
- introduce `ContentManifest` and schema/content version structure.

### Week 2

- add minimal in-repo fixture data;
- make importer and validator run against the fixture;
- add EditMode smoke for import and manifest validation;
- add CI workflow shells.

### Week 3

- build bootstrap scene;
- add one map shell;
- add movement and interaction shell;
- add PlayMode smoke for startup and movement.

### Week 4

- wire encounter -> battle entry -> battle exit;
- add save/load shell;
- add one end-to-end vertical-slice test;
- stop and reassess before expanding feature surface.
