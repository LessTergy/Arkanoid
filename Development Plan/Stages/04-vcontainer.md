# Этап 4. Стабилизировать композицию через VContainer

[Назад: рефакторинг после этапа 3](03-refactoring.md) · [К индексу](../README.md) · [Далее: этап 5](05-score-decorator.md)

**Результат:** работающий vertical slice собран через понятные scopes и явные зависимости.

## Задачи

- [x] `P4.1` Выписать фактические lifetime всех созданных объектов: app, gameplay session, level, scene object.
- [x] `P4.2` Зарегистрировать app services в `AppLifetimeScope` с обоснованными `Singleton`/`Scoped` lifetimes.
- [x] `P4.3` Зарегистрировать session services и scene components в `GameplayLifetimeScope`.
- [x] `P4.4` Проверить `Awake`/`Start` в проектных `MonoBehaviour`: оставить в них локальную инициализацию, а взаимодействие игровых систем держать в entry points, обработчиках и presenter. Не объединять независимые сценарии в один класс. **Критерий:** в `Awake`/`Start` компонентов нет управления состоянием партии или координации чужих систем.
- [x] `P4.5` Проверить внедрение зависимостей: обычные C#-классы получают их через конструктор; сценовые `MonoBehaviour` регистрируются как компоненты и используют `[Inject]` только при наличии внешних зависимостей. **Критерий:** все параметры конструкторов и `[Inject]` методов покрыты регистрациями соответствующего scope; лишней инъекции у компонентов нет.
- [x] `P4.6` Зафиксировать границу будущего динамического создания: добавлять узкую factory вместе с механикой, которая создаёт объекты во время игры, и не передавать `IObjectResolver` в игровой код. Сейчас не создавать фабрики ball/brick/pickup без потребителей. **Критерий:** текущие способы создания описаны; factory для pickup остаётся в `P7.5`, для prefab уровня — в `P9.3`, для дополнительных мячей появится вместе с соответствующей механикой.
- [x] `P4.7` Убедиться, что gameplay-классы не вызывают `Resolve`, `Find*`, `GameObject.Find` и не используют статические singleton instances.
- [x] `P4.8` Добавить composition smoke-test или PlayMode-test, который строит scope и разрешает основные entry points.

## Фактические lifetimes для P4.1

`app` и `gameplay session` — границы контейнеров в [AppLifetimeScope](../../Assets/Content/Scripts/Runtime/Composition/AppLifetimeScope.cs) и [GameplayLifetimeScope](../../Assets/Content/Scripts/Runtime/Composition/GameplayLifetimeScope.cs). `level` сейчас не имеет собственного scope: единственный уровень живёт внутри gameplay-сцены. `scene object` — способ владения Unity-объектом, а не отдельный `Lifetime` VContainer. Регистрация существующего компонента даёт контейнеру ссылку и инъекцию, но не создаёт и не уничтожает его GameObject.

| Объекты | Кто создаёт и как зарегистрировано | Фактическая граница жизни |
| --- | --- | --- |
| `AppLifetimeScope` | Unity создаёт компонент Bootstrap-сцены | Переживает загрузку Gameplay; это проверяет `BootstrapSmokeTests`. Уничтожение app scope завершает его контейнер. |
| `SceneNavigator` | `AppLifetimeScope`: `RegisterComponentInNewPrefab(..., Lifetime.Singleton).DontDestroyOnLoad()` | Один экземпляр на app scope, переживает смену gameplay-сцены. Подписка на `SceneManager.sceneLoaded` снимается в `OnDisable`. |
| `BootstrapEntryPoint` | App scope создаёт через `RegisterEntryPoint`, который использует `Singleton` | Один экземпляр на app scope. `Start` запускает переход в Gameplay один раз. |
| App `ScopeLifetimeProbe` | App scope регистрирует как `Scoped` и создаёт через build callback | Один экземпляр в app scope; получает `Dispose` при завершении этого scope. |
| `GameplayLifetimeScope` | Unity создаёт компонент Gameplay-сцены; scope является дочерним к `AppLifetimeScope` | Один контейнер на загрузку Gameplay. При выгрузке или restart сцены уничтожается; повторная загрузка создаёт новый. |
| `GameSession`, `LivesModel` | `GameplayLifetimeScope`: оба `Lifetime.Scoped` | Один экземпляр каждого на gameplay scope. `GameSession` в конструкторе создаёт пять объектов состояний (`Ready`, `Playing`, `LifeLost`, `LevelComplete`, `GameOver`); они живут вместе с сессией. |
| `GameplayPauseController`, `LaunchHandler`, `LifeLossHandler`, `LevelFinishedHandler`, `GameplayHudPresenter`, gameplay `ScopeLifetimeProbe` | Gameplay scope создаёт их как `Scoped` entry points или probe | Живут до уничтожения gameplay scope. `Dispose` у pause controller, life loss handler, level finished handler и HUD presenter снимает подписки; pause controller также восстанавливает `Time.timeScale`. `LaunchHandler` не хранит подписок. |
| `InputSystemPlayerInput`, `PaddleMovement`, `BallController`, `PlayfieldCamera`, `DeathZone`, `GameplayHudView` | Unity создаёт компоненты сцены; `GameplayLifetimeScope.RegisterComponent` регистрирует уже существующие ссылки | Живут со своими GameObject в Gameplay-сцене. `InputSystemPlayerInput` выключает actions в `OnDisable`, `GameplayHudView` снимает listeners кнопок в `OnDestroy`. |
| `BallAttachedState`, `BallFlyingState`, `BallLostState`, `BallStoppedState` | `BallController.Start` создаёт по одному объекту каждого состояния через `new` | Принадлежат конкретному мячу и живут вместе с его `BallController`; контейнер их отдельно не регистрирует. |
| `LevelView`, его набор блоков и каждый `BrickView` | Unity создаёт уровень и дочерние блоки; `LevelView.Awake` собирает `BrickView`, а gameplay scope регистрирует существующий `LevelView` | `LevelView` и его набор живут с уровнем в Gameplay-сцене. Отдельного level scope и загрузчика уровней сейчас нет. Каждый блок уничтожается при попадании либо вместе со сценой; `LevelView` снимает оставшиеся подписки в `OnDestroy`. |
| `BallConfig`, `PaddleConfig` | Готовые `ScriptableObject` assets передаются в gameplay scope через `RegisterInstance` | Контейнер их не создаёт и не уничтожает. Это ссылки на assets, а не новое состояние партии. Prefab `SceneNavigator` и ссылки `InputActionReference` также являются исходными assets, а не создаваемыми сервисами. |

Временные `PlayerMoveIntent`, `Vector2`, `Rect` и подобные значения, а также `GUIStyle` в отладочном `OnGUI`, создаются во время вызовов и не имеют отдельного scope. Статические калькуляторы экземпляров не создают. Динамического создания ball, brick и pickup через контейнер пока нет; конкретные factory будут добавлены вместе с механиками, которым они понадобятся.

Основание для границ app/gameplay: [BootstrapSmokeTests](../../Assets/Content/Tests/PlayMode/BootstrapSmokeTests.cs) проверяет родительство scope и сохранение app scope после выгрузки Gameplay; [LevelCompletionSmokeTests](../../Assets/Content/Tests/PlayMode/LevelCompletionSmokeTests.cs) и [LifeLossSmokeTests](../../Assets/Content/Tests/PlayMode/LifeLossSmokeTests.cs) получают модели из gameplay scope и находят объекты уровня и сцены. Инвентаризация сделана по коду и существующим тестам; Unity Editor в рамках `P4.1` повторно не запускался.

## Регистрации app services для P4.2

Текущему app scope нужны два рабочих объекта. `SceneNavigator` зарегистрирован как `Singleton`: один экземпляр prefab обслуживает переход из Bootstrap и все последующие перезапуски Gameplay. `DontDestroyOnLoad()` сохраняет его GameObject между сценами; дочерний gameplay scope получает эту регистрацию от родителя. `BootstrapEntryPoint` зарегистрирован через `RegisterEntryPoint`, что в VContainer соответствует singleton-регистрации lifecycle-интерфейсов; его `Start` должен выполниться один раз при создании app scope.

`ScopeLifetimeProbe` — диагностический объект. Для него выбран `Scoped`: один экземпляр создаётся в app scope через build callback, а `Dispose` показывает момент завершения именно этой области. Сейчас app scope один, поэтому число экземпляров `Scoped` и `Singleton` здесь одинаково; различие фиксирует владение probe конкретной областью. Других app services в текущем коде нет.

## Регистрации gameplay session для P4.3

В `GameplayLifetimeScope` уже зарегистрированы все зависимости текущего игрового цикла:

| Регистрация | Назначение и срок жизни |
| --- | --- |
| `GameSession`, `LivesModel` — `Scoped` | Состояние одной партии. При restart сцены новый gameplay scope получает новые экземпляры. |
| `GameplayPauseController`, `LaunchHandler`, `LifeLossHandler`, `LevelFinishedHandler`, `GameplayHudPresenter` — `Scoped` entry points | Включают обработку ввода, событий и HUD на время партии. `GameplayPauseController` дополнительно открыт как `AsSelf()` для `GameplayHudPresenter`. |
| `InputSystemPlayerInput`, `PaddleMovement`, `BallController`, `PlayfieldCamera`, `DeathZone`, `LevelView`, `GameplayHudView` — `RegisterComponent` | Существующие компоненты Gameplay-сцены. Контейнер внедряет зависимости в `PaddleMovement`, `BallController` и `DeathZone`; Unity владеет их GameObject. `InputSystemPlayerInput` доступен и как конкретный тип для управления паузой, и как `IPlayerInput` для игрового ввода. |
| `PaddleConfig`, `BallConfig` — `RegisterInstance` | Ссылки на существующие `ScriptableObject` assets. Контейнер предоставляет их потребителям, но не создаёт и не уничтожает. |
| `ScopeLifetimeProbe` — `Scoped` | Диагностика создания и завершения gameplay scope. |

`SceneNavigator` приходит из родительского app scope. Отдельная регистрация каждого `BrickView` не нужна: блоки принадлежат `LevelView`, не запрашивают инъекцию и не используются как зависимости обработчиков. Фактическое назначение сериализованных ссылок и создание нового scope после restart требуют проверки в Unity.

## Аудит `Awake`/`Start` для P4.4

Проверены все проектные `Awake`/`Start` в `Assets/Content/Scripts/Runtime`:

| Компонент | Работа в `Awake`/`Start` | Владелец |
| --- | --- | --- |
| `BallController` | Получает свои `Rigidbody2D`/`CircleCollider2D`, задаёт физические параметры и создаёт состояния своего мяча. | Сам мяч. |
| `PaddleMovement` | Получает свои компоненты, запоминает исходную позицию, проверяет спрайт и применяет ширину из `PaddleConfig` к собственному sprite и collider. | Сама платформа. |
| `PlayfieldCamera` | Получает свой `Camera` и задаёт его размер. | Сама камера. |
| `LevelView` | Находит дочерние `BrickView`, считает их и подписывается на их уничтожение. | Уровень и принадлежащие ему блоки. |
| `GameplayHudView` | Подключает кнопки к собственным событиям UI. | Сам HUD. |

Переход Bootstrap → Gameplay запускает `BootstrapEntryPoint`. Запуск мяча и партии обрабатывает `LaunchHandler`; потерю жизни — `LifeLossHandler`; завершение уровня — `LevelFinishedHandler`; паузу — `GameplayPauseController`; подписки и начальное отображение HUD — `GameplayHudPresenter`. Их регистрация находится в соответствующих lifetime scopes. Межсистемной orchestration в `Awake`/`Start` компонентов не найдено, поэтому перенос кода не потребовался. Поведение в Unity не изменялось.

## Аудит внедрения зависимостей для P4.5

`BootstrapEntryPoint`, игровые обработчики, `GameplayHudPresenter`, `ScopeLifetimeProbe` и состояния мяча получают нужные зависимости через конструкторы. `GameSession` и `LivesModel` не требуют внешних зависимостей; состояния сессии создаются внутри `GameSession` без параметров.

У сценовых компонентов есть три метода `[Inject]`: `PaddleMovement.Construct(IPlayerInput, PaddleConfig, PlayfieldCamera)`, `BallController.Construct(PaddleMovement, BallConfig)` и `DeathZone.Construct(BallController)`. Все три компонента зарегистрированы через `RegisterComponent` в `GameplayLifetimeScope`, а каждый тип параметра доступен из его регистраций. Остальные компоненты используют собственные Unity-компоненты, дочерние объекты или ссылки Inspector и не требуют метода инъекции. Изменений кода по результату аудита не понадобилось.

## Аудит поиска зависимостей для P4.7

В проектных `Assets/Content/Scripts/Core` и `Runtime` нет вызовов глобальных `Find*`, `GameObject.Find`, `Camera.main` или обращений к статическим singleton instances. Единственные вызовы `Resolve` находятся в build callbacks `AppLifetimeScope` и `GameplayLifetimeScope`: они создают диагностический `ScopeLifetimeProbe` внутри composition root. Игровые обработчики и компоненты контейнер не запрашивают.

`LevelView.GetComponentsInChildren<BrickView>()` обходит только дочерние объекты конкретного уровня при его инициализации; это локальный сбор содержимого prefab, а не глобальный поиск зависимости. `BallBounceCalculator` и `PaddlePositionCalculator` содержат только статические вычисления и не хранят singleton экземпляров. Правок игрового кода для P4.7 не потребовалось.

## Проверка композиции для P4.8

`BootstrapSmokeTests` дополнен разрешением `SceneNavigator` из дочернего контейнера, session models и `IPlayerInput`, а также проверкой состава `IStartable` и `ITickable` в app/gameplay scopes. Тест использует существующую загрузку Bootstrap → Gameplay и не ждёт прохождения уровня. Пользователь запустил тест в Unity Test Runner и подтвердил успешный результат.

## Gate 4

- [x] Поведение Gate 3 не изменилось.
- [x] По composition root видно, где и как создаётся каждая крупная система.
- [x] Уничтожение gameplay scope освобождает session objects и отписки.
