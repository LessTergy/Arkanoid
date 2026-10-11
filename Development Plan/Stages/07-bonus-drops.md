# Этап 7. Базовое выпадение и подбор бонуса

[Назад: этап 6](06-brick-hit-chain.md) · [К индексу](../README.md) · [Далее: этап 8](08-bonus-composite.md)

**Результат:** уничтоженный блок может породить pickup, который падает и применяется при контакте с платформой.

**Статус:** этап завершён, `P7.1–P7.8` выполнены, Gate 7 пройден. 2026-10-10 пользователь подтвердил успешные тесты, все ручные проверки и работу Android build на смартфоне, затем поручил завершить регрессию и этап. Результаты зафиксированы в [чек-листе настройки, ручной проверки и player build](07-bonus-checklist.md). Проверки Unity и устройства выполнил пользователь; агент выполнил статические проверки кода и обновление документации.

## Контекст этапов 0–6

- На входе в этап Bonus-скриптов не было. `Arkanoid.Core` имеет `noEngineReferences`; Unity-данные, физика и создание объектов остаются в Runtime.
- `BrickDefinition` хранит настройки и цвет, `BrickState` — HP/щит конкретного блока. Drop-настройки не должны менять цепочку `Indestructible → Shield → Damage` или попадать в Core как Unity-ссылки.
- `BrickView` публикует уничтожение до удаления GameObject. `LevelView` удаляет блок из учёта и вызывает `BrickDestroyed`, затем `Finished`. Это готовая точка подключения дропа; повторные/защитные попадания и indestructible не создают уничтожение. Позицию блока нужно получить в обработчике события.
- `GameplayScoreHandler` уже начисляет очки и повышает combo. Подбор ExpandPaddle не меняет score/combo/lives и не создаёт второй путь начисления.
- `PaddleMovement.SetWidth` обновляет Sliced sprite и collider; базовая ширина — `PaddleConfig.Width`. `PaddlePositionCalculator` и отскок мяча используют текущую ширину. Расширение у края требует удержать платформу внутри поля сразу, а не ждать движения игрока.
- `LifeLossHandler` синхронно проходит `LifeLost → Ready` либо `GameOver`; сброс эффекта обрабатывает само событие `LifeLost`, до следующего состояния. `LevelFinishedHandler` останавливает мяч и переводит сессию в `LevelComplete`; restart пересоздаёт сцену и gameplay scope.
- `GameplayPauseController` останавливает игровое время, но `GameSession` остаётся в `Playing`/`Ready`. Одной проверки состояния недостаточно для запрета подбора на паузе. `DeathZone` сейчас сообщает только о мяче; потеря pickup не должна расходовать жизнь.
- Создание через DI принадлежит factory/composition boundary; subscriptions и динамические pickups должны иметь владельца в gameplay scope. Нельзя полагаться на порядок подписчиков `BrickDestroyed` или оставлять объекты в старой сцене при restart.

## Задачи

- [x] `P7.1` Добавить необязательный `BonusDropDefinition` в `BrickDefinition`: шанс и прямая ссылка на pickup prefab. Отделить проверенные числовые Core-данные от Unity-ссылок; отсутствие профиля разрешено, некорректный назначенный профиль отклоняется явно.
- [x] `P7.2` Определить `IRandomProvider` с конечным результатом в `[0, 1]` и runtime adapter; fake random позволяет задавать точные значения.
- [x] `P7.3` Реализовать Core `BonusDropService`: только факт выпадения, без Unity, создания объектов, эффектов или score; конкретный prefab выбирает Runtime из профиля. Покрыть границы вероятности и валидацию до использования random.
- [x] `P7.4` Реализовать `BonusPickup`: падение вниз, физический trigger с нужной платформой, однократное потребление и удаление в DeathZone. Подготовить требования к prefab и слоям для настройки пользователем.
- [x] `P7.5` Подключить `BonusFactory` и обработчик `LevelView.BrickDestroyed`: DI при создании, позиция уничтоженного блока, один pickup на успешную попытку. Проверять разрешённое состояние и `RemainingBricks > 0` до random; зарегистрировать владельца динамических объектов и подписок в gameplay scope.
- [x] `P7.6` Реализовать `ExpandPaddleEffect` через `PaddleMovement.SetWidth`: ×1,5 от `PaddleConfig.Width`, без накопления. Согласовать sprite/collider, немедленно удержать расширенную платформу в границах поля и проверить отскок мяча.
- [x] `P7.7` Реализовать lifecycle: сброс ширины и удаление всех pickups на событии `LifeLost`, очистка на завершении уровня/партии и restart; остановка падения и подбора на паузе. Проверить возврат в Ready без бонусов и отсутствие старых подписок.
- [x] `P7.8` Завершить EditMode/PlayMode-регрессию и дать пользователю шаги Inspector, ручного сценария и player build. Gate закрывается после подтверждения пользователя.

## Реализация P7.1–P7.3

- `Core/Bonus/BonusDropSettings` хранит неизменяемый `Chance` и отклоняет NaN, infinity и значения вне `[0, 1]` в конструкторе.
- `Runtime/Bonus/BonusDropDefinition` хранит шанс (по умолчанию `0.25`) и прямую `BonusPickup`-ссылку `PickupPrefab`. `CreateSettings()` проверяет число и обязательную ссылку, включая шанс 0. В P7.4–P7.5 добавлена проверка физической конфигурации pickup на корне prefab.
- `BrickDefinition.BonusDrop` — необязательная ссылка. `CreateDropSettings()` возвращает `null` при её отсутствии; `CreateSettings()` также валидирует назначенный профиль, поэтому ошибка данных проявляется при существующей инициализации блока до gameplay-событий. Настройки HP/щита/score и assets не изменяются.
- `IRandomProvider.NextFloat01()` задаёт конечный результат в включительном `[0, 1]`; `UnityRandomProvider` использует `UnityEngine.Random.value`. `FakeRandomProvider` выдаёт точную последовательность, считает вызовы и явно отклоняет её исчерпание.
- `BonusDropService.ShouldDrop(BonusDropSettings)` возвращает только `bool`: отсутствие данных и шанс 0 → `false`, шанс 1 → `true`, иначе одна выборка и `roll < chance`. Неверный результат провайдера отклоняется через `InvalidOperationException`; ошибки провайдера не скрываются и не вызывают повторную выборку. Валидность шанса гарантирует `BonusDropSettings` до вызова random.
- Добавлены `BonusDropSettingsTests`, `BonusDropDefinitionTests`, `BonusDropServiceTests`: изначально 42 EditMode-кейса; после проверки prefab в P7.4–P7.5 — 49. Они проверяют отсутствие профиля, снимок данных, обязательный prefab и его компоненты, неверные числа, точную границу вероятности, крайние промежуточные шансы и число вызовов random. Тесты написаны, но агент их не запускал.

### Подтверждение пользователя для P7.1–P7.3

1. Дождаться импорта скриптов в Unity и проверить отсутствие ошибок компиляции в Console.
2. В `Window → General → Test Runner → EditMode` запустить три bonus test fixtures (после рефакторинга ссылок — 54 кейса) и существующие `BrickDefinitionTests`, `BrickSettingsTests`, `BrickStateTests`, `BrickHitContractTests`, `BrickHitChainTests`. Ожидается отсутствие failures.
3. Для настройки данных использовать существующий asset в `Assets/Content/Data/Bonuses` либо создать через `Create → Arkanoid → Bonus Drop Definition`; `Chance = 0.25` означает 25%. Назначить готовый pickup prefab в `Pickup Prefab`, затем профиль в `BrickDefinition → Bonus Drop` согласно [полному чек-листу](07-bonus-checklist.md).
4. У существующих definitions без профиля запуск из `Bootstrap` должен сохранить прежнее поведение блоков и счёта. Назначенный профиль без prefab должен явно отклоняться при инициализации уровня, даже при `Chance = 0`. После проверки вернуть definition в корректное состояние.

Компиляция, Test Runner, ручные сценарии и Android player build подтверждены пользователем 2026-10-10; P7.8 завершён.

## Реализация P7.4–P7.6

- `BonusPickup` двигает Kinematic Rigidbody2D вниз через `MovePosition` в `FixedUpdate`. Падение и подбор разрешены только в Playing без паузы. Контакт с назначенным collider платформы отключает физику, планирует удаление и один раз публикует `Collected`; контакт с назначенной DeathZone только удаляет pickup. `OnTriggerStay2D` повторно проверяет возможность подбора после resume. Ball/Brick/Wall и чужая платформа не применяют эффект.
- `BonusDropDefinition.CreateSettings()` требует назначенный `BonusPickup` prefab и отклоняет неверную скорость, отключённый pickup/trigger/simulation, некорректный body type и заблокированное движение по Y. Ссылки pickup на Rigidbody2D и BoxCollider2D назначаются вручную и обязаны принадлежать тому же объекту. Это проверяется при инициализации блока и повторно до random в обработчике уничтожения.
- `GameplayBonusHandler` подписан на `LevelView.BrickDestroyed`. До random проверяет Playing, отсутствие паузы, оставшиеся блоки и профиль. `BonusFactory` создаёт один объект через `IObjectResolver.Instantiate` в мировой позиции уничтоженного блока, под `LevelView`, с injection. Handler отслеживает экземпляры, удаляет их из учёта после уничтожения, а в `Dispose` снимает подписки и удаляет оставшиеся объекты. Все регистрации принадлежат gameplay scope; новых Inspector-ссылок в scope не требуется.
- `ExpandPaddleEffect.Apply()` задаёт `PaddleConfig.Width × 1.5` через `SetWidth`. Повторный подбор не накапливает ширину. `SetWidth` сразу обновляет Sliced sprite и BoxCollider2D, удерживает позицию в поле с учётом collider offset и X scale. Общая формула границ переиспользуется при движении. Неконечная/неположительная ширина, ширина больше поля и X scale, не позволяющий конечную локальную ширину, отклоняются до изменений. Для текущих настроек базовая ширина 2 даёт расширенную 3.
- Отскок уже использует текущие `PaddleMovement.Width` и центр collider; второй путь отскока не добавлен. Добавлены 10 PlayMode-кейсов `BonusIntegrationTests`: создание/DI, позиция, уникальное уничтожение, последний блок и порядок подписчиков, Ready/пауза/отсутствие профиля до random, падение/подбор без score, DeathZone, resume, scope disposal, повторное расширение, края/scale/offset и относительная точка отскока.
- Проверка агентом: Roslyn-разбор синтаксиса затронутых C# без компиляции Unity, проверка зависимостей Core, `.meta` и `git diff --check`. Test Runner не запускался; сцены, prefab и настройки проекта не редактировались.

### Настройка и ручная проверка P7.4–P7.6

1. Создать активный GameObject `ExpandPaddlePickup`, добавить на его корень `BonusPickup`, `Rigidbody2D`, `BoxCollider2D` и `SpriteRenderer`. Назначить sprite, размер collider и видимый Sorting Layer/Order. `BonusPickup` должен быть enabled, `Fall Speed = 3` (либо другое конечное положительное значение).
2. Rigidbody2D: `Body Type = Kinematic`, `Simulated = true`, `Freeze Rotation Z`, позицию Y не замораживать. BoxCollider2D: enabled и `Is Trigger = true`. Trigger-контакт разрешён и с Kinematic платформой, и со Static DeathZone; Full Kinematic Contacts для trigger не обязателен. Сохранить объект как prefab в `Assets/Content/Prefabs/Gameplay`, затем удалить его исходный экземпляр из сцены: экземпляры создаёт фабрика.
3. Создать слой `BonusPickup` и назначить его prefab. В `Project Settings → Physics 2D → Layer Collision Matrix` включить контакт этого слоя с фактическими слоями Paddle и DeathZone, выключить с Ball/Brick/Wall и самим BonusPickup. Имена/существование слоёв агент не проверял; использовать слои назначенных объектов. DeathZone collider должен находиться на GameObject компонента `DeathZone`, Paddle collider — существующий `PaddleMovement.Collider`.
4. Назначить prefab в `BonusDropDefinition.Pickup Prefab`, профиль — в нужные `BrickDefinition.Bonus Drop`. Для проверки установить `Chance = 1`; после проверки вернуть `0.25`. У платформы оставить Sliced draw mode, scale/rotation родителей — как в текущем игровом поле. `PaddleConfig.Width × 1.5` должен помещаться в `PlayfieldCamera.WorldBounds.width`.
5. Дождаться компиляции; запустить все EditMode и PlayMode-тесты согласно [полному чек-листу](07-bonus-checklist.md), включая `BonusIntegrationTests` (после P7.8 — 15 кейсов). Ожидаются успешные результаты. Автоматические кейсы отскока проверяют расчёт относительно расширенной ширины; реальный физический отскок проверить вручную.
6. Запустить из Bootstrap и уничтожить отмеченный блок, когда есть ещё разрушаемые блоки: появляется один pickup на его месте и падает вниз. Проверить shield/damage/indestructible и повторное попадание — pickup не появляется до нового уничтожения. Последний блок завершает уровень без нового pickup.
7. Поймать pickup: ширина становится 3 при базовой 2; sprite и collider совпадают, повторный подбор сохраняет 3, дополнительного score/combo нет. Повторить у левого/правого края без движения после подбора и проверить отскок мяча по центру и краям расширенной платформы.
8. Пропустить pickup: он исчезает в DeathZone, жизни не меняются. Проверить паузу во время падения и рядом с платформой: движение/подбор остановлены, после resume подбор снова возможен. Restart должен уничтожить объекты старого gameplay scope.

## Реализация P7.7–P7.8

- `ExpandPaddleEffect.Reset()` возвращает `PaddleConfig.Width`. `GameplayBonusHandler` подписан на `GameSession.StateChanged`: на `LifeLost`, `LevelComplete` и `GameOver` очищает pickups и сбрасывает ширину. Очистка сразу отключает collider/simulation, а Unity удаляет GameObject в конце кадра. Синхронный переход из LifeLost не оставляет окно для повторного подбора.
- `Dispose()` снимает подписки с уровня и сессии, удаляет оставшиеся pickups через общий `ClearPickups()`. При уничтожении scope ширина отдельно не сбрасывается: платформа может уже уничтожаться; новая gameplay-сцена инициализирует её базовой шириной. Пауза использует существующий `GameplayPauseController`.
- `BonusIntegrationTests` расширены до 15 кейсов: дополнены проверки последнего блока и disposal; добавлены LifeLost до Ready, физическая DeathZone на последней жизни, победа с двумя pickups и снятием паузы, двукратное пересоздание контейнера, shield/damage/indestructible до уникального уничтожения. Пересоздание тестового контейнера не заменяет два настоящих restart сцены — они входят в ручную проверку.
- Подготовлены 54 EditMode и 15 PlayMode bonus-кейсов. Пользователь запускает также всю существующую регрессию. Статически проверены синтаксис Roslyn и `git diff --check`; это не подтверждает компиляцию или поведение в Unity.
- Пользователь сообщил об `InvalidOperationException` при injection в `LastLife_PhysicalDeathZoneResetsAtLifeLostAndEndsGame`. Установленный VContainer 1.19 деактивирует prefab/clone до `InjectGameObject`, затем возвращает исходную активность. Проверка активности перенесена в `BonusDropDefinition.CreateSettings()` для исходного prefab; `BonusPickup.Construct()` проверяет компоненты и физические параметры без требования активного GameObject. Неактивный prefab по-прежнему отклоняется до random. Добавлен отрицательный EditMode-кейс и проверки активации clone/восстановления template в существующем PlayMode-кейсе. Повторные тесты Unity подтверждены пользователем 2026-10-10.
- Актуальный общий порядок настройки и приёмки находится в [чек-листе Stage 7](07-bonus-checklist.md). P7.8 и Gate 7 закрыты после подтверждения пользователем регрессии и Android player build 2026-10-10.
- По правке пользователя `BonusDropDefinition.PickupPrefab` и `BonusFactory.Create` используют `BonusPickup` вместо GameObject. Фабрика возвращает результат типизированного `IObjectResolver.Instantiate` без поиска компонента. Из `BonusPickup` удалены GetComponent и Awake; сериализуемые Rigidbody/Collider назначаются вручную. Тестовые объекты обновлены, добавлены четыре EditMode-кейса отсутствующих ссылок и компонентов с чужого объекта. После рефакторинга пользователь назначил Pickup Prefab и Rigidbody/Collider в существующем prefab и подтвердил работу геймплея; агент этим изменением assets не редактировал.
- После сообщения о нестабильном ожидании LifeLost подготовка физического контакта в `LastLife_PhysicalDeathZoneResetsAtLifeLostAndEndsGame` приведена к порядку существующего `LifeLossSmokeTests`: шаг после запуска, центр collider с учётом offset, скорость вниз и `Physics2D.SyncTransforms`. Ожидание события ограничено пятью итерациями с FixedUpdate и следующим кадром; добавлена диагностика сбоя. Статические проверки прошли; повторные тесты Unity подтверждены пользователем 2026-10-10, число кейсов не изменилось.

API сверены с официальными источниками: [Unity 6.3 MovePosition](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody2D.MovePosition.html), [OnTriggerStay2D](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnTriggerStay2D.html), [Kinematic trigger exception](https://docs.unity3d.com/6000.3/Documentation/Manual/2d-physics/rigidbody/body-types/kinematic/kinematic-body-type-reference.html), [VContainer Instantiate/injection](https://github.com/hadashia/vcontainer/blob/master/website/docs/resolving/container-api.mdx).

## Принятые правила и границы

После подготовки P7.8 пользователь прямо поручил настройку через Unity MCP. Создан `Assets/Content/Prefabs/Gameplay/ExpandPaddlePickup.prefab`: Kinematic simulated Rigidbody2D, root trigger 0.9×0.35, Fall Speed=3, бирюзовый Sliced sprite, Sorting Order=10, существующий слой Pickup. Prefab назначен существующему BonusDropDefinition с сохранением GUID и Chance=0.25; профиль подключён к Default/Durable/Shielded. Indestructible остаётся без профиля. Существующие ссылки gameplay scope и матрица контактов Pickup с Paddle/DeathZone проверены; дополнительных ссылок или слоёв не потребовалось. Временный экземпляр удалён, Gameplay сохранена без содержательных изменений. Штатная валидация сохранённых definitions прошла, Console без ошибок/предупреждений. Play Mode, тесты, ручная регрессия и Android player build затем подтверждены пользователем 2026-10-10; выполненные пункты отмечены в [чек-листе](07-bonus-checklist.md).

На Stage 7 существует только ExpandPaddle. Для отмеченной definition профиль задаёт шанс; стартовое значение — 25%, для проверки можно выставить 0% или 100%. HP/щит/цвет не определяют вероятность, отсутствие профиля означает отсутствие дропа. Веса, каталог идентификаторов и effect definitions здесь не нужны: несколько одиночных/составных бонусов и таблица выбора предусмотрены в [Stage 8, P8.13](08-bonus-composite.md).

Шанс и результат random — конечные числа в `[0, 1]`. Шанс 0 всегда отклоняет дроп, шанс 1 всегда разрешает его; оба случая обходятся без random. Для промежуточного шанса нужна ровно одна выборка, дроп разрешён при `roll < chance`. Отсутствие профиля и запрещённое состояние не расходуют random. Некорректные данные отклоняются до создания объектов; definitions и игровые настройки не меняются от выпадения или подбора.

Дроп разрешён только за новое уничтожение во время активной игры. Последний разрушаемый блок не порождает pickup: к событию `BrickDestroyed` счётчик уже равен нулю. Это сохраняет результат независимо от порядка подписчиков; последний score по-прежнему начисляется до победы. Дополнительных начислений за pickup нет.

ExpandPaddle задаёт целевую ширину от базовой, а не умножает текущую. Повторный подбор не увеличивает её ещё раз; таймера нет. Ширина сбрасывается при каждой потере жизни, в том числе последней, и при завершении уровня/партии. Reset возвращает `PaddleConfig.Width`, не записывает новые значения в asset.

На каждом `LifeLost` активное улучшение сбрасывается и все непойманные pickups удаляются. `LevelComplete`, `GameOver` и уничтожение gameplay scope также очищают их. В Ready новые pickups не создаются; падение и подбор разрешены только в Playing без паузы. Пауза останавливает оба действия; после возобновления pickup остаётся доступен, даже если уже находится у платформы. Подбор и удаление должны исключать повторное применение в том же physics tick.

Factory владеет созданием через DI, gameplay-обработчик — отслеживанием активных pickups и подписками. Pickup распознаёт только назначенную платформу и DeathZone; Ball, Brick и Wall не применяют эффект. Пропуск бонуса не вызывает `DeathZone.BallEntered` и не расходует жизнь.

Первоначальный план Composite и IBonusEffect/BonusContext заменён согласованной реализацией Stage 8: компоненты BonusEffect на prefab без промежуточных definitions. Загрузка через Addressables — Stage 9; анимации/звук — Stage 11. Pooling, таймеры, стеки и новые bonus-services на будущее не входят в Stage 7.

## Проверка и настройка

- **EditMode:** отсутствие профиля, шанс 0/1 и граница промежуточного шанса, некорректные числа и гарантированное число вызовов fake random.
- **PlayMode:** реальное уничтожение с гарантированным дропом; shield/damage/indestructible и повтор не создают pickup; последний блок завершает уровень без дропа. Пойманный bonus применяется ровно один раз, missed pickup удаляется без потери жизни, score/combo не меняются.
- **Регрессия lifecycle:** повторный ExpandPaddle, края поля и отскок, пауза/resume, удаление pickups и сброс ширины при LifeLost, последний проигрыш, победа и два restart без старых объектов/подписок. Проверить сохранность HP/щита и порядка последнего начисления из Stage 6.

Пользователь создаёт pickup prefab, назначает sprite, Rigidbody2D, trigger collider, скорость падения, слой и матрицу контактов с Paddle/DeathZone; связывает drop-профиль и prefab в Inspector. Точные шаги выдаются после соответствующего кода; сцены/prefab агент без прямого поручения не редактирует. После автоматических тестов проверить ручной сценарий и player build.

## Gate 7

Gate 7 пройден 2026-10-10 по подтверждению пользователя: тесты, все ручные сценарии, регрессия и Android build на смартфоне работают. Модель смартфона не указана; проверка iOS не заявлена. Это не меняет подтверждённую приёмку этапа на Android.

- [x] Только отмеченные definitions могут породить бонус.
- [x] Одна попытка на уникальное уничтожение; дропа с последнего блока нет, порядок score/победы сохранён.
- [x] Подбор применяет ExpandPaddle один раз; повторный bonus не накапливает ширину.
- [x] Непойманный pickup удаляется в DeathZone без изменения жизней; терминальные состояния и restart не оставляют старые объекты/подписки.
- [x] LifeLost удаляет все pickups и возвращает базовую ширину; пауза останавливает движение и подбор.
- [x] Sprite/collider согласованы, расширенная платформа остаётся внутри поля, отскок мяча корректен.
- [x] Пользователь подтвердил EditMode/PlayMode, ручной сценарий и player build.
