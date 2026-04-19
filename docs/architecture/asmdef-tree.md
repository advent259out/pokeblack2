# asmdef Directory Tree Draft

Status: draft

Last updated: 2026-04-19

## Naming note

This draft uses `PokeBlack2.*` as the temporary assembly prefix because it matches the current repository name.

After `ADR-0001-canonical-product-identity.md` is decided, do a single dedicated rename PR if the prefix changes.

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
└── PokeBlack2/
    ├── Bootstrap/
    │   ├── Runtime/
    │   │   └── PokeBlack2.Bootstrap.asmdef
    │   └── Tests/
    │       └── PlayMode/
    │           └── PokeBlack2.Bootstrap.Tests.PlayMode.asmdef
    │
    ├── Core/
    │   ├── Runtime/
    │   │   └── PokeBlack2.Core.asmdef
    │   └── Tests/
    │       └── EditMode/
    │           └── PokeBlack2.Core.Tests.EditMode.asmdef
    │
    ├── Content/
    │   ├── Contracts/
    │   │   └── PokeBlack2.Content.Contracts.asmdef
    │   ├── Runtime/
    │   │   └── PokeBlack2.Content.Runtime.asmdef
    │   ├── Import/
    │   │   └── Editor/
    │   │       └── PokeBlack2.Content.Import.Editor.asmdef
    │   ├── Generated/
    │   └── AuthoredOverrides/
    │
    ├── Infrastructure/
    │   ├── Runtime/
    │   │   └── PokeBlack2.Infrastructure.asmdef
    │   └── Editor/
    │       └── PokeBlack2.Infrastructure.Editor.asmdef
    │
    ├── World/
    │   ├── Runtime/
    │   │   └── PokeBlack2.World.Runtime.asmdef
    │   └── Tests/
    │       └── PlayMode/
    │           └── PokeBlack2.World.Tests.PlayMode.asmdef
    │
    ├── Battle/
    │   ├── Runtime/
    │   │   └── PokeBlack2.Battle.Runtime.asmdef
    │   └── Tests/
    │       └── PlayMode/
    │           └── PokeBlack2.Battle.Tests.PlayMode.asmdef
    │
    ├── UI/
    │   ├── Runtime/
    │   │   └── PokeBlack2.UI.Runtime.asmdef
    │   └── Editor/
    │       └── PokeBlack2.UI.Editor.asmdef
    │
    └── Tools/
        └── Editor/
            └── PokeBlack2.Tools.Editor.asmdef
```

## Assembly responsibilities

### `PokeBlack2.Core`

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

### `PokeBlack2.Content.Contracts`

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

### `PokeBlack2.Content.Runtime`

Imported definition assets and runtime lookup access.

Contains:

- `ScriptableObject` definitions;
- lookup/index builders used at runtime;
- manifest access;
- content registry/adapters for runtime reads.

Must not contain:

- raw export parsing;
- editor-only menus;
- gameplay truth/state.

### `PokeBlack2.Content.Import.Editor`

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

### `PokeBlack2.Infrastructure`

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

### `PokeBlack2.World.Runtime`

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

### `PokeBlack2.Battle.Runtime`

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

### `PokeBlack2.UI.Runtime`

Shared runtime menu/panel systems not specific to a single world or battle scene.

Contains:

- bag/party screens;
- pause/settings shell;
- reusable menu widgets.

Must not contain:

- domain truth;
- editor tooling;
- importer code.

### `PokeBlack2.Bootstrap`

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

### `PokeBlack2.Tools.Editor`

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
PokeBlack2.Core
PokeBlack2.Content.Contracts

PokeBlack2.Content.Runtime
  -> PokeBlack2.Content.Contracts

PokeBlack2.Infrastructure
  -> PokeBlack2.Core
  -> PokeBlack2.Content.Runtime

PokeBlack2.World.Runtime
  -> PokeBlack2.Core
  -> PokeBlack2.Content.Runtime
  -> PokeBlack2.Infrastructure

PokeBlack2.Battle.Runtime
  -> PokeBlack2.Core
  -> PokeBlack2.Content.Runtime
  -> PokeBlack2.Infrastructure

PokeBlack2.UI.Runtime
  -> PokeBlack2.Core
  -> PokeBlack2.Content.Runtime
  -> PokeBlack2.Infrastructure

PokeBlack2.Bootstrap
  -> PokeBlack2.Core
  -> PokeBlack2.Content.Runtime
  -> PokeBlack2.Infrastructure
  -> PokeBlack2.World.Runtime
  -> PokeBlack2.Battle.Runtime
  -> PokeBlack2.UI.Runtime

PokeBlack2.Content.Import.Editor
  -> PokeBlack2.Content.Contracts
  -> PokeBlack2.Content.Runtime

PokeBlack2.Infrastructure.Editor
  -> PokeBlack2.Infrastructure

PokeBlack2.UI.Editor
  -> PokeBlack2.UI.Runtime

PokeBlack2.Tools.Editor
  -> PokeBlack2.Content.Runtime
```

## Forbidden reference patterns

Do not allow:

- runtime assemblies -> any `*.Editor` assembly;
- `PokeBlack2.Core` -> `UnityEngine` or `UnityEditor`;
- `PokeBlack2.Content.Runtime` -> `World`, `Battle`, `UI`, or `Bootstrap`;
- `World.Runtime` <-> `Battle.Runtime` direct cyclic references;
- `UI.Runtime` becoming a hidden dependency sink for all systems.

## Test assemblies

### EditMode

Use EditMode tests for:

- `PokeBlack2.Core` deterministic logic;
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
