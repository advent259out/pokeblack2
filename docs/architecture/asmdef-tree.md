# asmdef Directory Tree Draft

Status: draft

Last updated: 2026-04-19

## Naming note

`ADR-0001-canonical-product-identity.md` settles the canonical product identity as `PokeBlack`.

This draft therefore uses `PokeBlack.*` for future asmdefs even though the repository/workspace path may still be `pokeblack2`.

Any legacy implementation symbols that already use `PokeBlack2` remain transitional and must be renamed in one dedicated PR rather than piecemeal.

## Design goals

- keep domain logic engine-agnostic where possible;
- isolate editor import code from runtime;
- keep references one-way;
- minimize compile churn;
- make vertical-slice testing easy;
- avoid assembly explosion too early.

## Proposed tree

```text
Assets/
└── PokeBlack/
    ├── Bootstrap/
    │   ├── Runtime/
    │   │   └── PokeBlack.Bootstrap.asmdef
    │   └── Tests/
    │       └── PlayMode/
    │           └── PokeBlack.Bootstrap.Tests.PlayMode.asmdef
    │
    ├── Core/
    │   ├── Runtime/
    │   │   └── PokeBlack.Core.asmdef
    │   └── Tests/
    │       └── EditMode/
    │           └── PokeBlack.Core.Tests.EditMode.asmdef
    │
    ├── Content/
    │   ├── Contracts/
    │   │   └── PokeBlack.Content.Contracts.asmdef
    │   ├── Runtime/
    │   │   └── PokeBlack.Content.Runtime.asmdef
    │   ├── Import/
    │   │   └── Editor/
    │   │       └── PokeBlack.Content.Import.Editor.asmdef
    │   ├── Generated/
    │   └── AuthoredOverrides/
    │
    ├── Infrastructure/
    │   ├── Runtime/
    │   │   └── PokeBlack.Infrastructure.asmdef
    │   └── Editor/
    │       └── PokeBlack.Infrastructure.Editor.asmdef
    │
    ├── World/
    │   ├── Runtime/
    │   │   └── PokeBlack.World.Runtime.asmdef
    │   └── Tests/
    │       └── PlayMode/
    │           └── PokeBlack.World.Tests.PlayMode.asmdef
    │
    ├── Battle/
    │   ├── Runtime/
    │   │   └── PokeBlack.Battle.Runtime.asmdef
    │   └── Tests/
    │       └── PlayMode/
    │           └── PokeBlack.Battle.Tests.PlayMode.asmdef
    │
    ├── UI/
    │   ├── Runtime/
    │   │   └── PokeBlack.UI.Runtime.asmdef
    │   └── Editor/
    │       └── PokeBlack.UI.Editor.asmdef
    │
    └── Tools/
        └── Editor/
            └── PokeBlack.Tools.Editor.asmdef
```

## Assembly responsibilities

### `PokeBlack.Core`

Pure gameplay and domain logic.

Contains:

- deterministic battle rules;
- world-state mutation rules;
- save snapshot model;
- script/event runtime;
- domain events and typed IDs.

Must not contain:

- `UnityEngine`;
- `UnityEditor`;
- scene references;
- asset loading;
- animation or sprite code.

### `PokeBlack.Content.Contracts`

Versioned normalized DTO/contracts shared between tooling expectations and Unity import code.

Contains:

- normalized data shapes;
- schema version markers;
- validation result types;
- stable content IDs.

Must not contain:

- importer logic;
- Unity asset types;
- runtime services.

### `PokeBlack.Content.Runtime`

Imported definition assets and runtime lookup access.

Contains:

- `ScriptableObject` definitions;
- lookup/index builders used at runtime;
- `ContentManifest` access;
- schema/content version metadata consumed by runtime bootstrap;
- content registry/adapters for runtime reads.

Must not contain:

- raw export parsing;
- editor-only menus;
- gameplay truth/state.

### `PokeBlack.Content.Import.Editor`

Editor-only validation and import seam.

Contains:

- importers;
- validators;
- generated asset builders;
- reimport commands.

Must not contain:

- runtime behavior;
- battle/world presentation;
- scene logic.

### `PokeBlack.Infrastructure`

Cross-cutting runtime services.

Contains:

- save IO;
- platform abstraction;
- logging;
- diagnostics;
- time/random wrappers if needed.

Must not contain:

- domain rules;
- importer logic;
- large UI features.

### `PokeBlack.World.Runtime`

Unity-side overworld adapters and presentation.

Contains:

- map presentation;
- player input adapters;
- collision and trigger adapters;
- NPC and dialogue presentation;
- world-facing scene controllers.

Must not contain:

- authoritative save state;
- battle formula logic;
- importer logic.

### `PokeBlack.Battle.Runtime`

Unity-side battle presentation and orchestration adapters.

Contains:

- battle screen flow;
- animation hooks;
- HUD presentation;
- battle-to-core event mapping.

Must not contain:

- authoritative battle rules;
- importer logic;
- menu systems unrelated to battle.

### `PokeBlack.UI.Runtime`

Shared runtime menu/panel systems not specific to a single world or battle scene.

Contains:

- bag/party screens;
- pause/settings shell;
- reusable menu widgets.

Must not contain:

- domain truth;
- editor tooling;
- importer code.

### `PokeBlack.Bootstrap`

Composition root and high-level game flow startup.

Contains:

- startup scene entry;
- service wiring/installers;
- composition root;
- top-level mode transitions.

Must not contain:

- deep business rules;
- raw content import;
- duplicate world/battle logic.

### `PokeBlack.Tools.Editor`

General editor tooling that is not itself the import seam.

Contains:

- debug windows;
- custom inspectors;
- visualizers;
- lightweight authoring helpers.

Must not contain:

- runtime dependencies;
- production gameplay state.

## Allowed reference graph

```text
PokeBlack.Core
PokeBlack.Content.Contracts

PokeBlack.Content.Runtime
  -> PokeBlack.Content.Contracts

PokeBlack.Infrastructure
  -> PokeBlack.Core
  -> PokeBlack.Content.Runtime

PokeBlack.World.Runtime
  -> PokeBlack.Core
  -> PokeBlack.Content.Runtime
  -> PokeBlack.Infrastructure

PokeBlack.Battle.Runtime
  -> PokeBlack.Core
  -> PokeBlack.Content.Runtime
  -> PokeBlack.Infrastructure

PokeBlack.UI.Runtime
  -> PokeBlack.Core
  -> PokeBlack.Content.Runtime
  -> PokeBlack.Infrastructure

PokeBlack.Bootstrap
  -> PokeBlack.Core
  -> PokeBlack.Content.Runtime
  -> PokeBlack.Infrastructure
  -> PokeBlack.World.Runtime
  -> PokeBlack.Battle.Runtime
  -> PokeBlack.UI.Runtime

PokeBlack.Content.Import.Editor
  -> PokeBlack.Content.Contracts
  -> PokeBlack.Content.Runtime

PokeBlack.Infrastructure.Editor
  -> PokeBlack.Infrastructure

PokeBlack.UI.Editor
  -> PokeBlack.UI.Runtime

PokeBlack.Tools.Editor
  -> PokeBlack.Content.Runtime
```

## Forbidden reference patterns

Do not allow:

- runtime assemblies -> any `*.Editor` assembly;
- `PokeBlack.Core` -> `UnityEngine` or `UnityEditor`;
- `PokeBlack.Content.Runtime` -> `World`, `Battle`, `UI`, or `Bootstrap`;
- `World.Runtime` <-> `Battle.Runtime` direct cyclic references;
- `UI.Runtime` becoming a hidden dependency sink for all systems.

## Test assemblies

### EditMode

Use EditMode tests for:

- `PokeBlack.Core` deterministic logic;
- content manifest/version checks;
- importer validation;
- save serialization;
- assembly dependency smoke checks if needed.

### PlayMode

Use PlayMode tests for:

- bootstrap startup;
- movement;
- map interaction;
- dialogue interaction;
- battle entry/exit;
- save/load vertical slice.

## Migration order

Do not move everything at once.

### Step 1

Create the asmdef files and empty target folders first.

This step is intentionally narrow:

- add asmdef files;
- add empty folders if needed;
- wire references;
- do not move gameplay, importer, or runtime behavior yet.

### Step 2

Move code by bounded context in this order:

1. `Core`
2. `Content.Contracts`
3. `Content.Runtime`
4. `Infrastructure`
5. `World.Runtime`
6. `Battle.Runtime`
7. `UI.Runtime`
8. `Bootstrap`
9. editor/tooling assemblies

### Step 3

After each move:

- compile;
- fix references;
- run relevant tests;
- update this document if boundaries changed.
