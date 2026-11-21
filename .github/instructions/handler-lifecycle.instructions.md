---
description: 'Handler lifecycle facts for reviewers: confirmed in .NET MAUI 11.0.10, CreatePlatformView runs once per handler in a real app, so do not ask for guards against a second call'
applyTo: '**/Handlers/**,**/Views/**,**/Platform/**,src/CommunityToolkit.Maui.DeviceTests*/**'
---

# Handler Lifecycle: CreatePlatformView Runs Once

**Confirmed in .NET MAUI 11.0.10: a handler's `CreatePlatformView` runs once per handler instance in a real app.** Re-check this note when the MAUI pin moves past 11.0.10.

## Why

All paths are in the `dotnet/maui` tag `11.0.10`.

- MAUI creates the platform view only when `PlatformView` is null (`ElementHandler.cs` `SetVirtualView`: `if (PlatformView is null) PlatformView = CreatePlatformElement();`, else "Reconnecting"). The only callers chain `ElementHandler` -> `ViewHandler` -> `ViewHandler<TVirtualView, TPlatformView>`.
- `PlatformView` becomes null only in `IElementHandler.DisconnectHandler`, which then calls the handler's own `DisconnectHandler`. The setter is `private protected`, so no other assembly can reset it.
- The toolkit handlers (`MediaElementHandler.*.cs`) create the manager with `MediaManager ??= new(...)`, then call `CreatePlatformView` once. Their `DisconnectHandler` calls `Dispose()`, which disposes the manager and sets `MediaManager = null`. A re-created platform view therefore always gets a new manager.
- `CameraViewHandler.shared.cs` creates a new `CameraManager` on every `CreatePlatformView` (`cameraManager = CreateCameraManager()`, not `??=`). Its `DisconnectHandler` calls `Disconnect()`, then `Dispose()`, which sets `cameraManager` to null. With a `PlatformViewFactory`, `ConnectHandler` adopts the factory's view instead. `CameraManager` is internal, so only a handler subclass can call `CreatePlatformView` twice.
- Handlers are transient: `AddHandler<TType, TTypeRender>` registers the handler with `AddTransient` (`MauiHandlersCollectionExtensions.cs`). A new handler is a new manager.

## Scenarios checked (all 11.0.10)

| Scenario | Result |
|---|---|
| Navigation, leave and return | A popped page (`Pop`, `PopToRoot`) disconnects its tree (`Page.cs` `SendNavigatedFrom` -> `ViewExtensions.DisconnectHandlers`; on iOS, not while Shell's stack still holds it; `HandlerDisconnectPolicy.Manual` skips a subtree in `ViewExtensions.cs`). `ElementHandler.DisconnectHandler` sets `view.Handler = null`, so showing it again builds a new handler (`ElementExtensions.cs`). A page covered by a push keeps its handler and platform view. |
| Re-parent in the same window | `ToHandler` returns the same handler; `handler.VirtualView == view`, so `SetVirtualView` is skipped. Platform view reused. |
| Multi-window, iPad scenes, Mac Catalyst windows, Windows windows | Each window has its own `MauiContext` (`MauiContextExtensions.cs`). Re-parenting to another window: `handler.MauiContext != context` gives a new handler, and `Element.SetHandler` disconnects the old one, so the old manager is disposed. A scene destroyed and restored takes the same path. |
| Android Activity recreation (rotation, theme change) | New Activity gives a new window scope, same path as above. |
| iOS and Mac Catalyst `CollectionView` recycling (`TemplatedCell2`) | Same template keeps the view and handler and only the `BindingContext` changes. `PrepareForReuse` keeps the tree. |
| Android `CollectionView` recycling (`TemplatedItemViewHolder`, `ItemContentView.Recycle`) | A template change creates a new `View` and handler. Recycle never resets a handler. |
| Hot reload | `HotReloadExtensions` sets `view.Handler = null`, which disconnects, then a new handler is built. |
| `HandlerDisconnectPolicy.Manual` | Handler and platform view are kept. No re-create. |
| `SetVirtualView(otherView)` on an existing handler | Platform view is non-null, so it only reconnects. MAUI never does this for view handlers. |

## The only ways to reach a second call

- App code calls the public `MediaManager` API (`new MediaManager(...)` and `CreatePlatformView()`) twice.
- A `MediaElementHandler` or `CameraViewHandler` subclass calls the protected `CreatePlatformView` again. `CameraManager` is internal, so this is the only route for the camera.

MAUI's lifecycle never does either.

## What it means for reviews

- Do not ask for guards, release logic or "what if it runs twice" handling around a second `CreatePlatformView` on the same manager. This applies to both `MediaManager` and `CameraManager`.
- Existing tests that call it twice exercise the public API directly. Keep them (maintainer decision, 2026-11-16). Existing guards, such as `ReleasePreviousPlayer` in `MediaManager.macios.cs`, stay too; do not flag them as dead code.
- Reopen the question only if MAUI nulls `PlatformView` without `DisconnectHandler`, re-creates a platform view on an existing handler (for example handler recycling across views), or the toolkit stops disposing the manager in `DisconnectHandler`.
