# Stage 8 — настройка и приёмка уникальных бонусов

[К этапу 8](08-bonus-composite.md)

Актуальная реализация с 2026-10-11: один Runtime-компонент BonusEffect на корне каждого игрового pickup. Контекст, definitions/assets эффектов и бонусов, фабрика эффектов и Composite удалены по согласованному KISS-рефакторингу. Профиль содержит прямые prefab-ссылки. Статическая проверка C# пройдена; после рефакторинга Unity импорт, Test Runner, Play Mode и Android build агент не запускал. Gate 8 открыт.

## 1. Импорт и назначения

| Prefab в Prefabs/Gameplay/Bonuses | Компонент на корне |
|---|---|
| ExpandPaddlePickup, бирюзовый | ExpandPaddleEffect |
| AddLifePickup, зелёный | AddLifeEffect |
| DoubleScorePickup, оранжевый | EnableDoubleScoreEffect |

- [ ] Дождаться импорта Unity; нет Missing Script на prefab roots и Missing ссылок в профиле.
- [ ] Все три — variants BonusPickup. На каждом ровно один enabled effect из таблицы; он находится на том же объекте, что BonusPickup.
- [ ] BonusPickup.prefab — шаблон без эффекта. В drop-профиль назначать только конкретные variants; наличие шаблона в Entries должно явно отклоняться.
- [ ] Проверить активный root/BonusPickup, конечный Fall Speed>0; Rigidbody2D Kinematic/Simulated, Y не заморожен; enabled trigger BoxCollider2D. Ссылки Rigidbody/Collider принадлежат корню.
- [ ] Слой Pickup и матрица: Paddle/DeathZone включены, Ball/Brick/Wall/Pickup выключены.
- [ ] Платформа имеет согласованные sprite/collider; расширение удерживает её внутри поля.

Ссылку Effect в Inspector назначать не требуется — компонент находится автоматически на своём корне. GUID прежних AddLifePickup/DoubleScorePickup/BonusPickup сохранены; ExpandPaddlePickup — новый variant. Папки Data/Bonuses/Effects и Definitions больше не используются.

## 2. Профиль выпадения

Профиль `Assets/Content/Data/Bonuses/BonusDropDefinition.asset` сохранён с текущей пользовательской настройкой: Chance=1, одна запись Pickup Prefab=DoubleScorePickup, Weight=1. Профиль и ссылки блоков на него не пересоздавались.

- [ ] Проверить профиль в Inspector: нет Missing или ссылок на удалённый Comeback; каждая запись имеет Pickup Prefab и конечный Weight>0.
- [ ] Для физических сценариев временно ставить Chance=1 и единственную запись нужного бонуса. Менять вариант вне Play Mode.
- [ ] Для проверки трёх AddLife pickups оставить минимум четыре разрушаемых блока: последний не порождает бонус.
- [ ] Проверить Default/Normal, Durable, Shielded; definition без профиля и Indestructible нужны для отрицательных сценариев.
- [ ] После проверки собрать общую таблицу ExpandPaddlePickup/AddLifePickup/DoubleScorePickup, стартовые Weight=1, Chance=0.25. Сохранить профиль; баланс можно изменить позже.

Старая схема через BonusDefinition удалена. Пустая таблица, null prefab, неверная физика/вес/шанс, отсутствие/несколько/disabled root effect отклоняют профиль до random даже при Chance=0. Chance=0 не отключает валидацию.

## 3. Автоматическая регрессия

- [ ] `Window → General → Test Runner`: EditMode → Run All.
- [ ] BonusContentTests (5): правильные компоненты/физика трёх реальных variants, шаблон без эффекта и прямые ссылки профиля.
- [ ] BonusEffectTests (3): AddLife до 5/из 0, DoubleScore без накопления и изменения total/combo. Fixtures фабрики эффектов и Composite удалены вместе с соответствующим кодом.
- [ ] BonusEffectTests, LivesModelTests, BonusDropSettingsTests (15), BonusDropServiceTests (44), BonusDropDefinitionTests (36) и прежние score/combo/brick/game-flow fixtures.
- [ ] PlayMode → Run All. BonusIntegrationTests (35): прямые prefab-ссылки, injection компонентов clone, новый произвольный effect без регистрации/изменения фабрики, три уникальных варианта, cap жизней, physics/pause/DeathZone/lifecycle/disposal/restart.
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

Gate 8 закрывается после подтверждения пользователя. Требования Composite, definitions/assets и ручного Rescue/Comeback отменены. Добавление новых бонусов описано в [руководстве](../ADDING_BONUS.md).
