# Stage 8 — настройка и приёмка уникальных бонусов

[К этапу 8](08-bonus-composite.md)

Актуальный контент с 2026-10-11: ExpandPaddle, AddLife, DoubleScore. Rescue заменён отдельным AddLife с сохранением GUID definition/prefab; Comeback и оба группирующих effect assets удалены. Механизм Composite проверяется техническими тестами, сохранённых групп в игре нет. После этой правки Unity импорт, Test Runner, Play Mode и Android build агент не запускал; Gate 8 открыт.

## 1. Импорт и назначения

| Definition в Data/Bonuses/Definitions | Effect в Data/Bonuses/Effects | Prefab в Prefabs/Gameplay/Bonuses |
|---|---|---|
| ExpandPaddleBonus | ExpandPaddleEffect | BonusPickup, бирюзовый |
| AddLifeBonus | AddLifeEffect | AddLifePickup, зелёный |
| DoubleScoreBonus | EnableDoubleScoreEffect | DoubleScorePickup, оранжевый |

- [ ] Дождаться импорта Unity. Проверить отсутствие Missing в трёх definitions и variants; Rescue/Comeback assets/prefabs отсутствуют.
- [ ] Проверить AddLifeBonus: Effect=AddLifeEffect, Pickup Prefab=AddLifePickup. Зелёный variant наследует BonusPickup, root name=AddLifePickup.
- [ ] Проверить активный root/BonusPickup, конечный Fall Speed>0; Rigidbody2D Kinematic/Simulated, Y не заморожен; enabled trigger BoxCollider2D. Rigidbody/Collider принадлежат корню.
- [ ] Слой Pickup и существующая матрица контактов: Paddle/DeathZone включены, Ball/Brick/Wall/Pickup выключены.
- [ ] Платформа использует согласованные sprite/collider; расширение удерживает её внутри поля.

Сохранены GUID AddLifeBonus `fcefc0ffc8dfded190c2873e0eb30221` и AddLifePickup `57973cc438b262d4f8f1572edcf7e519` от бывшего Rescue. Повторно назначать эти ссылки обычно не требуется.

## 2. Профиль выпадения

Профиль `Assets/Content/Data/Bonuses/BonusDropDefinition.asset` сохранён с текущей пользовательской настройкой: Chance=1, одна запись DoubleScoreBonus, Weight=1. Профиль и ссылки блоков на него не пересоздавались.

- [ ] Проверить профиль в Inspector: нет Missing или ссылок на удалённый Comeback; каждая запись имеет Bonus и конечный Weight>0.
- [ ] Для физических сценариев временно ставить Chance=1 и единственную запись нужного бонуса. Менять вариант вне Play Mode.
- [ ] Для проверки трёх AddLife pickups оставить минимум четыре разрушаемых блока: последний не порождает бонус.
- [ ] Проверить Default/Normal, Durable, Shielded; definition без профиля и Indestructible нужны для отрицательных сценариев.
- [ ] После проверки собрать общую таблицу ExpandPaddleBonus/AddLifeBonus/DoubleScoreBonus, стартовые Weight=1, Chance=0.25. Сохранить профиль; баланс можно изменить позже.

Старая схема одного Pickup Prefab не используется. Назначенная пустая таблица, null/effect/prefab, некорректная физика/вес/шанс отклоняют весь профиль до random даже при Chance=0. Технические composite definitions также проверяются на пустые группы/циклы.

## 3. Автоматическая регрессия

- [ ] `Window → General → Test Runner`: EditMode → Run All.
- [ ] BonusContentTests: 6 кейсов; три сохранённых эффекта меняют только своё состояние, definitions ссылаются на правильный effect и валидный prefab.
- [ ] BonusEffectFactoryTests (24) и CompositeBonusEffectTests (9): технические группы, порядок, вложенность, снимок, общие ссылки и ошибки. Rescue/Comeback assets не нужны.
- [ ] BonusEffectTests, LivesModelTests, BonusDropSettingsTests (15), BonusDropServiceTests (44), BonusDropDefinitionTests (35) и прежние score/combo/brick/game-flow fixtures.
- [ ] PlayMode → Run All. BonusIntegrationTests (34): три уникальных варианта weighted table, AddLife до/на пределе, физический подбор ровно один раз, DoubleScore и последнее начисление, pause/DeathZone/lifecycle/disposal/restart scope.
- [ ] Пройти также Bootstrap/LevelCompletion/LifeLoss smoke и brick/score integration fixtures.
- [ ] Console без новых исключений, MissingReference и NullReference; сохранить failed tests при сбое.

Fixtures создают временные объекты; они не подтверждают назначения gameplay-сцены. BonusContentTests читает реальные assets. Пересоздание scope не заменяет два настоящих restart.

## 4. Ручные сценарии из Bootstrap

Score/combo сравнивать непосредственно до/после подбора: удары мяча сами меняют эти значения. У бонусов нет таймера.

| № | Действие | Ожидаемый результат |
|---:|---|---|
| 1 | Начать партию, уничтожить непоследний блок с профилем. | Lives=3, базовая ширина, score/combo=0 до игры; один падающий pickup, очки только за блок. |
| 2 | ExpandPaddle: поймать два pickups, в том числе у краёв; отбить мяч центром/краями. | Базовая ширина ×1.5 без дальнейшего роста; sprite/collider согласованы, платформа в поле. Lives/double/score/combo от подбора не меняются. |
| 3 | AddLife: поймать три pickups подряд с 3 жизней. | 3 → 4 → 5 → 5, HUD меняется только при увеличении; на 5 pickup потребляется. Ширина/double/score/combo не меняются. |
| 4 | DoubleScore: поймать бонус дважды. | Включён ×2 следующих начислений; повтор не даёт ×4. Ширина/lives/score/combo от подбора не меняются. |
| 5 | Получить DoubleScore при combo=3, уничтожить блок BaseScore=100. | Combo=4, за блок +800 (100 ×4 ×2). |
| 6 | Пропустить по очереди каждый вариант в DeathZone; проверить чужие контакты. | Pickup удаляется без эффекта и без потери жизни; мяч/блоки/стены не собирают бонус. |
| 7 | Поставить паузу при падении/контакте каждого варианта, затем resume. | На паузе нет падения/применения; после resume ровно один подбор с действием выбранного бонуса. |
| 8 | Отдельно подобрать AddLife, ExpandPaddle и DoubleScore; оставить падающие pickups и потерять жизнь. | Жизни уменьшаются на 1, добавленные жизни не откатываются целиком; pickups удалены, ширина базовая, double=false, total сохранён, combo=0. Следующий блок на 100 даёт +100. |
| 9 | Потерять последнюю жизнь. | Очистка при LifeLost, затем GameOver, lives=0; бонус не воскрешает игру, новых pickups нет. |
| 10 | Завершить уровень с double и падающими pickups. | Последнее начисление удвоено до LevelComplete; последний блок без дропа. Затем сброс ширины/double и очистка pickups, total сохранён. |
| 11 | Shield/damage/indestructible/повтор/последний блок/нет профиля/Chance=0. | Дропа нет; попытка только при новом уничтожении непоследнего разрушаемого блока с профилем. |
| 12 | Общая таблица и два настоящих restart после бонусов. | На успешную попытку один из трёх вариантов; каждый restart: lives=3, score/combo=0, double=false, базовая ширина, нет старых pickups/подписок. |

- [ ] Все сценарии выполнены; точные границы random подтверждены fake-random тестами.

## 5. Устройство и подтверждение

- [ ] Сохранить назначения и итоговый профиль; сделать Android player build и запустить на смартфоне.
- [ ] Проверить различимость трёх бонусов, сенсорное управление, HUD lives, DoubleScore, края платформы, паузу, потерю жизни, победу и restart.
- [ ] Сообщить результаты EditMode/PlayMode, ручных сценариев, двух restart и Android. При сбое — название теста/сценария и текст ошибки. iOS не считается проверенным по Android.

Gate 8 закрывается после подтверждения пользователя. Требования ручного Rescue/Comeback и демонстрации их вложенности отменены.
