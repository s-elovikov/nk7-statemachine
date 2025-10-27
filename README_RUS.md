# nk7-statemachine

Легковесная конечная state machine для Unity, построенная вокруг типобезопасных триггеров, payload-состояний и асинхронного жизненного цикла на UniTask.

## Возможности
- Типизированные триггеры `enum` и payload-объекты, гарантируют compile-time безопасность переходов
- Асинхронные `OnBeforeEnter/OnEnter/OnBeforeExit/OnExit`, совместимые с `Cysharp.Threading.Tasks`
- Контроль допустимых переходов: выброс исключения при попытке неразрешённого перехода
- Каждый переход создаёт новый DI-scope; зависимости предыдущего состояния освобождаются автоматически
- Расширение `RegisterStateMachine` для автоматической регистрации в `nk7-container` (define `NK7_CONTAINER`)
- AOT-дружественный конструктор (`UnityEngine.Scripting.Preserve`) и отсутствие аллокаций на цепочках регистрации

## Содержание
- [Установка](#установка)
  - [Unity Package Manager](#unity-package-manager)
  - [Ручная установка](#ручная-установка)
- [Быстрый старт](#быстрый-старт)
  - [1. Определите триггеры и payload](#1-определите-триггеры-и-payload)
  - [2. Реализуйте состояния](#2-реализуйте-состояния)
  - [3. Зарегистрируйте состояния и переходы](#3-зарегистрируйте-состояния-и-переходы)
  - [4. Выполните переход](#4-выполните-переход)
- [Жизненный цикл состояния](#жизненный-цикл-состояния)
- [Управление зависимостями и scope](#управление-зависимостями-и-scope)
- [Контроль переходов](#контроль-переходов)
- [Требования](#требования)

## Установка

### Unity Package Manager
1. Откройте Unity Package Manager (`Window → Package Manager`).
2. Нажмите `+ → Add package from git URL…`.
3. Укажите `https://github.com/lsd7nk/nk7-statemachine.git?path=/src/StateMachine`.

Unity не обновляет такие пакеты автоматически, поэтому при необходимости меняйте хэш вручную или используйте [UPM Git Extension](https://github.com/mob-sakai/UpmGitExtension).

### Ручная установка
Скопируйте папку `src/StateMachine` в свой проект и добавьте `Nk7.StateMachine.asmdef` в сборку.

## Быстрый старт

### 1. Определите триггеры и payload
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

Payload может быть структурой или классом. Структуры полезны для лёгких значимых данных и не вызывают боксинга, пока вы не приводите их к `IPayload`. Если payload содержит ссылочные поля или живёт дольше одного кадра, используйте класс, чтобы избежать лишних копирований.

```csharp
public sealed class MenuPayload : IPayload
{
    public MenuPayload(IMenuContext context) => Context = context;
    public IMenuContext Context { get; }
}
```

### 2. Реализуйте состояния
Наследуйтесь от `State<TTrigger, TPayload>` или реализуйте `IPayloadedState<TTrigger, TPayload>` вручную.

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

### 3. Зарегистрируйте состояния и переходы
Если вы используете `nk7-container`, подключите расширение регистрации:

```csharp
public sealed class GameRoot : RootContainer
{
    public override void Register(IBaseDIService di)
    {
        di.RegisterStateMachine<GameTrigger>(DIContainer); // регистрация stateMachine в контейнере

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

### 4. Выполните переход
```csharp
await stateMachine.PushAsync(GameTrigger.Boot, new BootPayload("Menu"), cancellationToken);
```

При явном использовании без DI-контейнера реализуйте `IFactoryService<IState<TTrigger>>` и `IScopeService`, затем передайте их в конструктор `StateMachine<TTrigger>`.

## Жизненный цикл состояния
- `OnBeforeEnterAsync` — вызывается до выхода из текущего состояния, удобно для предзагрузки.
- `OnEnterAsync` — основная логика входа в состояние.
- `OnBeforeExitAsync` — срабатывает перед `OnEnterAsync` следующего состояния.
- `OnExitAsync` — финализация состояния, вызывается после входа в новое состояние.
- `Dispose` (в `State<TTrigger, TPayload>`) — освобождение ресурсов при уничтожении состояния.

Методы вызываются последовательно, все возвращаемые `UniTask` дожидаются завершения перед переходом к следующему шагу.

## Управление зависимостями и scope
- На каждый `PushAsync` создаётся новый scope через `IScopeService`.
- Предыдущее состояние освобождает scope после `OnExitAsync`, что автоматически освобождает зависимости этого scope.
- Состояния можно регистрировать как `Scoped` — они будут пересоздаваться при каждом входе.

## Контроль переходов
- Для каждого триггера требуется регистрация `stateMachine.Register<State>(trigger)`.
- Допустимые переходы описываются через `AllowTransition(from, to)` или цепочки `.AllowTransition(nextTrigger)`.
- Попытка перехода без регистрации типа или без разрешённого перехода приводит к `InvalidOperationException` с описанием текущего и целевого триггера.

## Требования
- Unity 2021.2+
- `com.cysharp.unitask` 2.3.0
- Для интеграции с `nk7-container` необходим define `NK7_CONTAINER` (контейнер добавляет его автоматически)
