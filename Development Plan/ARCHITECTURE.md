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
│   │   └── BonusDropDefinition.asset
│   └── Levels/
├── Input/
├── Materials/
├── Prefabs/
│   ├── Gameplay/
│   │   └── Bonuses/
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
- `GameplayLifetimeScope` владеет сервисами партии: `GameSession`, `LivesModel`, `ScoreService`, gameplay presenters/controllers. Для счёта `ScoreService`, `ComboModel` и цепочка калькуляторов зарегистрированы как `Lifetime.Singleton` внутри gameplay scope: все его дочерние scopes используют одни экземпляры. `Lifetime.Scoped` создавал бы отдельные экземпляры в каждом дочернем scope. Полный restart создаёт новый gameplay scope и исходное состояние счёта; сохранение gameplay scope при смене уровня уточняется в этапе 9. Остальные текущие регистрации остаются `Scoped`; их перенос при загрузке уровней также рассматривается в этапе 9. Stage 8 расширяет существующий `GameplayBonusHandler`; отдельный `BonusService` без самостоятельной ответственности не требуется.
- Уровень получает отдельный дочерний scope или явный `LevelContext`, который уничтожается при смене уровня.
- В текущем gameplay `LevelView.BrickDestroyed` сообщает об уникальном уничтожении перед `Finished`. `GameplayScoreHandler` повышает комбо и начисляет очки через `ScoreService` только в `Playing`, сбрасывает комбо при `LifeLost`. Base score берётся из проверенных `BrickState.Settings`, без константы и проверки `Basic`. `BrickView` не зависит от счёта и UI.
- `GameplayHudPresenter` читает текущие `ScoreService.Total` и `ComboModel.Count` при старте и обновляет HUD через `ScoreChanged` и `ComboChanged`; подписки снимаются в `Dispose`. `GameplayHudView` хранит подписи и отдельные ссылки TMP для score и combo. Сброс combo приходит из модели, без зависимости от порядка подписчиков игрового состояния.
- Обычные C# entry points регистрируются через VContainer lifecycle interfaces только когда им действительно нужен Unity PlayerLoop.
- Динамические prefab instances, которым требуется injection, создаются через фабрику на границе с `IObjectResolver`.

## Данные и состояние блоков

План Stage 7 использует `LevelView.BrickDestroyed` для дропа, не меняя Ball, hit-chain или путь score. Необязательный drop-профиль в `BrickDefinition` связывает шанс с одним pickup prefab; Unity-ссылки остаются в Runtime, проверка вероятности и решение `BonusDropService` — в Core. Factory создаёт объект с DI, gameplay-обработчик владеет активными pickups и подписками. Применение ExpandPaddle использует существующий `PaddleMovement.SetWidth` и базовую ширину из `PaddleConfig`.

В P7.1–P7.3 реализована граница данных и вероятности: `BonusDropDefinition.CreateSettings()` создаёт неизменяемый `BonusDropSettings`, проверяя шанс и наличие prefab. `BrickDefinition.CreateDropSettings()` допускает отсутствие профиля, а существующий `CreateSettings()` отклоняет неверный назначенный профиль уже при инициализации блока. Core-сервис `BonusDropService.ShouldDrop()` принимает только числовые settings и `IRandomProvider`; он не зависит от Unity, score, эффектов и игровых состояний. `UnityRandomProvider` адаптирует `Random.value`, fake в EditMode выдаёт заданную последовательность. Для отсутствия профиля и шанса 0/1 выборок нет; иначе одна выборка, проверка её диапазона и строгое `roll < chance`. Компиляция и тесты Unity подтверждены пользователем; Gate 7 закрыт 2026-10-10.

В P7.4–P7.6 `GameplayBonusHandler` подключён к уникальному уничтожению, отсекает запрещённое состояние/паузу/последний блок до random и владеет подписками и динамическими pickups. `BonusFactory` использует `IObjectResolver.Instantiate` с injection и родителем `LevelView`. `BonusPickup` требует активный Kinematic Rigidbody2D и root BoxCollider2D trigger, двигается через `MovePosition`, публикует однократный подбор назначенной платформой и удаляется в назначенной DeathZone. Проверка prefab включена в drop definition до random. `ExpandPaddleEffect` задаёт базовую ширину ×1,5 без накопления; `PaddleMovement.SetWidth` синхронно согласует sprite/collider и удерживает позицию в поле с учётом offset/scale. Отскок уже использует текущую ширину. Эти типы зарегистрированы как Scoped в gameplay scope; новых интерфейсов эффектов нет.

В P7.7 `GameplayBonusHandler` обрабатывает само событие LifeLost, до синхронного перехода в Ready/GameOver: удаляет непойманные pickups и вызывает `ExpandPaddleEffect.Reset()` к базовой ширине. LevelComplete и GameOver выполняют ту же очистку и сброс. Collider/simulation отключаются сразу, GameObject удаляется в конце кадра. Dispose снимает обе подписки и очищает pickups; сброс платформы при уничтожении scope не вызывается, поскольку её Unity-объект может уже уничтожаться. Restart пересоздаёт gameplay-сцену с базовой шириной. Ограничения Playing/паузы принадлежат существующим handler/pickup и `GameplayPauseController`. Последний разрушаемый блок не создаёт дроп. P7.8 завершён: пользователь подтвердил тесты, ручную регрессию и Android build на смартфоне 2026-10-10; Gate 7 пройден. В Stage 8 появляются дополнительные бонусы, общий контракт эффектов и выбор по весам; Stage 7 не вводит эти механизмы заранее.

В P6.1–P6.2 добавлены `Runtime/Bricks/BrickDefinition`, `Core/Bricks/BrickSettings` и `Core/Bricks/BrickState`. Definition хранит общие настройки и цвет; `CreateSettings()` копирует игровые значения в неизменяемый C#-объект с проверкой диапазонов. Этот переход сохраняет `noEngineReferences` у Core и не передаёт ему ScriptableObject или presentation-данные.

Блоки задаются независимыми параметрами: начальное HP `1–5`, shield charges и `IsIndestructible`, который представляет бесконечное здоровье. Эти параметры можно сочетать; normal/durable/shielded/indestructible — имена контентных пресетов, отдельные классы для них не нужны. Границы HP принадлежат `BrickSettings`; Inspector использует те же константы. Для indestructible начальное числовое HP также лежит в `1–5` и не расходуется. Щит трактуется как оболочка, поглощающая попадание целиком. Визуальные prefab-пресеты ссылаются на definitions. Огненный мяч исключён из текущего объёма, урон остаётся единичным.

Каждый `BrickState` хранит свои текущие HP и shield charges; одинаковые settings можно безопасно разделять между экземплярами. Изменения выполняются через `TryConsumeShieldCharge()` и `TryApplyDamage()`, разрушение вычисляется по нулевому HP. Состояние защищает собственные инварианты: не уходит ниже нуля, не изменяет definition, отклоняет damage при щите и любые изменения у indestructible или уничтоженного блока. `BrickHitProcessor` отклоняет повтор по уничтоженному состоянию, затем передаёт запрос цепочке. `LevelView.Construct` при инъекции сначала инициализирует все активные дочерние блоки из definitions, затем учитывает и подписывает только разрушаемые. Уровень без активных разрушаемых явно отклоняется. Состояния не зависят от порядка `Awake` и сохраняются при `LifeLost`; повторная инициализация блока отклоняется. Этап 6 завершён; пользователь подтвердил регрессию, включая сохранение повреждений при потере жизни и восстановление состояний при restart.

В P6.3 добавлены `BrickHitRequest` с целевым состоянием и `BrickHitResult` со снимком HP/щита и одним `BrickHitOutcome`. Исходы `Ignored`, `Indestructible`, `ShieldAbsorbed`, `ShieldBroken`, `Damaged`, `Destroyed` различают защиту, damage и новое уничтожение. Result не хранит Unity-ссылок, ссылку на изменяемое состояние или рассчитанный award. Для score учитывается только `Destroyed` в `Playing`, base берётся из settings, начисление остаётся в существующем пути через `LevelView` и `GameplayScoreHandler`.

В [P6.4](Stages/06-brick-hit-chain.md#p64--порядок-обработки) зафиксировано: игнорирование уже уничтоженного состояния до цепочки, затем `Indestructible → Shield → Damage`. Любое поглощение shield, включая последний заряд, прекращает текущий запрос до damage. Handlers и short-circuit-тесты реализованы в P6.7; общая цепочка создаётся один раз в `GameplayLifetimeScope` с `Lifetime.Singleton`. `BrickView.Hit()` публикует `Destroyed` только по соответствующему outcome, до удаления GameObject. Score/завершение остаются в существующих подписчиках уровня.

В P6.9 из `BrickView` удалён `BrickTypeId`: разновидность задаётся назначенной definition. Существующий Normal asset сохранён, добавлены Durable/Shielded/Indestructible с разными цветами. `BrickView` хранит обязательные Inspector-ссылки на renderer тела, отдельный renderer оболочки и TMP-подпись состояния. Цвет применяется при инициализации, подпись HP/щита и видимость оболочки обновляются после применённого попадания. Только presentation использует цвет, label и Unity renderers; Core не зависит от них. Варианты prefab настраивает пользователь. Не вводятся каталог типов, дополнительный presenter, анимации или второй путь score.

В P6.6 добавлен `IBrickHitHandler.Handle(BrickHitRequest)`, возвращающий `BrickHitResult`; пользователь подтвердил компиляцию. Нетерминальные handlers получают next через конструктор: либо возвращают конечный result своего правила, либо один раз возвращают `_next.Handle(request)` с тем же запросом. Терминальный Damage не получает next. Общий базовый класс и изменяемый `SetNext` не вводятся. `BrickHitChainTests` проверяет передачу запроса и остановку цепочки, `BrickHitIntegrationTests` — жизненный цикл view/уровня, смешанный учёт, валидацию до score/combo и повтор до удаления объекта. Для проверки настоящего внутреннего `GameplayScoreHandler` Runtime предоставляет доступ сборке `Arkanoid.Tests.PlayMode` через `InternalsVisibleTo`.

## Обязательные паттерны и критерии их уместности

### Решение Stage 8 — компоненты префабов, приёмка ожидается

Подробные контракты: [Stage 8](Stages/08-bonus-composite.md), [чек-лист](Stages/08-bonus-checklist.md), [добавление бонуса](ADDING_BONUS.md). Пользователь согласовал KISS-рефакторинг 2026-10-11: общий контекст, промежуточные definitions/assets, фабрика эффектов и Composite удалены. Неизменные правила: стартовые 3 жизни, максимум 5, AddLife +1 без воскрешения из 0; pickup не начисляет score/combo, DoubleScore использует существующий decorator.

`BonusEffect : MonoBehaviour` имеет один Apply(). Каждый effect — компонент на корне конкретного pickup prefab. ExpandPaddle получает через Inject PaddleMovement/PaddleConfig, AddLife — LivesModel, EnableDoubleScore — DoubleScoreDecorator. У каждого эффекта только собственные зависимости. Чистые правила жизней и расчёта счёта остаются в Core; Runtime-компоненты вызывают существующие модели.

BonusPickup сохраняет падение/контакты/однократность. Он проверяет ровно один enabled BonusEffect на своём корне и кеширует его при injection; сериализуемой ссылки на effect нет. Effect на дочернем объекте, отсутствующий/disabled effect или несколько эффектов явно отклоняются. Rigidbody/Collider остаются назначенными сериализуемыми ссылками с того же корня.

BonusFactory принимает BonusPickup prefab, проверяет исходную активность, physics и effect, затем выполняет типизированный IObjectResolver.Instantiate. Контейнер внедряет зависимости во все компоненты clone. Нет type switch, регистрации каждого effect и дополнительной инициализации из фабрики; новые типы бонусов не меняют фабрику или handler. Как и при исправлении Stage 7, Construct не требует activeSelf во время временной деактивации VContainer.

В Prefabs/Gameplay/Bonuses хранится общий BonusPickup.prefab без поведения и три конкретных variants: ExpandPaddlePickup → ExpandPaddleEffect, AddLifePickup → AddLifeEffect, DoubleScorePickup → EnableDoubleScoreEffect. Шаблон не назначается в профиль. Физика/визуал наследуются; на каждом variant свой единственный root effect. GUID существующих pickup assets и перенесённых скриптов сохранены; ExpandPaddlePickup — новый variant.

BonusDropDefinition хранит Chance и упорядоченные Entries `PickupPrefab + Weight`. Все prefabs и веса проверяются до random, включая Chance=0 и невыбранные записи. Core получает числовой immutable snapshot. Chance проверяется до выбора, веса конечны и >0, сумма double; один вариант не требует selection random. Точная внутренняя граница выбирает следующую запись, roll=1 — последнюю. Существующий профиль мигрирован без смены GUID: DoubleScorePickup, Chance=1, Weight=1; настройки пользователя сохранены.

GameplayBonusHandler вызывает pickup.Effect.Apply(), владеет pickups/подписками и на LifeLost/LevelComplete/GameOver очищает pickups, возвращает базовую ширину и выключает double. Total и добавленные жизни не откатываются; score handler сбрасывает combo на LifeLost. Начисление за последний блок происходит до терминального сброса. Disposal не меняет уничтожаемую платформу, новый scope начинает с исходного состояния.

Компонент эффекта живёт вместе с pickup и уничтожается после подбора. Длительное действие, если оно потребуется, должно выполняться соответствующим владельцем игровой механики с явным сроком жизни; новый сервис или конфиг не создаётся только ради обёртки одного вызова.

Composite удалён из бонусов по согласованному упрощению. Его исходное требование для демонстрационной версии остаётся отдельным вопросом, без формальной реализации ради отметки. Импорт/тесты/ручная приёмка текущего рефакторинга пользователем ещё не подтверждены.

| Паттерн | Механика | Решаемая проблема |
|---|---|---|
| Decorator | Расчёт очков | Независимые `Combo` и `DoubleScore` оборачивают базовый расчёт, могут комбинироваться в явном порядке и тестироваться отдельно. |
| Chain of Responsibility | Обработка попадания в блок | `Indestructible → Shield → Damage` последовательно рассматривают один запрос и могут остановить его до изменения health. |
| Composite | Применение не определено | Удалён из бонусов по согласованному KISS-рефакторингу. Для исходного требования портфолио нужна отдельная обоснованная задача. |

Для расчёта счёта [сравнение P5.4](Stages/05-score-decorator.md#сравнение-подходов-для-p54) показывает, что двум текущим множителям достаточно одной формулы. Decorator даёт общий `IScoreCalculator` каждому слою ценой дополнительных типов и вложенных вызовов. Темп роста серии принадлежит `ComboModel`; изменение этого темпа не требует нового score decorator. Дополнительные обёртки должны реализовывать согласованные правила начисления. Превосходство паттерна по скорости или объёму кода не установлено.

Для попаданий [сравнение P6.5](Stages/06-brick-hit-chain.md#p65--сравнение-подходов) показывает, что трём текущим правилам достаточно одного processor с ранними `return`. Chain of Responsibility выбран в согласованном scope для отдельных владельцев защитных правил, общего контракта сегментов цепочки и прямых тестов делегирования/short-circuit через подставной next. Цена — дополнительные классы и ссылки. Независимость Ball/UI обеспечивает граница обработки при любом подходе; преимущество цепочки по скорости или объёму кода не установлено.

Перед реализацией каждого паттерна:

1. Формулируется реальная изменчивость механики, порядок композиции и условие досрочной остановки.
2. Описывается простое решение без паттерна и конкретный недостаток этого решения при уже запланированных вариантах контента.
3. Фиксируется, почему выбранный паттерн делает механику понятнее или дешевле для расширения. Для счёта промежуточное сравнение хранится в этапе 5; итоговое обоснование в корневом README выполняется в P13.4 после реализации типов блоков и бонусов.
4. Если запланированный контент не оправдывает паттерн, сначала изменяется механика или набор вариантов. Формальные классы «для галочки» не добавляются.
5. Если даже после корректировки механики паттерн остаётся неуместным, до написания кода пересматривается выбор проекта.

Наличие класса с названием паттерна недостаточно. Acceptance criteria включают корректное поведение, использование общего контракта, изоляцию изменений и отдельные тесты композиции/порядка обработки.
