# nk7-statemachine

Lightweight finite state machine for Unity built around type-safe triggers, payload states, and an asynchronous lifecycle powered by UniTask.

## Features
- Strongly typed `enum` triggers and payload objects guarantee compile-time transition safety
- Asynchronous `OnBeforeEnter/OnEnter/OnBeforeExit/OnExit` lifecycle powered by `Cysharp.Threading.Tasks`
- Fluent registration API with `StateMachineTrigger` chaining when calling `Register().AllowTransition(...)`
- Transition guard rails: throws when a disallowed transition is requested
- Optional `nk7-container` integration that wires scopes via `IScopeService` and exposes a `RegisterStateMachine` extension
- AOT-friendly constructor (`UnityEngine.Scripting.Preserve`) and zero allocations during registration chains

## Table of Contents
- [Installation](#installation)
  - [Unity Package Manager](#unity-package-manager)
  - [Manual Installation](#manual-installation)
- [Quick Start](#quick-start)
  - [1. Define triggers and payload](#1-define-triggers-and-payload)
  - [2. Implement states](#2-implement-states)
  - [3. Register states and transitions](#3-register-states-and-transitions)
  - [4. Perform a transition](#4-perform-a-transition)
- [State Lifecycle](#state-lifecycle)
- [Dependency and Scope Management](#dependency-and-scope-management)
- [Transition Control](#transition-control)
- [Requirements](#requirements)

## Installation

### Unity Package Manager
1. Open Unity Package Manager (`Window → Package Manager`).
2. Click `+ → Add package from git URL…`.
3. Enter `https://github.com/lsd7nk/nk7-statemachine.git?path=/src/StateMachine`.

Unity does not auto-update Git-based packages; update the hash manually when needed or use [UPM Git Extension](https://github.com/mob-sakai/UpmGitExtension).

### Manual Installation
Copy the `src/StateMachine` folder into your project and add `Nk7.StateMachine.asmdef` to the assembly.

## Quick Start

### 1. Define triggers and payload
```csharp
public enum GameTrigger
{
    Boot,
    Menu,
    Gameplay
}
```

```csharp
public readonly struct BootPayload : IPayload
{
    public BootPayload(string scene) => Scene = scene;
    public string Scene { get; }
}
```

Payload can be a struct or a class. Structs shine for lightweight value-type data and avoid boxing as long as you don’t cast them to `IPayload`. If the payload stores reference fields or lives beyond a single frame, prefer a class to avoid unnecessary copies.

```csharp
public sealed class MenuPayload : IPayload
{
    public MenuPayload(IMenuContext context) => Context = context;
    public IMenuContext Context { get; }
}
```

### 2. Implement states
Inherit from `State<TTrigger, TPayload>` or implement `IPayloadedState<TTrigger, TPayload>` manually.

```csharp
public sealed class BootState : State<GameTrigger, BootPayload>
{
    private readonly ISceneLoader _sceneLoader;

    public BootState(ISceneLoader sceneLoader) => _sceneLoader = sceneLoader;

    public override async UniTask OnEnterAsync(GameTrigger trigger, BootPayload payload, CancellationToken ct)
    {
        await _sceneLoader.LoadAsync(payload.Scene, ct);
    }
}
```

### 3. Register states and transitions
If you use `nk7-container`, enable the registration extension:

```csharp
public sealed class GameRoot : RootContainer
{
    public override void Register(IBaseDIService di)
    {
        di.RegisterStateMachine<GameTrigger>(DIContainer); // register the state machine in the container

        di.RegisterScoped<BootState>();
        di.RegisterScoped<MenuState>();
        di.RegisterScoped<GameplayState>();
    }

    public override void Resolve()
    {
        var stateMachine = DIContainer.Resolve<IStateMachine<GameTrigger>>();

        stateMachine.Register<BootState>(GameTrigger.Boot)
            .AllowTransition(GameTrigger.Menu);

        stateMachine.Register<MenuState>(GameTrigger.Menu)
            .AllowTransition(GameTrigger.Gameplay);

        stateMachine.Register<GameplayState>(GameTrigger.Gameplay)
            .AllowTransition(GameTrigger.Menu);
    }
}
```

### 4. Perform a transition
```csharp
await stateMachine.PushAsync(GameTrigger.Boot, new BootPayload("Menu"), cancellationToken);
```

Outside of `nk7-container` supply an `IStatesFactoryService<IState<GameTrigger>>` implementation that creates state instances:

```csharp
public sealed class ActivatorStatesFactory : IStatesFactoryService<IState<GameTrigger>>
{
    public IState<GameTrigger> GetService(Type serviceType) =>
        (IState<GameTrigger>)Activator.CreateInstance(serviceType);
}
```

```csharp
var stateMachine = new StateMachine<GameTrigger>(new ActivatorStatesFactory());

stateMachine.Register<BootState>(GameTrigger.Boot)
    .AllowTransition(GameTrigger.Menu);
```

## State Lifecycle
- `OnBeforeEnterAsync` — runs before exiting the current state, handy for preloading
- `OnEnterAsync` — main entry logic
- `OnBeforeExitAsync` — fires before `OnEnterAsync` of the next state
- `OnExitAsync` — finalizes the current state after the next state enters
- `Dispose` (in `State<TTrigger, TPayload>`) — releases resources when the state is torn down

Each method awaits the returned `UniTask` before moving to the next step in the sequence.

## Dependency and Scope Management
- **With `nk7-container`**: every `PushAsync` creates a new scope via `IScopeService`, the previous state releases its scope after `OnExitAsync`, and states registered as `Scoped` are re-created on every entry
- **Without the container**: the lifetime comes from your `IStatesFactoryService`; return fresh instances if you need per-transition cleanup or reuse cached singletons and dispose them manually

## Transition Control
- Every trigger requires an explicit `stateMachine.Register<State>(trigger)`
- `AllowTransition(from, to)` (or chained `.AllowTransition(nextTrigger)`) returns the lightweight `StateMachineTrigger` struct, letting you fluently describe the graph
- Attempting an unregistered or disallowed transition throws `InvalidOperationException` with current and target triggers in the message

## Requirements
- Unity 2021.2+
- `com.cysharp.unitask` 2.3.0 (declared in `package.json`)
