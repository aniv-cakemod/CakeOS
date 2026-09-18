# CakeOS Spaces architecture

## Authority

Linux/shared application code is authoritative. Product state and behaviour live under `apps/Spaces/App`. Product UI lives under `apps/Spaces/Hui`. Platform hosts supply adapters; they do not reimplement Spaces.

```text
SpaceDefinition / SpaceRegistry
        |
        +-- JsonSpaceStore (XDG state)
        |
SpaceConversationService
        |
        +-- ISpaceConversationStore
        |      +-- JsonSpaceConversationStore (standalone proof)
        |      +-- future CakeOS Chat repository adapter
        |
SpacesApplicationService
        |
        +-- ISpacesShellBridge
        +-- ISpacesFilePicker
        +-- ISpacesModelCatalog
        |
SpacesHuiController
        |
SpacesHuiScene (CakeUI/HUI)
        |
Linux host / future Windows QA host
```

## Persistence and safety

`SpaceRegistry` owns mutation rules and reconciles the four built-ins on load. Built-in identity/kind cannot be changed by an update and built-ins cannot be deleted. Custom deletion is orchestrated by `SpacesApplicationService.DeleteCustomSpaceAsync`, which detaches conversations first.

`JsonSpaceStore` and `JsonSpaceConversationStore` write a temporary file and atomically replace the destination. They do not present in-memory fixtures as persistent product state.

## Chat scope

A Space launch persists the selected Space ID. For configured chat/research Spaces, `SpacesApplicationService` opens the most recently used Space conversation if one exists; otherwise it creates a new conversation whose `SpaceId` is the selected Space. Explicit New Chat always creates a new assigned conversation.

Home and ordinary Chat clear Space scope.

## Launch routing

- Study kind -> Study product.
- Agent kind -> Tasks product.
- Research and General/Shopping kinds -> configured chat workspace.

The launch plan carries registered context, attached files, thinking mode, generated-surface data and typed layout data. A preferred model is used only when the model catalog reports it available; unavailable/erroring catalogs fall back to normal shell model selection.

## HUI ownership

`SpacesHuiScene` owns the product pixels and interactions: navigation, Space picker, editor, lifecycle actions, files, conversations, archive visibility, compact mode and generated-surface status. The platform host remains thin.

The HUI currently exposes one example pair. The domain supports a list; expanding the editor must preserve existing pairs rather than silently claiming full example editing.

## Platform boundaries

`ISpacesShellBridge` is deliberately neutral. Linux and Windows hosts must implement the same operations rather than creating separate product logic.

The current CakeOS branch does not expose authoritative Chat/Study/Tasks shell services to bind against. Therefore graphical launch is not claimed yet. The adapter must be added when those services are available; no fake shell is used as release evidence.

## Testing truth

`apps/Spaces/Tests` proves registry lifecycle, persistence, files, layouts and persistent scoped conversations.

`apps/Spaces/Hui.Tests` proves HUI construction, editing/controller behaviour, launch routing, model fallback and shell/layout contracts.

Neither test is a substitute for a real graphical Linux session. Linux graphical QA and Windows QA are tracked separately.
