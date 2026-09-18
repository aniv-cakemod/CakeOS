# CakeOS Spaces

Status: **IMPLEMENTED shared/HUI milestone; Linux graphical shell integration remains PARTIAL.**

Spaces is a Linux-first CakeOS application implemented in shared domain code plus CakeUI/HUI. CakeAI is the product-behaviour reference; no Avalonia product UI is transplanted.

## What is implemented

- persistent Space records with atomic JSON replacement;
- built-in Study, Shopping, Research and Agent Spaces;
- create, edit/update, archive, restore, fork and safe custom delete;
- built-in delete protection;
- persistent current Space scope;
- attached file references with read-only/read-write permission metadata;
- preferred model, instructions, thinking mode, examples, generated-surface data and typed layout documents;
- persistent Space-scoped conversation records for the standalone Spaces slice;
- reopen-most-recent and explicit conversation opening;
- new conversations are assigned to the active Space;
- deleting a custom Space detaches its conversations instead of destroying them;
- preferred-model availability fallback: an unavailable preferred model does not prevent launch;
- Study -> Study product routing contract;
- Agent -> Tasks routing contract;
- Research -> configured Chat/Research workspace routing contract;
- HUI Spaces scene with global navigation, Space picker, editor, archive visibility, file management, conversation list, generated-surface summary and layout action;
- compact HUI mode;
- product registration for `cakeos.spaces`.

## Persistence

The default Linux state location follows XDG:

```text
$XDG_STATE_HOME/cakeos/spaces/
```

When `XDG_STATE_HOME` is unset, Spaces uses:

```text
~/.local/state/cakeos/spaces/
```

The current standalone files are `spaces.json` and `conversations.json`. The shell adapter can replace the standalone conversation store with CakeOS's authoritative shared chat repository when that service is available.

## Build and tests

```bash
dotnet run --project apps/Spaces/Tests/CakeOS.Spaces.Tests.csproj
dotnet run --project apps/Spaces/Hui.Tests/CakeOS.Spaces.Hui.Tests.csproj
```

CI runs both on Ubuntu 24.04. A green run proves the shared domain, persistence, HUI construction and controller/shell contracts. It does **not** by itself prove a graphical CakeOS desktop session.

## Linux launch status

The HUI product is registered and the scene/controller are implemented. The current CakeOS checkout does not yet expose authoritative Home/Chat/Study/Tasks shell services for Spaces to bind to, so the final graphical shell adapter is **PARTIAL/BLOCKED by the missing shared shell services**, not replaced with fake navigation.

When those services are present, the host supplies:
- `ISpacesShellBridge`
- `ISpacesFilePicker`
- optionally `ISpacesModelCatalog`
- the authoritative `ISpaceConversationStore` when Chat owns conversations.

## Windows QA

No independent Windows implementation exists. Windows QA must be created from this shared/HUI implementation after the Linux shell adapter is real. Do not fork product behaviour into Avalonia-only controls.

## Product reference / provenance

Behaviour reference: `CroakyJake12/CakeAI@7c021082565b3e0ef9110bc4a1287ca3cc2c1fbb`.

HUI base for this workstream: CakeOS `repair/hui-donor-parity-components@b7aee9853fa6c6b65b201bea3890245beffed772`.

No external donor backend is required for Spaces; it is Cake-owned.

## Known gaps

- final CakeOS graphical shell adapter to real Chat/Study/Tasks services;
- platform file-picker adapter in the graphical host;
- live generated-surface rendering (data/editing is present, preview runtime is not);
- graphical layout editor host (typed layout persistence and action contract are present);
- Edit-with-Cake planning flow;
- full multi-example HUI editor (the domain supports multiple examples; the current editor exposes one pair);
- graphical Linux screenshot/runtime acceptance;
- Windows QA executable.

See `docs/spaces/SPACES-CAPABILITY-MATRIX.md` for claim-by-claim status.
