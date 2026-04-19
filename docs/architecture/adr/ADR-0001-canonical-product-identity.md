# ADR-0001 Canonical Product Identity

Status: accepted

Date: 2026-04-19

## Context

The project goal is a near-1:1 Unity recreation of Pokemon Black, not Pokemon Black 2.

The local repository folder is currently named `pokeblack2`, and earlier foundation docs used `PokeBlack2.*` as a temporary namespace and assembly prefix while the team deferred the naming decision.

That temporary state is no longer acceptable because the next bounded-context PR will introduce the asmdef skeleton. If the product identity, future assembly names, and repository path are allowed to drift apart, every later PR inherits the ambiguity.

## Decision

1. The canonical product identity is `PokeBlack`.
2. The repository or workspace name `pokeblack2` is an operational path only. It is not the product name, not the target game identity, and not a naming directive for future assemblies.
3. New architecture docs, future asmdefs, and new namespace or assembly prefixes introduced after this ADR must use `PokeBlack`.
4. Existing implementation symbols, paths, or generated artifacts that already contain `PokeBlack2` are transitional. They must not be renamed piecemeal across unrelated PRs.
5. If code or asset identifiers need to move from `PokeBlack2` to `PokeBlack`, do that in one dedicated rename PR with narrow scope.
6. When ambiguity is possible, write `Pokemon Black` or `Pokemon Black 2` explicitly instead of relying on folder names or shorthand.

## Consequences

- The asmdef skeleton planned after this ADR should use `PokeBlack.*` names.
- The current repository folder may remain `pokeblack2` without changing product identity.
- This ADR does not rename current code, assets, menu paths, or generated files by itself.
- This ADR does not change runtime behavior, import behavior, or ROM/export boundaries.
- A dedicated rename PR may still be required later if legacy `PokeBlack2` symbols become an obstacle.

## Non-goals

- choosing a ROM region or ROM hash baseline;
- renaming the repository folder;
- renaming current runtime/editor code in this PR;
- changing package IDs, company names, or release branding outside the product identity decision.
