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
│   │   ├── Definitions/
│   │   ├── Effects/
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

### Решение Stage 8 — код и контент подготовлены, приёмка ожидается

Подробные контракты находятся в [Stage 8](Stages/08-bonus-composite.md), общая настройка/приёмка — в [чек-листе](Stages/08-bonus-checklist.md). Пользователь согласовал 2026-10-10 стартовые 3 жизни, максимум 5, AddLife +1 и исключение `AddScoreEffect`. Все задачи этапа подготовлены в коде/контенте и проверены статически; Unity пользователь проверит общей порцией. `LivesModel.TryAddLife()` владеет пределом и событием изменения жизней, не воскрешает модель с 0 жизней; `DoubleScoreDecorator.IsEnabled` остаётся единственным состоянием удвоения. Подбор не начисляет очки и не повышает combo.

`IBonusEffect.Apply(BonusContext)`, leaf effects и `CompositeBonusEffect` размещаются в Core как обычные C#-типы. Контекст передаёт `LivesModel`, уже собранный `DoubleScoreDecorator` и узкую операцию расширения платформы через `Action`; Unity-ссылок и контейнера в Core нет. Операция использует существующий `PaddleMovement.SetWidth` с `PaddleConfig.Width × 1.5`. Группа применяет дочерние эффекты по порядку, включая вложенную группу, без отдельного контракта или списка в pickup view. Общий `Reset` у каждого эффекта не нужен: одноразовое добавление жизни не откатывается.

`BonusFactory.Create` принимает выбранную BonusDefinition, проверяет effect/prefab до DI Instantiate и связывает clone с IBonusEffect до первого physics tick. Pickup отклоняет null/reinitialization; handler вызывает Apply без ветвления по типу. Актуальный контент с 2026-10-11: ExpandPaddleBonus → ExpandPaddleEffect/BonusPickup, AddLifeBonus → AddLifeEffect/AddLifePickup, DoubleScoreBonus → EnableDoubleScoreEffect/DoubleScorePickup. AddLifePickup и DoubleScorePickup — зелёный/оранжевый variants исходного prefab. Rescue заменён одиночным AddLife с сохранением GUID definition/prefab; Comeback и оба группирующих effect assets удалены. После правки импорт и назначения ещё требуют проверки пользователя.

`BonusEffectFactory` проверяет ссылки, типы, пустые группы и циклы по текущему пути обхода; общие assets в разных ветвях разрешены. Затем собирает runtime-снимок без применения effects. Различение типов definitions находится только на этой границе сборки. Definitions не хранят состояние партии, не получают DI и не изменяются при подборе. Проверка исходного prefab выполняется до Instantiate; injection не требует activeSelf во время временной деактивации VContainer. Временная регистрация общего ExpandPaddle в DI удалена: эффект определяется выбранной definition.

В P8.13 drop-профиль переведён на общий Chance и упорядоченные Entries `BonusDefinition + Weight`. CreateSettings проверяет все записи, эффекты и prefabs до random, даже при Chance=0. Core получает immutable числовой снимок; TrySelect сначала проверяет шанс, затем возвращает индекс. Веса конечны и строго положительны, сумма double; один вариант не требует selection random, несколько — одну выборку после успеха. Точная внутренняя граница выбирает следующую запись; roll=1 — последнюю. Профиль мигрирован без смены GUID. На 2026-10-11 текущая настройка пользователя сохранена: DoubleScoreBonus, Weight=1, Chance=1; три уникальных варианта можно включить после проверки. Старое поле одного prefab удалено; немигрированный профиль явно отклоняется, скрытого fallback нет.

`GameplayBonusHandler` сохраняет владение pickups и подписками, применяет собранный `IBonusEffect` и на `LifeLost`, `LevelComplete`, `GameOver` очищает pickups, возвращает базовую ширину и выключает double. Счёт и добавленные жизни не откатываются; combo по-прежнему сбрасывает `GameplayScoreHandler` только на `LifeLost`. При уничтожении scope не вызывается сброс уничтожаемой платформы; restart создаёт исходное состояние. Адресуемая загрузка и перенос lifetime при смене уровня остаются Stage 9.

Composite первоначально обслуживал вложенные группирующие бонусы. Пользователь отменил их игровой контент 2026-10-11. Core/Runtime-механизм и технические тесты сохранены как уже реализованный обязательный паттерн проекта; в сохранённых definitions сейчас только независимые эффекты. Уместное применение Composite для итоговой демонстрации остаётся открытым вопросом, новые механики в этой правке не добавлены.

Валидация Stage 7 учитывает порядок VContainer 1.19: `Instantiate` временно деактивирует prefab и clone, выполняет injection, затем возвращает исходную активность. Требование активного исходного prefab проверяется в `BonusDropDefinition.CreateSettings()` до random. Definition хранит `BonusPickup`, фабрика принимает и создаёт этот компонент через типизированный `Instantiate`. `BonusPickup` использует вручную назначенные сериализуемые `_rigidbody` и `_collider`, без `GetComponent` и Awake. `Construct()` проверяет наличие ссылок, принадлежность тому же объекту и физические параметры, не требуя activeSelf во время injection. Неактивный prefab остаётся ошибкой данных. После смены типа ссылки пользователь завершил настройку Pickup Prefab и Rigidbody/Collider, затем подтвердил тесты и работу геймплея; Gate 7 пройден 2026-10-10.

| Паттерн | Механика | Решаемая проблема |
|---|---|---|
| Decorator | Расчёт очков | Независимые `Combo` и `DoubleScore` оборачивают базовый расчёт, могут комбинироваться в явном порядке и тестироваться отдельно. |
| Chain of Responsibility | Обработка попадания в блок | `Indestructible → Shield → Damage` последовательно рассматривают один запрос и могут остановить его до изменения health. |
| Composite | Технический механизм групп эффектов | Реализация и тесты общего leaf/group-контракта сохранены. Группирующие игровые бонусы отменены; применение для итоговой демонстрации пока не определено. |

Для расчёта счёта [сравнение P5.4](Stages/05-score-decorator.md#сравнение-подходов-для-p54) показывает, что двум текущим множителям достаточно одной формулы. Decorator даёт общий `IScoreCalculator` каждому слою ценой дополнительных типов и вложенных вызовов. Темп роста серии принадлежит `ComboModel`; изменение этого темпа не требует нового score decorator. Дополнительные обёртки должны реализовывать согласованные правила начисления. Превосходство паттерна по скорости или объёму кода не установлено.

Для попаданий [сравнение P6.5](Stages/06-brick-hit-chain.md#p65--сравнение-подходов) показывает, что трём текущим правилам достаточно одного processor с ранними `return`. Chain of Responsibility выбран в согласованном scope для отдельных владельцев защитных правил, общего контракта сегментов цепочки и прямых тестов делегирования/short-circuit через подставной next. Цена — дополнительные классы и ссылки. Независимость Ball/UI обеспечивает граница обработки при любом подходе; преимущество цепочки по скорости или объёму кода не установлено.

Перед реализацией каждого паттерна:

1. Формулируется реальная изменчивость механики, порядок композиции и условие досрочной остановки.
2. Описывается простое решение без паттерна и конкретный недостаток этого решения при уже запланированных вариантах контента.
3. Фиксируется, почему выбранный паттерн делает механику понятнее или дешевле для расширения. Для счёта промежуточное сравнение хранится в этапе 5; итоговое обоснование в корневом README выполняется в P13.4 после реализации типов блоков и бонусов.
4. Если запланированный контент не оправдывает паттерн, сначала изменяется механика или набор вариантов. Формальные классы «для галочки» не добавляются.
5. Если даже после корректировки механики паттерн остаётся неуместным, до написания кода пересматривается выбор проекта.

Наличие класса с названием паттерна недостаточно. Acceptance criteria включают корректное поведение, использование общего контракта, изоляцию изменений и отдельные тесты композиции/порядка обработки.
