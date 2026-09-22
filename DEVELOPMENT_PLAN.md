# План разработки классического 2D Arkanoid

> Рабочий документ и линейный TODO-лист. Задачи выполняются сверху вниз. Следующий этап начинается только после прохождения `Gate` предыдущего этапа.

## 1. Цель проекта

Сделать небольшой, законченный классический 2D Arkanoid, который одновременно:

- понятен игроку без обучения;
- имеет аккуратную архитектуру без избыточных абстракций;
- использует VContainer как прозрачный composition-инструмент там, где DI действительно упрощает lifetimes и зависимости;
- демонстрирует осознанный выбор архитектурных решений: Decorator, Composite и Chain of Responsibility рассматриваются как кандидаты, а не как самоцель;
- использует Addressables с явным владением и освобождением ресурсов;
- содержит автоматические тесты правил, а не только MonoBehaviour-кода;
- собирается в готовый Windows-билд и сопровождается понятным README.

Главный принцип разработки: сначала получить простую работающую механику, затем стабилизировать её тестами, и только после этого связывать с другими системами или расширять паттернами.

## 2. Подтверждённые рабочие предположения

Эти решения подтверждены и считаются исходными ограничениями проекта:

1. Первый релиз и критерии готовности ориентированы на Windows desktop, landscape 16:9. Gameplay-логика не должна зависеть от Windows API или конкретного устройства ввода, чтобы новый runtime input adapter можно было добавить без изменения правил игры.
2. На первом этапе управление — `A/D` и стрелки, запуск мяча — `Space`; gamepad поддерживается теми же Input Actions. Управление платформой мышью считается опциональным улучшением.
3. В итоговой версии будет 3 коротких уровня.
4. Начальный визуальный слой — залитая цветом sprite-геометрия. Финальный арт владелец проекта выстраивает самостоятельно; генерация арта в scope разработки не входит.
5. VContainer используется для composition и управления lifetimes, Addressables — для локального level content. Decorator, Composite и Chain of Responsibility применяются только после сравнения с более простым решением и только если уменьшают связанность, ветвление или стоимость расширения конкретной механики.
6. Расширение платформы и временные модификаторы сбрасываются при потере жизни. Полноценная система таймеров и стекирования эффектов в базовый объём не входят.

Смена целевой платформы не должна затрагивать Core-правила. Если меняются доступные устройства, разрешения экрана или platform services, сначала обновляются runtime adapters и соответствующие acceptance criteria.

## 3. Границы первой версии

### Входит в объём

- главное меню, игровой экран, пауза, победа и поражение;
- платформа, один активный мяч, жизни и счёт;
- обычные, прочные, защищённые щитом и неразрушимые блоки;
- выпадение и подбор бонусов;
- одиночные и составные эффекты бонусов;
- базовый счёт, комбо и удвоение очков;
- 3 локально поставляемых Addressable-уровня;
- начальная графика из простых залитых цветом sprite-форм с возможностью независимо заменить её авторским артом;
- визуальная и звуковая обратная связь;
- EditMode-тесты правил и несколько ключевых PlayMode smoke-тестов;
- Windows-билд, README и короткое демонстрационное видео/GIF.

### Не входит в базовый объём

- мобильная адаптация и touch UI;
- онлайн-функции, таблица лидеров и аккаунты;
- сохранение прохождения между запусками;
- редактор уровней внутри игры;
- procedural generation;
- мультибол, лазеры и десятки типов бонусов;
- object pooling до появления измеримой необходимости;
- remote Addressables и сервер доставки контента;
- сложные шейдеры и уникальный арт-пайплайн.

Эти пункты можно брать только после завершения релизного Gate.

## 4. Исходное состояние проекта

Проверено на момент составления плана:

- Unity `6000.3.18f1`;
- URP `17.3.0`;
- Input System `1.19.0`, активен новый Input System;
- VContainer `1.19.0`;
- UniTask подключён Git dependency;
- DOTween установлен в `Assets/3rd-Party/Demigiant/DOTween`;
- определены `DOTWEEN` и `UNITASK_DOTWEEN_SUPPORT`;
- Unity Test Framework `1.6.0`;
- Addressables пока не установлен;
- в Build Settings находится только `Assets/Scenes/SampleScene.unity`;
- собственных C#-скриптов пока нет.

Отдельно: `productName` сейчас записан как `Arcanoid`, тогда как проект и этот документ используют `Arkanoid`. Исправление включено в этап 0.

## 5. Целевая структура

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

## 6. Архитектурные правила

Направление зависимостей:

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

### VContainer scopes

- `AppLifetimeScope` в `Bootstrap` хранит долгоживущую инфраструктуру: navigation, level catalog/loader, audio service и фабрику уровня.
- `GameplayLifetimeScope` создаёт session-scoped сервисы: `GameSession`, `ScoreService`, `BonusService`, gameplay presenters/controllers.
- Уровень получает отдельный дочерний scope или явный `LevelContext`, который уничтожается при смене уровня.
- Обычные C# entry points регистрируются через VContainer lifecycle interfaces только когда им действительно нужен Unity PlayerLoop.
- Динамические prefab instances, которым требуется injection, создаются через фабрику на границе с `IObjectResolver`.

### Правило выбора паттернов

Перед реализацией Decorator, Composite или Chain of Responsibility выполняется короткая архитектурная проверка:

1. Сначала формулируется реальная изменчивость механики: что добавляется, в каком порядке комбинируется и что должно останавливаться досрочно.
2. Описывается самое простое читаемое решение без именованного паттерна.
3. Паттерн выбирается, только если он убирает разрастающиеся условные ветки, изолирует независимые правила, даёт естественную композицию или заметно упрощает тестирование и добавление нового варианта.
4. Если ожидается один стабильный вариант и паттерн добавляет больше типов и косвенности, остаётся простая реализация.
5. Решение и его причина кратко фиксируются в README. Отказ от паттерна с хорошим обоснованием считается таким же корректным инженерным результатом, как его применение.

Название паттерна не является acceptance criterion. Критериями остаются понятность кода, изоляция изменений, тестируемость и корректное игровое поведение.

## 7. Общий Definition of Done

Задача считается завершённой, только если:

- поведение можно проверить по указанному acceptance criterion;
- Unity Console не содержит новых ошибок и необъяснённых предупреждений;
- обязательные ссылки Inspector назначены, а их отсутствие не скрывается тихими `null`-проверками;
- изменённая логика покрыта тестом там, где тест даёт ценность;
- нет временного кода, закомментированных блоков и debug shortcuts в релизном пути;
- изменения проверены в Play Mode;
- после этапов, меняющих контент или загрузку, проверен player build;
- README/этот план обновлены, если фактическое решение отличается от запланированного.

## 8. Линейный TODO

### Этап 0. Зафиксировать требования и подготовить основание

**Результат:** проект открывается без ошибок, имеет понятную структуру, зависимости и минимальный composition root.

- [x] `P0.1` Подтвердить и зафиксировать решения из раздела 2: текущая платформа, управление, число уровней, визуальный scope и правило выбора паттернов.
- [ ] `P0.2` Создать короткий `README.md` с целью проекта, версией Unity и инструкцией запуска; расширять его по мере разработки.
- [ ] `P0.3` Исправить Player Settings `productName` с `Arcanoid` на `Arkanoid`. Не переименовывать папку проекта и solution без отдельной необходимости.
- [ ] `P0.4` Создать структуру `Assets/Content` из раздела 5 и перенести собственные assets из template-папок, сохраняя `.meta`.
- [ ] `P0.5` Создать asmdef-файлы `Arkanoid.Core`, `Arkanoid.Runtime`, `Arkanoid.Tests.EditMode`, `Arkanoid.Tests.PlayMode` и проверить направление references.
- [ ] `P0.6` Удалить из будущего gameplay Input Actions лишний template-набор. Создать actions: `Move`, `Launch`, `Pause`, `Confirm`, `Cancel`.
- [ ] `P0.7` Настроить keyboard и gamepad bindings. Не добавлять touch bindings до решения о mobile scope.
- [ ] `P0.8` Проверить DOTween Setup, активные модули Sprite/UI/Physics2D и compile symbols. Не изменять сторонние исходники.
- [ ] `P0.9` Установить совместимую с Unity 6.3 версию Addressables через Package Manager и зафиксировать фактическую версию в README.
- [ ] `P0.10` Создать пустые `Bootstrap` и `Gameplay` scenes; добавить `Bootstrap` первой сценой Build Settings.
- [ ] `P0.11` Создать `AppLifetimeScope` и `GameplayLifetimeScope` с одной простой тестовой регистрацией; проверить создание и disposal scope без ошибок.
- [ ] `P0.12` Добавить один проходящий EditMode smoke-test, чтобы проверить корректность test assemblies.
- [ ] `P0.13` Сделать baseline commit/tag, от которого можно сравнивать последующие изменения.

**Gate 0**

- [ ] Проект компилируется с нулём ошибок.
- [ ] `Bootstrap` запускает `Gameplay`.
- [ ] Test Runner видит EditMode и PlayMode assemblies.
- [ ] Addressables и DOTween проходят базовую инициализацию.

### Этап 1. Изолированная механика платформы

**Результат:** платформа предсказуемо двигается в ограниченном поле без мяча, блоков и игрового состояния.

- [ ] `P1.1` Настроить orthographic camera и игровую область под 16:9; определить единицы поля и безопасные границы.
- [ ] `P1.2` Создать слои `Paddle`, `Ball`, `Brick`, `Wall`, `Pickup`, `DeathZone` и collision matrix с минимально нужными взаимодействиями.
- [ ] `P1.3` Создать prefab `Paddle` с `Rigidbody2D`, collider и простым SpriteRenderer.
- [ ] `P1.4` Создать `PaddleConfig` с speed, width и movement bounds.
- [ ] `P1.5` Реализовать `IPlayerInput` adapter над Input System. Gameplay-код не должен читать `Keyboard.current` напрямую.
- [ ] `P1.6` Реализовать `PaddleMovement`: чтение нормализованного axis, движение в physics tick, clamp по границам поля.
- [ ] `P1.7` Проверить одинаковую скорость при разных frame rates и отсутствие дрожания у границ.
- [ ] `P1.8` Добавить тест чистой функции расчёта/clamp новой позиции.

**Gate 1**

- [ ] Платформа управляется с клавиатуры и gamepad.
- [ ] Не выходит за левую и правую границы.
- [ ] Поведение не зависит заметно от render frame rate.

### Этап 2. Изолированная механика мяча

**Результат:** один мяч запускается, стабильно движется и корректно отражается от стен и платформы.

- [ ] `P2.1` Создать prefab `Ball` с `Rigidbody2D`, `CircleCollider2D` и frictionless/bouncy `PhysicsMaterial2D`.
- [ ] `P2.2` Создать `BallConfig`: speed, launch angle range, minimum vertical component, maximum correction threshold.
- [ ] `P2.3` Реализовать состояния мяча `Attached`, `Flying`, `Lost`.
- [ ] `P2.4` В состоянии `Attached` удерживать мяч относительно платформы; по `Launch` задавать нормализованное стартовое направление.
- [ ] `P2.5` Поддерживать постоянную целевую скорость мяча после столкновений без разгона от physics solver.
- [ ] `P2.6` Реализовать отражение от платформы в зависимости от точки контакта: центр даёт более вертикальный отскок, край — более горизонтальный.
- [ ] `P2.7` Ограничить почти горизонтальные и почти вертикальные траектории, чтобы мяч не зацикливался.
- [ ] `P2.8` Создать стены сверху/слева/справа и trigger `DeathZone` снизу.
- [ ] `P2.9` Добавить визуальный debug режима направления/скорости только для Editor/Development Build.
- [ ] `P2.10` Добавить тесты расчёта launch direction и paddle bounce direction.

**Gate 2**

- [ ] Мяч ожидает запуска на платформе.
- [ ] После запуска не теряет скорость и не приобретает бесконечно горизонтальную траекторию.
- [ ] Отскок от разных участков платформы заметно меняет направление.
- [ ] DeathZone однозначно сообщает о потере мяча.

### Этап 3. Простые блоки и первый вертикальный срез

**Результат:** существует минимальная полностью играбельная партия с одним уровнем.

- [ ] `P3.1` Создать prefab простого блока с collider, view и идентификатором.
- [ ] `P3.2` Реализовать простое правило: одно попадание уничтожает блок и порождает событие `BrickDestroyed`.
- [ ] `P3.3` Собрать небольшой тестовый layout вручную, без Addressables и генератора уровня.
- [ ] `P3.4` Реализовать `GameSession` с состояниями `Ready`, `Playing`, `LifeLost`, `LevelComplete`, `GameOver`.
- [ ] `P3.5` Реализовать `LivesModel` с 3 жизнями и явными событиями изменения.
- [ ] `P3.6` Связать DeathZone с потерей жизни: остановить текущий мяч, сбросить позицию платформы/мяча, перейти в `Ready` или `GameOver`.
- [ ] `P3.7` Реализовать подсчёт оставшихся разрушаемых блоков и переход в `LevelComplete` при нуле.
- [ ] `P3.8` Добавить минимальный HUD: lives, score placeholder, текст состояния и кнопка restart.
- [ ] `P3.9` Реализовать pause, блокирующую gameplay input и physics simulation выбранным единообразным способом.
- [ ] `P3.10` Добавить EditMode-тесты переходов `GameSession` и `LivesModel`.
- [ ] `P3.11` Добавить PlayMode smoke-test: запуск → уничтожение последнего блока → `LevelComplete`.

**Gate 3 — первый playable milestone**

- [ ] Игрок может начать партию, разбить все блоки, победить, потерять все жизни и проиграть.
- [ ] Restart не требует перезапуска Editor/приложения.
- [ ] HUD показывает актуальные жизни и состояние.
- [ ] Нет зависимости от будущих бонусов, Addressables или паттернов.

### Этап 4. Стабилизировать композицию через VContainer

**Результат:** работающий vertical slice собран через понятные scopes и явные зависимости.

- [ ] `P4.1` Выписать фактические lifetime всех созданных объектов: app, gameplay session, level, scene object.
- [ ] `P4.2` Зарегистрировать app services в `AppLifetimeScope` с обоснованными `Singleton`/`Scoped` lifetimes.
- [ ] `P4.3` Зарегистрировать session services и scene components в `GameplayLifetimeScope`.
- [ ] `P4.4` Перенести orchestration из случайных `Start/Awake` в один-два entry point/presenter-класса.
- [ ] `P4.5` Внедрять обычные C#-зависимости через constructor; scene MonoBehaviour — через регистрацию component и injection method только при необходимости.
- [ ] `P4.6` Создать узкие factories для динамических ball/brick/pickup instances. Скрыть `IObjectResolver` внутри factory implementations.
- [ ] `P4.7` Убедиться, что gameplay-классы не вызывают `Resolve`, `Find*`, `GameObject.Find` и не используют статические singleton instances.
- [ ] `P4.8` Добавить composition smoke-test или PlayMode-test, который строит scope и разрешает основные entry points.

**Gate 4**

- [ ] Поведение Gate 3 не изменилось.
- [ ] По composition root видно, где и как создаётся каждая крупная система.
- [ ] Уничтожение gameplay scope освобождает session objects и отписки.

### Этап 5. Расширяемый расчёт счёта (кандидат: Decorator)

**Результат:** базовый счёт, комбо и удвоение очков реализованы без связанных между собой условных веток; выбранная структура обоснована фактическими правилами.

- [ ] `P5.1` Полностью записать правила счёта примерами: base score, combo, double score, combo+double и reset при потере мяча.
- [ ] `P5.2` Определить минимальный контракт расчёта и необходимые входные данные, не закладывая будущие модификаторы, которых ещё нет.
- [ ] `P5.3` Реализовать базовый расчёт и `ComboModel`; покрыть их тестами до добавления double score.
- [ ] `P5.4` Сравнить три варианта для независимых модификаторов: одна явная формула, ordered modifier pipeline и Decorator. Зафиксировать, какой вариант делает порядок и добавление следующего реального modifier наиболее понятными.
- [ ] `P5.5` Реализовать выбранный вариант. Если выбран Decorator, использовать композицию вида `DoubleScore(ComboScore(BaseScore))`; если нет — не создавать классы с суффиксом `Decorator` только ради демонстрации названия.
- [ ] `P5.6` Явно зафиксировать порядок применения combo и multiplier и проверить его примерами в тестах.
- [ ] `P5.7` Реализовать `ScoreService`, который принимает результат calculator/pipeline, изменяет total и публикует событие для HUD.
- [ ] `P5.8` Связать уничтожение блока со `ScoreService`, не связывая BlockView напрямую с UI.
- [ ] `P5.9` Показать score и combo в HUD; анимацию оставить на этап polish.
- [ ] `P5.10` Добавить parameterized tests выбранной композиции: base score, combo, double, combo+double, reset combo, отсутствие отрицательного результата.
- [ ] `P5.11` Кратко записать в README, почему Decorator выбран или отклонён для этой механики.

**Gate 5**

- [ ] Правило base score не знает о деталях combo и double score.
- [ ] Порядок модификации результата виден в одном месте и подтверждён тестами.
- [ ] Добавление нового реального modifier не требует менять логику существующих modifiers.
- [ ] HUD отображает тот же итог, который хранит `ScoreService`.

### Этап 6. Обработка попадания в блок (кандидат: Chain of Responsibility)

**Результат:** единый запрос попадания предсказуемо обрабатывает защитные свойства блока, а выбранная структура не усложняет простые типы блоков.

- [ ] `P6.1` Создать `BrickDefinition`: base score, max health, shield charges, indestructible flag, presentation reference/id.
- [ ] `P6.2` Создать runtime `BrickState`: current health, current shield, destroyed state. Не изменять ScriptableObject во время игры.
- [ ] `P6.3` Определить `BrickHitRequest` и `BrickHitResult` с достаточными данными для feedback и score.
- [ ] `P6.4` Записать порядок правил и точки short-circuit: indestructible прекращает обработку; shield поглощает удар; damage применяется только после снятия защиты.
- [ ] `P6.5` Сравнить один явный `BrickHitProcessor`, таблицу правил и Chain of Responsibility. Выбрать цепочку только если handlers действительно независимо добавляются/переставляются или ранняя остановка становится понятнее, чем единый метод.
- [ ] `P6.6` Реализовать выбранную структуру. Для Chain of Responsibility использовать узкий `IBrickHitHandler`; для простого processor сохранить те же `BrickHitRequest`/`BrickHitResult` и не имитировать handlers искусственными классами.
- [ ] `P6.7` Реализовать правила indestructible, shield и damage без дублирования состояния между слоями.
- [ ] `P6.8` Собирать выбранный processor/набор правил в одном factory/composition-коде, а не через условные ветки в `Ball`.
- [ ] `P6.9` Сделать четыре визуально различимых типа: normal, durable, shielded, indestructible.
- [ ] `P6.10` Исключить indestructible blocks из условия завершения уровня.
- [ ] `P6.11` Добавить тесты порядка и short-circuit независимо от выбранной реализации: indestructible не получает damage; shield поглощает удар; damage применяется после снятия shield; durable block требует N попаданий.
- [ ] `P6.12` Кратко записать в README, почему Chain of Responsibility выбран или отклонён.

**Gate 6**

- [ ] Ball сообщает только факт столкновения/попадания и не знает разновидностей блоков.
- [ ] Результат обработки однозначно определяет feedback, score и состояние блока.
- [ ] Новое защитное правило добавляется в одном месте и не требует менять Ball/UI.
- [ ] Уровень завершается после уничтожения всех разрушаемых блоков.

### Этап 7. Базовое выпадение и подбор бонуса

**Результат:** уничтоженный блок может породить pickup, который падает и применяется при контакте с платформой.

- [ ] `P7.1` Определить `BonusDropDefinition`: chance/weight, pickup prefab id, effect definition id.
- [ ] `P7.2` Ввести `IRandomProvider`, чтобы выпадение было воспроизводимо в тестах. Runtime implementation использует Unity random только за adapter boundary.
- [ ] `P7.3` Реализовать `BonusDropService`, который решает только факт/тип выпадения.
- [ ] `P7.4` Создать pickup prefab: движение вниз, collider trigger, визуальный тип, уничтожение в DeathZone.
- [ ] `P7.5` Создать `BonusFactory`, которая порождает pickup через VContainer-aware factory boundary.
- [ ] `P7.6` Реализовать первый одиночный бонус `ExpandPaddle` без преждевременной абстракции группы эффектов.
- [ ] `P7.7` Сбрасывать размер платформы при потере жизни и завершении gameplay scope.
- [ ] `P7.8` Добавить тесты drop chance/weight с fake random и применения одиночного эффекта.

**Gate 7**

- [ ] Только отмеченные definitions могут породить бонус.
- [ ] Непойманный pickup корректно уничтожается.
- [ ] Пойманный pickup применяется один раз.
- [ ] Потеря жизни возвращает платформу к базовому размеру.

### Этап 8. Составные бонусы (кандидат: Composite)

**Результат:** pickup применяет одиночные и составные бонусы через один понятный контракт без специальных проверок типов.

- [ ] `P8.1` Описать два реальных use case: одиночный `ExpandPaddle` и составной `Rescue = ExpandPaddle + AddLife`.
- [ ] `P8.2` Определить минимальный `IBonusEffect.Apply(BonusContext)` и перенести `ExpandPaddleEffect` на этот контракт.
- [ ] `P8.3` Реализовать независимые effects: `AddLifeEffect`, `AddScoreEffect`, `EnableDoubleScoreEffect`.
- [ ] `P8.4` Сравнить простой список effects в `BonusDefinition` и `CompositeBonusEffect`, который сам реализует `IBonusEffect`. Выбрать Composite, только если группу действительно нужно передавать, тестировать или комбинировать как один effect.
- [ ] `P8.5` Реализовать выбранную композицию без type checks в pickup. Не добавлять nested composites, пока конкретный игровой bonus не потребует вложенности.
- [ ] `P8.6` Создать serializable definitions и `BonusEffectFactory`, собирающую runtime effects без передачи container внутрь effect.
- [ ] `P8.7` Создать составной бонус `Rescue`: `ExpandPaddle + AddLife`. Ограничить жизни заданным maximum.
- [ ] `P8.8` Создать бонус `DoubleScore`, действующий до потери жизни; связать его с выбранным механизмом расчёта счёта из этапа 5.
- [ ] `P8.9` Определить политику повторного подбора: базовый scope — refresh/no stack для размера и double score.
- [ ] `P8.10` Добавить тесты: одиночный effect, группа применяет каждый effect один раз и в ожидаемом порядке, maximum lives, reset on life lost.
- [ ] `P8.11` Кратко записать в README, почему Composite выбран или почему простого списка effects достаточно.

**Gate 8**

- [ ] Pickup-код зависит только от `IBonusEffect` и не проверяет конкретный тип эффекта.
- [ ] Группировка effects находится в bonus domain/composition-коде, а не в pickup view.
- [ ] Решение не поддерживает вложенность и сценарии, которых фактически нет в игре.
- [ ] Сочетание bonus effect и механизма расчёта счёта работает после потери жизни и restart без утечки состояния.

### Этап 9. Data-driven уровни и Addressables

**Результат:** три уровня загружаются локально через Addressables, сменяются без утечек и не требуют ручного размещения блоков в сцене.

- [ ] `P9.1` Создать `LevelDefinition` ScriptableObject: id, display name, grid/placements, brick definition ids, bonus overrides, optional theme.
- [ ] `P9.2` Выбрать один формат layout и не смешивать подходы. Рекомендуется компактная grid-модель с пустыми cells и ссылками на `BrickDefinition`.
- [ ] `P9.3` Реализовать обычный `LevelBuilder`, который получает уже загруженный definition и создаёт blocks через factory.
- [ ] `P9.4` Перевести тестовый ручной layout этапа 3 в `LevelDefinition`; сравнить игровой результат до удаления ручной версии.
- [ ] `P9.5` Создать локальные Addressables groups: `LevelData`, `LevelPrefabs`, `SharedGameplay` с осмысленными labels/addresses.
- [ ] `P9.6` Определить `ILevelLoader` и `AddressablesLevelLoader`.
- [ ] `P9.7` Возвращать из loader явный lease/loaded-level object, владеющий operation handles. Не отдавать наружу «голый» asset без владельца.
- [ ] `P9.8` При создании prefab из загруженного asset сохранять handle до уничтожения всех instances; не освобождать asset сразу после `Instantiate`.
- [ ] `P9.9` Связать загрузку UniTask с cancellation token gameplay/level scope. Обработать cancel отдельно от реальной ошибки.
- [ ] `P9.10` При смене уровня: остановить gameplay → уничтожить level instances/scope → освободить handles → загрузить следующий level → начать `Ready`.
- [ ] `P9.11` Добавить loading overlay и error state с `Retry`/`Back to Menu`; не скрывать исключение пустым catch.
- [ ] `P9.12` Создать 3 уровня с растущей сложностью и гарантированно разрушаемым layout.
- [ ] `P9.13` Выполнить Addressables content build и player build из чистого состояния.
- [ ] `P9.14` Проверить повторный цикл `Level 1 → 2 → 3 → restart → menu` и Memory Profiler/Addressables Event Viewer на удержанные handles.

**Gate 9**

- [ ] Gameplay scene не содержит вручную расставленных level blocks.
- [ ] Все 3 уровня проходят по очереди.
- [ ] Ошибка загрузки приводит в управляемое UI-состояние.
- [ ] Каждый успешный load имеет симметричный release после уничтожения instances.

### Этап 10. Полный игровой flow и UI

**Результат:** приложение воспринимается как законченная небольшая игра, а не тестовая сцена.

- [ ] `P10.1` Реализовать state flow: `Boot → MainMenu → Loading → Ready → Playing → Paused/LifeLost → LevelComplete → Loading/RunComplete → MainMenu`.
- [ ] `P10.2` Сделать Main Menu: Play, Quit; в Editor Quit не генерирует ошибку.
- [ ] `P10.3` Доработать HUD: score, lives, level, combo, активные modifiers.
- [ ] `P10.4` Сделать overlays: Ready/Launch, Pause, Level Complete, Game Over, Victory, Loading Error.
- [ ] `P10.5` Настроить navigation и focus для keyboard/gamepad UI.
- [ ] `P10.6` Гарантировать, что UI только отображает state и отправляет commands, но не определяет правила победы/поражения.
- [ ] `P10.7` Проверить повторные входы в gameplay: не остаются старые subscriptions, tweens, cancellation sources и session state.
- [ ] `P10.8` Добавить PlayMode smoke-test полного flow хотя бы через один минимальный test level.

**Gate 10**

- [ ] Игру можно пройти от запуска приложения до Victory и вернуться в меню.
- [ ] Все экраны доступны без мыши.
- [ ] Pause/restart/menu работают из каждого разрешённого состояния.

### Этап 11. Feedback, DOTween и звук

**Результат:** каждое важное событие читается визуально и на слух, но presentation не управляет правилами.

- [ ] `P11.1` Составить таблицу feedback events: paddle hit, brick damaged, shield broken, brick destroyed, pickup spawned/collected, life lost, level complete.
- [ ] `P11.2` Добавить короткий scale/color tween при попадании в блок.
- [ ] `P11.3` Добавить отдельный shield-break feedback, визуально отличимый от damage.
- [ ] `P11.4` Добавить pickup spawn/collect tween и HUD punch при изменении score/lives.
- [ ] `P11.5` Добавить частицы только для событий, где они улучшают читаемость.
- [ ] `P11.6` Создать `AudioService` и простой набор SFX; не вызывать `AudioSource` из Core.
- [ ] `P11.7` Связать lifecycle твинов с владельцем через `SetLink` или явный `Kill`. Для `Sequence` управлять самой sequence.
- [ ] `P11.8` Не использовать `SetAutoKill(false)` без фактического повторного использования.
- [ ] `P11.9` Если tween ожидается через UniTask, использовать корректную семантику ожидания завершения и cancellation; не делать `async void`.
- [ ] `P11.10` Проверить, что уничтожение уровня во время анимаций не создаёт callbacks в уничтоженные objects.

**Gate 11**

- [ ] Игрок различает damage, shield и destruction без чтения Console.
- [ ] Отключение presentation/звука не ломает gameplay result.
- [ ] При смене уровня нет DOTween warnings и обращений к destroyed objects.

### Этап 12. Тестирование, устойчивость и производительность

**Результат:** основные риски проекта покрыты автоматическими и ручными проверками.

- [ ] `P12.1` Довести EditMode coverage критичных правил: session transitions, lives, выбранная композиция score modifiers, combo, обработка brick hit, drop selection, группировка bonus effects, level completion.
- [ ] `P12.2` Держать каждый тест сфокусированным на одном наблюдаемом правиле; не тестировать private implementation details.
- [ ] `P12.3` Добавить PlayMode tests только для границ Unity: physics contact, scope composition, level load/unload, main flow.
- [ ] `P12.4` Выполнить ручную матрицу разрешений минимум 16:9 и 16:10; проверить Canvas scaling и границы камеры.
- [ ] `P12.5` Выполнить 20 последовательных restart/level transitions и проверить Console.
- [ ] `P12.6` Проверить ball edge cases: угол стены, стык colliders, край платформы, высокая скорость, pause во время контакта.
- [ ] `P12.7` Профилировать CPU/GC в Development Build. Исправлять только измеримые проблемы.
- [ ] `P12.8` Проверить Addressables handles и память после полного прохождения и возврата в меню.
- [ ] `P12.9` Проверить отсутствие per-frame allocations в основных `Update/FixedUpdate` loops.
- [ ] `P12.10` Запустить все EditMode/PlayMode tests после clean reimport и сохранить итог в release checklist.

**Gate 12**

- [ ] Все тесты зелёные.
- [ ] В типичном gameplay нет повторяющегося GC allocation из собственного кода.
- [ ] Полный run не оставляет ошибок, tween warnings и загруженных level resources после возврата в меню.

### Этап 13. Релиз и демонстрация инженерных решений

**Результат:** проверяющий может быстро запустить игру, понять архитектуру и увидеть выполнение требований.

- [ ] `P13.1` Обновить README: Unity version, setup, управление, запуск Editor/build, известные ограничения.
- [ ] `P13.2` Добавить таблицу `Требование → Реализация → Файл/класс → Как проверить в игре`.
- [ ] `P13.3` Кратко описать VContainer scopes и почему container не используется как Service Locator.
- [ ] `P13.4` Кратко описать решения для score modifiers, bonus effects и brick hits: какие паттерны рассматривались, что было выбрано или отклонено и почему это проще поддерживать.
- [ ] `P13.5` Описать Addressables ownership: кто загружает, когда живут instances, кто и когда освобождает handles.
- [ ] `P13.6` Добавить diagram основных зависимостей и state flow, не дублируя весь код.
- [ ] `P13.7` Подготовить release Windows build из зафиксированного commit.
- [ ] `P13.8` Пройти build с нуля по пользовательскому сценарию и проверить управление/звук/UI.
- [ ] `P13.9` Записать видео 60–90 секунд: normal/durable/shielded block, составной bonus, double+combo score, смена Addressable level, victory/game over.
- [ ] `P13.10` Проверить репозиторий: нет `Library`, `Temp`, `Logs`, build cache, локальных IDE-файлов и чужих лицензируемых assets без attribution.
- [ ] `P13.11` Создать release tag и приложить build, README и видео/GIF.

**Release Gate**

- [ ] Проект импортируется и компилируется на указанной версии Unity.
- [ ] Build запускается без установки дополнительных инструментов.
- [ ] Три уровня полностью проходимы.
- [ ] Все заявленные технологии видны и объяснены, но не усложняют базовый gameplay.
- [ ] Из README за 3–5 минут понятно, что проверять на собеседовании.

## 9. Рекомендуемые контрольные точки

| Milestone | После этапа | Демонстрируемый результат |
|---|---:|---|
| Foundation | 0 | Чистый проект, scopes, tests, зависимости |
| Mechanics prototype | 2 | Платформа и качественный отскок мяча |
| Playable MVP | 3 | Победа, поражение, жизни, простые блоки |
| Architecture pass | 4 | Явные lifetimes и DI через VContainer |
| Architecture decisions complete | 8 | Расширяемые score, brick hit и bonus mechanics с обоснованными решениями |
| Content complete | 10 | Три Addressable-уровня и полный flow |
| Release candidate | 12 | Тесты, стабильность, отсутствие утечек |
| Portfolio release | 13 | Build, README и видео |

Ориентир для одного опытного разработчика при готовых простых assets: 8–12 полноценных рабочих дней. Если создание визуала и звука выполняется с нуля, лучше планировать ещё 2–4 дня. Срок не должен сокращаться за счёт отскоков мяча, тестов загрузки или release documentation — это наиболее показательные части работы.

## 10. Порядок коммитов

Рекомендуется один небольшой осмысленный commit на завершённую задачу или компактную группу задач:

```text
chore: initialize project structure and test assemblies
feat: add constrained paddle movement
feat: add stable ball launch and paddle bounce
feat: complete minimal game session flow
refactor: compose gameplay services with VContainer
feat: add composable score modifiers
feat: process brick hits through handler chain
feat: apply grouped bonus effects
feat: load level content with Addressables
test: cover gameplay rules and level lifecycle
docs: add architecture and verification guide
```

Не объединять весь проект в один итоговый commit: история изменений сама является частью демонстрации инженерного процесса.

## 11. Идеи только после Release Gate

- управление платформой мышью и touch;
- multi-ball с корректным условием потери жизни;
- временные эффекты с duration/stacking policy;
- object pooling для pickups/particles;
- level selection и локальный progress save;
- accessibility options: reduced motion, volume controls, high-contrast palette;
- WebGL build;
- editor tooling для проверки LevelDefinition.

Каждое расширение начинается с отдельного мини-ТЗ, acceptance criteria и оценки влияния на существующие правила.
