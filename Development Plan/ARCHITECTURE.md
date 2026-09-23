# Архитектура проекта

[К индексу плана](README.md)

## Целевая структура

```text
Assets/Content/
├── Art/
├── Audio/
├── Data/
│   ├── Ball/
│   ├── Bricks/
│   ├── Bonuses/
│   └── Levels/
├── Input/
├── Materials/
├── Prefabs/
│   ├── Gameplay/
│   └── UI/
├── Scenes/
│   ├── Bootstrap.unity
│   └── Gameplay.unity
├── Scripts/
│   ├── Core/
│   │   ├── Bonus/
│   │   ├── Bricks/
│   │   ├── GameFlow/
│   │   └── Score/
│   └── Runtime/
│       ├── Ball/
│       ├── Bonus/
│       ├── Bricks/
│       ├── Composition/
│       ├── GameFlow/
│       ├── Infrastructure/
│       ├── Input/
│       ├── Levels/
│       ├── Paddle/
│       └── UI/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

Рекомендуемые assembly definitions:

- `Arkanoid.Core` — обычные C#-правила, без MonoBehaviour и прямой зависимости от UI;
- `Arkanoid.Runtime` — Unity adapters, views, VContainer, Input System, UniTask, DOTween, Addressables;
- `Arkanoid.Tests.EditMode` — быстрые тесты `Arkanoid.Core`;
- `Arkanoid.Tests.PlayMode` — минимальные интеграционные тесты сцен и физики.

Не нужно дробить проект на большее число runtime assemblies без реальной причины.

PlayMode Test Runner создаёт project root scope до запуска тела теста. Поэтому smoke-test навигации сам открывает `Bootstrap` и вызывает зарегистрированный `SceneNavigator`; одноразовый автоматический переход `Bootstrap` → `Gameplay` дополнительно проверяется обычным запуском Play из `Bootstrap`.

## Направление зависимостей

```text
Unity input/physics/UI ──> Runtime adapters ──> Core rules
                                  ↑
                         VContainer composition
                                  ↑
                    Addressables / audio / storage
```

- `Core` не знает о сценах, Canvas, Addressables, DOTween и контейнере.
- MonoBehaviour отвечает за Unity lifecycle, ссылки Inspector и визуальное представление.
- Правила счёта, жизней, бонусов и обработки попадания живут в обычных C#-классах.
- Зависимости передаются явно. `IObjectResolver` используется только в composition/factory/infrastructure-коде и не передаётся в доменную логику.
- Не создаётся глобальный `ServiceLocator` и не используются произвольные вызовы `Resolve` из gameplay-классов.
- Интерфейс вводится на границе, где действительно нужна подмена, несколько реализаций или изоляция теста.
- `IPlayerInput` описывает намерение игрока (`Move`, `Launch`, `Pause`), а не клавиши или конкретное устройство. Input System отвечает за bindings; DI только передаёт выбранный adapter и сам по себе не заменяет эту границу.
- Для связи систем используются узкие события/контракты. Глобальный event bus не нужен.
- DOTween отвечает только за presentation. Завершение твина не является источником истины для игровых правил.
- Асинхронные операции получают `CancellationToken`, связанный со сроком жизни владельца.
- Владелец Addressables handle хранит его до уничтожения всех созданных из ресурса объектов и освобождает ровно один раз.

## VContainer scopes

- `AppLifetimeScope` создаётся из project root prefab через `VContainerSettings` и переживает смену сцен. Он регистрирует долгоживущую инфраструктуру: navigation, level catalog/loader, audio service и фабрику уровня. `SceneNavigator` создаётся из отдельного prefab; ссылки на сцены задаются через `SceneReference` в Inspector.
- `GameplayLifetimeScope` создаёт session-scoped сервисы: `GameSession`, `ScoreService`, `BonusService`, gameplay presenters/controllers.
- Уровень получает отдельный дочерний scope или явный `LevelContext`, который уничтожается при смене уровня.
- Обычные C# entry points регистрируются через VContainer lifecycle interfaces только когда им действительно нужен Unity PlayerLoop.
- Динамические prefab instances, которым требуется injection, создаются через фабрику на границе с `IObjectResolver`.

## Обязательные паттерны и критерии их уместности

| Паттерн | Механика | Решаемая проблема |
|---|---|---|
| Decorator | Расчёт очков | Независимые `Combo` и `DoubleScore` оборачивают базовый расчёт, могут комбинироваться в явном порядке и тестироваться отдельно. |
| Chain of Responsibility | Обработка попадания в блок | `Indestructible → Shield → Damage` последовательно рассматривают один запрос и могут остановить его до изменения health. |
| Composite | Составные бонусы | Одиночный эффект и группа эффектов имеют один контракт; `Rescue` и вложенный `Comeback` собираются из переиспользуемых leaf effects. |

Перед реализацией каждого паттерна:

1. Формулируется реальная изменчивость механики, порядок композиции и условие досрочной остановки.
2. Описывается простое решение без паттерна и конкретный недостаток этого решения при уже запланированных вариантах контента.
3. В README фиксируется, почему выбранный паттерн делает эту механику понятнее или дешевле для расширения.
4. Если запланированный контент не оправдывает паттерн, сначала изменяется механика или набор вариантов. Формальные классы «для галочки» не добавляются.
5. Если даже после корректировки механики паттерн остаётся неуместным, до написания кода пересматривается выбор проекта.

Наличие класса с названием паттерна недостаточно. Acceptance criteria включают корректное поведение, использование общего контракта, изоляцию изменений и отдельные тесты композиции/порядка обработки.
