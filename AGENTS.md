# AGENTS.md

This file defines the working rules for human contributors and AI agents in this repository.

## Purpose

Build a maintainable Unity remake project by locking architecture boundaries first, proving one playable vertical slice second, and scaling content only after that slice is stable.

This repository must not grow into a pile of ad-hoc MonoBehaviours, hidden scene state, and one-off import scripts. Every change should strengthen one of these layers:

1. Offline content pipeline: local source data -> raw exports -> normalized contracts.
2. Editor-only import seam: validated normalized content -> imported Unity assets.
3. Runtime: pure gameplay state plus Unity presentation, with no direct dependency on ROMs or raw exports.

## Canonical naming rule

The canonical product identity is `PokeBlack`.

The repository and workspace name `pokeblack2` is an operational path only. It is not the product name and not a naming directive for future assemblies.

Use these rules:

- use `PokeBlack.*` for new assembly and namespace prefixes introduced after `ADR-0001`;
- explicitly write `Pokemon Black` vs `Pokemon Black 2` in docs, prompts, and issue titles when it matters;
- do not perform piecemeal renames of existing `PokeBlack2` implementation symbols.

If legacy `PokeBlack2` code or assets need to move to `PokeBlack`, do the rename in one dedicated PR before gameplay code spreads further.

## Non-negotiable architecture rules

### 1) Runtime never reads ROMs or raw exports

Runtime code must never read from any local ROM directory or raw export directory.

Allowed runtime inputs:

- imported Unity assets;
- versioned content manifests and generated lookup assets;
- save files;
- user settings.

Not allowed at runtime:

- parsing `.nds` or ROM-adjacent files;
- reading raw extractor output directly;
- invoking Python export tools;
- branching on file layout inside export folders.

### 2) Import is Editor-only

All normalization import, validation import, and generated asset creation belongs in Editor-only code.

Allowed in Editor-only assemblies:

- validators;
- importers;
- reimport and regeneration commands;
- authoring tools;
- debug viewers for imported content.

Not allowed in runtime assemblies:

- `UnityEditor` references;
- importer logic;
- schema conversion logic;
- editor menu commands.

### 3) Generated content is not hand-edited

Treat imported and generated assets as build products.

- Change the normalized contract or the importer, not the generated output.
- If a manual patch is necessary, place it in an authored override location such as `Assets/PokeBlack/Content/AuthoredOverrides/`.
- Do not mix generated and hand-authored edits in the same folder.

### 4) Gameplay logic belongs in `Core`

Business rules must live in pure C# domain code, not in view scripts.

Examples that belong in `PokeBlack.Core`:

- battle resolution;
- move validation;
- encounter selection;
- save snapshot rules;
- story flag and state transitions;
- deterministic script and event execution.

Examples that do not belong in `Core`:

- animation timing;
- sprite swapping;
- scene transitions;
- camera motion;
- input polling from Unity objects.

### 5) MonoBehaviours are adapters, not owners of game truth

MonoBehaviours may:

- collect player input;
- forward commands to services or domain objects;
- subscribe to events;
- update visuals;
- hold scene references.

MonoBehaviours must not become the source of truth for:

- inventory;
- party state;
- battle rules;
- story progression;
- encounter tables;
- map scripting state.

### 6) Static definitions are immutable, runtime state is serializable

Use imported assets, usually `ScriptableObject` definitions, for shared read-only content definitions.

Use pure C# state objects for mutable runtime state such as:

- current map state;
- player party;
- NPC and trigger state;
- battle state;
- save snapshots.

All mutable runtime state must be serializable and versionable.

### 7) One PR = one bounded context

A single PR should stay inside one main area:

- `pipeline`
- `content`
- `core`
- `world`
- `battle`
- `ui`
- `save`
- `tooling`
- `ci`
- `docs`

Avoid mixing, for example:

- importer refactor plus battle formula changes;
- menu UI rewrite plus save schema changes;
- map rendering work plus Python normalization changes.

### 8) No new boundary without documentation

If you introduce any of the following, update docs in the same PR:

- new assembly;
- new top-level directory;
- new schema version;
- new save version;
- new generated-content root;
- new service boundary.

Minimum doc updates:

- `docs/architecture/roadmap.md`
- `docs/architecture/asmdef-tree.md`
- a relevant ADR if the change is architectural

## Assembly rules

Use the assembly plan from `docs/architecture/asmdef-tree.md`.

Hard rules:

- `PokeBlack.Core` must not reference `UnityEngine` or `UnityEditor`.
- `PokeBlack.Content.Contracts` must stay engine-agnostic.
- `*.Editor` assemblies must be editor-only.
- runtime assemblies must not reference `*.Editor` assemblies.
- keep references one-way; if a cycle appears, stop and refactor instead of forcing it.

## Testing rules

### Required split

- EditMode tests for:
  - pure domain logic;
  - importer validation;
  - manifest and version checks;
  - save serialization.
- PlayMode tests for:
  - bootstrap smoke;
  - movement and warp;
  - dialogue interaction;
  - battle entry and exit;
  - save and load vertical slice.

### Minimum expectation per PR

Every non-trivial PR must include at least one of:

- a new automated test;
- an updated automated test;
- a documented reason why a test is not yet possible, plus a follow-up item.

## CI and workflow rules

- `main` is protected and receives changes via PR only.
- use GitHub Actions for validation.
- required checks must have unique job names.
- prefer small topic branches.
- draft PRs are welcome for work in progress.

Suggested branch prefixes:

- `feat/...`
- `fix/...`
- `chore/...`
- `refactor/...`
- `docs/...`
- `test/...`
- `ci/...`

Suggested PR title format:

```text
<type>(<area>): <summary>
```

Examples:

- `docs(repo): add architecture guardrails`
- `chore(repo): add asmdef skeleton`
- `feat(content): add content manifest validation`

## Definition of done

A task is done only when all are true:

1. code compiles;
2. relevant tests pass locally or in CI;
3. boundaries are respected;
4. generated files are not manually patched;
5. docs are updated if structure changed;
6. the PR explains what changed, why it stayed small, how it was tested, and what remains.

## Forbidden shortcuts

Do not:

- commit ROMs or copyrighted raw exports;
- read export folders at runtime;
- put core rules in MonoBehaviours;
- hand-edit generated assets;
- create hidden side effects in editor scripts;
- introduce cross-assembly cycles;
- combine architecture refactor and bulk content import in one PR;
- rename namespaces and assemblies gradually across unrelated PRs.

## Required PR summary template

Use this structure in every PR description:

```md
## What changed

## Why this PR is small enough

## Boundaries touched

## How tested

## Risks / follow-ups
```

## Immediate task order for AI agents

Do these in order unless a human reprioritizes:

1. Land the asmdef skeleton from `docs/architecture/asmdef-tree.md` without moving behavior yet.
2. Add `ContentManifest` plus schema/content version checks.
3. Add minimal fixture content so import smoke tests do not require a ROM.
4. Add CI for EditMode smoke, PlayMode smoke, and manifest/version validation.
5. Build the first vertical slice in bounded PRs following this sub-order:
   - bootstrap
   - one map
   - movement
   - dialogue
   - encounter
   - battle
   - save/load
6. Before the first vertical slice is playable, do not expand content import scope, do not do large-scale UI beautification, and do not add more maps.

When uncertain, optimize for clear boundaries and testability, not for raw feature count.
