# CakeOS Spaces

Status: **PARTIAL — shared/domain persistence foundation implemented; HUI and shell integration still required.**

This slice is Linux-first and intentionally platform-neutral. It takes product behaviour from CakeAI Spaces without transplanting the Avalonia host.

## Implemented in this checkpoint

- persistent Space records via atomic JSON file replacement;
- built-in Study, Shopping, Research and Agent Spaces;
- create/update/archive/restore/fork/delete-custom operations;
- built-in delete protection;
- current Space scope persistence;
- attached file references with read-only/read-write permission metadata;
- model preference, instructions, thinking mode, examples and generated-surface metadata in the shared model;
- launch policy mapping Study → Study, Agent → Tasks, all other configured Spaces → Chat;
- Space conversation contract/service for listing, starting and detaching conversations;
- delete-safety primitive that detaches conversations instead of destroying them;
- standalone smoke executable proving persistence and core lifecycle behaviour.

## Build / smoke

```bash
dotnet run --project apps/Spaces/Tests/CakeOS.Spaces.Tests.csproj
```

This does **not** yet prove a graphical Linux CakeOS app. A green smoke means only the shared domain/persistence checkpoint is green.

## Next Linux work

1. HUI Spaces scene using the accepted CakeUI/HUI component layer.
2. CakeOS persistence-path and file-picker adapters.
3. Real shell/chat conversation-store adapter.
4. Home/Chat/Study/Tasks/Research routing.
5. generated-surface and layout integration where current CakeOS services allow it.
6. Linux graphical host proof and screenshots.
7. Only then create the Windows QA port from this shared implementation.

## Product reference

Behaviour reference: `CroakyJake12/CakeAI@7c021082565b3e0ef9110bc4a1287ca3cc2c1fbb`.

No Avalonia UI code is copied into this app.
