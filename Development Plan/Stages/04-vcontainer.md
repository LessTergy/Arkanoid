# Этап 4. Стабилизировать композицию через VContainer

[Назад: рефакторинг после этапа 3](03-refactoring.md) · [К индексу](../README.md) · [Далее: этап 5](05-score-decorator.md)

**Результат:** работающий vertical slice собран через понятные scopes и явные зависимости.

## Задачи

- [x] `P4.1` Выписать фактические lifetime всех созданных объектов: app, gameplay session, level, scene object.
- [ ] `P4.2` Зарегистрировать app services в `AppLifetimeScope` с обоснованными `Singleton`/`Scoped` lifetimes.
- [ ] `P4.3` Зарегистрировать session services и scene components в `GameplayLifetimeScope`.
- [ ] `P4.4` Перенести orchestration из случайных `Start/Awake` в один-два entry point/presenter-класса.
- [ ] `P4.5` Внедрять обычные C#-зависимости через constructor; scene MonoBehaviour — через регистрацию component и injection method только при необходимости.
- [ ] `P4.6` Создать узкие factories для динамических ball/brick/pickup instances. Скрыть `IObjectResolver` внутри factory implementations.
- [ ] `P4.7` Убедиться, что gameplay-классы не вызывают `Resolve`, `Find*`, `GameObject.Find` и не используют статические singleton instances.
- [ ] `P4.8` Добавить composition smoke-test или PlayMode-test, который строит scope и разрешает основные entry points.

## Фактические lifetimes для P4.1

`app` и `gameplay session` — границы контейнеров в [AppLifetimeScope](../../Assets/Content/Scripts/Runtime/Composition/AppLifetimeScope.cs) и [GameplayLifetimeScope](../../Assets/Content/Scripts/Runtime/Composition/GameplayLifetimeScope.cs). `level` сейчас не имеет собственного scope: единственный уровень живёт внутри gameplay-сцены. `scene object` — способ владения Unity-объектом, а не отдельный `Lifetime` VContainer. Регистрация существующего компонента даёт контейнеру ссылку и инъекцию, но не создаёт и не уничтожает его GameObject.

| Объекты | Кто создаёт и как зарегистрировано | Фактическая граница жизни |
| --- | --- | --- |
| `AppLifetimeScope` | Unity создаёт компонент Bootstrap-сцены | Переживает загрузку Gameplay; это проверяет `BootstrapSmokeTests`. Уничтожение app scope завершает его контейнер. |
| `SceneNavigator` | `AppLifetimeScope`: `RegisterComponentInNewPrefab(..., Lifetime.Singleton).DontDestroyOnLoad()` | Один экземпляр на app scope, переживает смену gameplay-сцены. Подписка на `SceneManager.sceneLoaded` снимается в `OnDisable`. |
| `BootstrapEntryPoint`, app `ScopeLifetimeProbe` | App scope создаёт entry point и `Scoped` probe через build callback | Живут с app scope; probe получает `Dispose` при его завершении. `BootstrapEntryPoint.Start` запускает переход в Gameplay. |
| `GameplayLifetimeScope` | Unity создаёт компонент Gameplay-сцены; scope является дочерним к `AppLifetimeScope` | Один контейнер на загрузку Gameplay. При выгрузке или restart сцены уничтожается; повторная загрузка создаёт новый. |
| `GameSession`, `LivesModel` | `GameplayLifetimeScope`: оба `Lifetime.Scoped` | Один экземпляр каждого на gameplay scope. `GameSession` в конструкторе создаёт пять объектов состояний (`Ready`, `Playing`, `LifeLost`, `LevelComplete`, `GameOver`); они живут вместе с сессией. |
| `GameplayPauseController`, `LaunchHandler`, `LifeLossHandler`, `LevelFinishedHandler`, `GameplayHudPresenter`, gameplay `ScopeLifetimeProbe` | Gameplay scope создаёт их как `Scoped` entry points или probe | Живут до уничтожения gameplay scope. `Dispose` у pause controller, life loss handler, level finished handler и HUD presenter снимает подписки; pause controller также восстанавливает `Time.timeScale`. `LaunchHandler` не хранит подписок. |
| `InputSystemPlayerInput`, `PaddleMovement`, `BallController`, `PlayfieldCamera`, `DeathZone`, `GameplayHudView` | Unity создаёт компоненты сцены; `GameplayLifetimeScope.RegisterComponent` регистрирует уже существующие ссылки | Живут со своими GameObject в Gameplay-сцене. `InputSystemPlayerInput` выключает actions в `OnDisable`, `GameplayHudView` снимает listeners кнопок в `OnDestroy`. |
| `BallAttachedState`, `BallFlyingState`, `BallLostState`, `BallStoppedState` | `BallController.Start` создаёт по одному объекту каждого состояния через `new` | Принадлежат конкретному мячу и живут вместе с его `BallController`; контейнер их отдельно не регистрирует. |
| `LevelView`, его набор блоков и каждый `BrickView` | Unity создаёт уровень и дочерние блоки; `LevelView.Awake` собирает `BrickView`, а gameplay scope регистрирует существующий `LevelView` | `LevelView` и его набор живут с уровнем в Gameplay-сцене. Отдельного level scope и загрузчика уровней сейчас нет. Каждый блок уничтожается при попадании либо вместе со сценой; `LevelView` снимает оставшиеся подписки в `OnDestroy`. |
| `BallConfig`, `PaddleConfig` | Готовые `ScriptableObject` assets передаются в gameplay scope через `RegisterInstance` | Контейнер их не создаёт и не уничтожает. Это ссылки на assets, а не новое состояние партии. Prefab `SceneNavigator` и ссылки `InputActionReference` также являются исходными assets, а не создаваемыми сервисами. |

Временные `PlayerMoveIntent`, `Vector2`, `Rect` и подобные значения, а также `GUIStyle` в отладочном `OnGUI`, создаются во время вызовов и не имеют отдельного scope. Статические калькуляторы экземпляров не создают. Динамического создания ball, brick и pickup через контейнер пока нет — это предмет `P4.6`.

Основание для границ app/gameplay: [BootstrapSmokeTests](../../Assets/Content/Tests/PlayMode/BootstrapSmokeTests.cs) проверяет родительство scope и сохранение app scope после выгрузки Gameplay; [LevelCompletionSmokeTests](../../Assets/Content/Tests/PlayMode/LevelCompletionSmokeTests.cs) и [LifeLossSmokeTests](../../Assets/Content/Tests/PlayMode/LifeLossSmokeTests.cs) получают модели из gameplay scope и находят объекты уровня и сцены. Инвентаризация сделана по коду и существующим тестам; Unity Editor в рамках `P4.1` повторно не запускался.

## Gate 4

- [ ] Поведение Gate 3 не изменилось.
- [ ] По composition root видно, где и как создаётся каждая крупная система.
- [ ] Уничтожение gameplay scope освобождает session objects и отписки.
