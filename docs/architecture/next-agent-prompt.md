# Next Instructions for the AI Agent

Use this after merging the docs-only architecture PR.

## Mission

Stabilize repository boundaries before adding more gameplay surface.

Do not widen scope. Do not import more content just because the pipeline allows it. Do not mix architecture refactors with feature expansion in the same PR.

## Hard constraints

1. Follow `AGENTS.md` strictly.
2. Use `docs/architecture/asmdef-tree.md` as the target assembly layout.
3. Use `docs/architecture/roadmap.md` as the milestone order.
4. Prefer small PRs, one bounded context each.
5. Runtime must not read ROMs or raw exports.
6. Generated assets are not edited by hand.
7. Pure gameplay logic belongs in `PokeBlack2.Core`.

## Execute in this order

### PR 1 - Canonical identity ADR

Create:

- `docs/architecture/adr/ADR-0001-canonical-product-identity.md`

Decide and document:

- canonical product name;
- canonical namespace/assembly prefix;
- repo-name vs product-name relationship;
- migration rule if a rename is required.

Do not rename code yet unless the ADR is explicitly approved.

### PR 2 - asmdef skeleton only

Add the asmdef files and empty folders from `docs/architecture/asmdef-tree.md`.

Do not move gameplay behavior yet.

Output required in the PR description:

- assembly list;
- allowed references;
- any compile blockers discovered.

### PR 3 - content manifest and versioning

Add:

- `ContentManifest`
- schema version object(s)
- content version checks
- import-time validation failure path

Goal:

- one place to answer: what content version is this build using?

### PR 4 - fixture import smoke

Add a tiny fixture dataset that is legal to store in-repo and can drive:

- import validation;
- generated asset creation;
- EditMode smoke tests.

Goal:

- CI must not require a ROM.

### PR 5 - CI shell

Add GitHub Actions workflows for:

- EditMode smoke;
- PlayMode smoke;
- manifest/version validation.

Keep job names unique.

### PR 6 onward - first vertical slice only

From this point forward, keep one bounded context per PR and stay inside the first vertical slice.

Split the vertical slice into this exact order:

1. bootstrap
2. one map
3. movement
4. dialogue
5. encounter
6. battle
7. save/load

### PR 6 - bootstrap shell

Add a minimal startup/composition root that can:

- initialize runtime services;
- load one map shell;
- expose a stable entry scene for future tests.

### PR 7 - first world slice

Add:

- one map shell;
- player movement;
- collision;
- one NPC interaction/dialogue.

### PR 8 - encounter to battle path

Add:

- one encounter trigger;
- minimal battle entry;
- minimal battle exit back to world.

### PR 9 - save/load shell

Add:

- save snapshot;
- restore to the correct location/state.

### PR 10 - end-to-end vertical slice test

Add one PlayMode test that covers:

- startup;
- movement;
- dialogue;
- encounter;
- battle;
- save/load.

## Required response format after every PR

```md
## What changed

## Why this PR is small enough

## Boundaries touched

## How tested

## Risks / follow-ups
```

## Stop conditions

Stop and ask for human review if any of the following appears:

- assembly cycles;
- naming ADR requires a broad rename;
- importer design requires runtime access to raw exports;
- save schema becomes coupled to scene objects;
- a PR would need to touch more than one bounded context.

## Explicitly forbidden before the slice passes

Do not do any of the following before the first vertical slice is playable end-to-end:

- widen content import coverage;
- add more maps;
- do large-scale UI beautification;
- mix multiple bounded contexts in one PR.

## Success condition

The next major milestone is not more content imported.

The next major milestone is:

> A tiny playable vertical slice with stable boundaries, repeatable import, and CI that runs without a ROM.
