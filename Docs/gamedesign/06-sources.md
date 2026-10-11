# Источники, метод и границы анализа

[Оглавление](README.md) · [Механики](01-mechanics.md) · [Баланс](02-balance-and-powerups.md) · [Уровни](03-levels.md) · [Босс](04-boss-and-session.md) · [Перенос](05-deviations-and-transfer.md)

## Приоритет источников

1. Исполняемые выражения и вызовы в `src/` — основа фактических правил клона.
2. Раскладки из `Level::getLayout()` — основа карт и числовой статистики уровней.
3. README автора — заявленный замысел и список отсутствующих функций; он не заменяет проверку ветвей.
4. [StrategyWiki: Arkanoid/Gameplay](https://strategywiki.org/wiki/Arkanoid/Gameplay) — контекст оригинала для сравнения. Прочитана через браузер 11 октября 2026 года; страница указывает последнюю редакцию 27 июня 2023 года. Первичная попытка чтения веб-инструментом получила 403, браузер открыл полный текст.

Текст справочника не воспроизводится целиком. Сопоставление касается управления, отражений, трёх групп блоков и семи бонусов. Параметры босса, пороги жизней, скорости, шанс выпадения и статистика уровней установлены по коду, а не приписаны StrategyWiki.

## Карта кода

| Система | Точки входа |
| --- | --- |
| Размеры и цикл времени | [headers.h](../../src/headers.h#L5), [Silnik::run](../../src/engine.cpp#L25), [main](../../src/main.cpp) |
| Ввод и игровой кадр | [GameplayState::handleEvents](../../src/game_states/gameplay.cpp#L54), [update](../../src/game_states/gameplay.cpp#L466) |
| Платформа | [Paddle ctor](../../src/objects/paddle.cpp#L3), [getCollisionSide](../../src/objects/paddle.cpp#L69), [catchBall](../../src/objects/paddle.cpp#L99) |
| Мяч | [Ball ctor](../../src/objects/ball.cpp#L5), [movement](../../src/objects/ball.cpp#L33), [bouncePaddle](../../src/objects/ball.cpp#L97), [increaseSpeed](../../src/objects/ball.cpp#L141) |
| Блоки | [Brick::setTexture](../../src/objects/brick.cpp#L18), [whichSide](../../src/objects/brick.cpp#L156), [handleHit](../../src/objects/brick.cpp#L181) |
| Раскладки и старт | [Level::setLayout](../../src/level.cpp#L120), [startLevel](../../src/level.cpp#L157), [getLayout](../../src/level.cpp#L224) |
| Выпадение и урон | [GameplayState::checkCollisions](../../src/game_states/gameplay.cpp#L206) |
| Капсула и лазер | [PowerUp::drop](../../src/objects/bonus.cpp#L35), [activatePowerUp](../../src/objects/bonus.cpp#L78), [shootLaser](../../src/objects/bonus.cpp#L125), [Laser::update](../../src/objects/bonus.cpp#L173) |
| Эффекты и переход | [handlePowerUps](../../src/game_states/gameplay.cpp#L317), [checkGameState](../../src/game_states/gameplay.cpp#L363), [reset](../../src/game_states/gameplay.cpp#L396) |
| Очки и жизни | [GameplayState::increaseScore](../../src/game_states/gameplay.cpp#L174), [UpperInterface::increaseScore](../../src/upper_interface.cpp#L27) |
| Сохранение | [saveGame](../../src/game_states/gameplay.cpp#L407), [loadSave](../../src/game_states/gameplay.cpp#L435), [JSON](../../data/scoreboard_data.json) |
| Босс | [Boss](../../src/objects/boss.cpp#L3), [BossState::checkCollisions](../../src/game_states/final_level.cpp#L111), [checkGameState](../../src/game_states/final_level.cpp#L179), [стрельба](../../src/boss_animation_states/boss_shooting.cpp#L11) |
| Сессия | [MenuState::selectOption](../../src/game_states/main_menu.cpp#L235), [GameplayMenu](../../src/gameplay_menu.cpp#L11), [TossCoin](../../src/game_states/toss_coin.cpp#L36), [GameOver](../../src/game_states/game_over.cpp#L49), [Scoreboard](../../src/game_states/scoreboard.cpp#L105) |
| Обратная связь | [изменение формы](../../src/paddle_animation_states/paddle_transformation.cpp#L48), [смерть платформы](../../src/paddle_animation_states/paddle_death.cpp#L10), [смерть босса](../../src/boss_animation_states/boss_death.cpp#L12), [HUD](../../src/upper_interface.cpp#L108) |

Ссылки относительные, чтобы папку документации можно было переносить вместе с проектом. `#L…` — номера строк на момент анализа, пригодные для просмотра в интерфейсах репозитория с поддержкой таких якорей. В локальном Markdown-просмотрщике ссылка может открыть файл без перехода к строке; имя метода остаётся ориентиром.

## Как получены данные уровней

Из 32 ветвей `case 0…31` извлечены все строки `level_layout.insert`. Каждая строка проверена на длину 13 и допустимые значения `_`, 0…9. Начальные пустые строки оставлены: они задают высоту композиции, а не служат форматированием.

Для видимого раунда R:

```text
C = сумма количества блоков типов 0…7
S = количество блоков типа 8
G = количество блоков типа 9
HP серебра = 2 + floor(R/8)
Разрушаемые цели = C + S
Минимальные повреждающие попадания = C + S × HP серебра
Базовые очки = sum(count[type] × (50 + 10 × type), type=0…7) + S × 50R
```

«Минимальные попадания» — сумма HP, а не число отскоков или минимальных нажатий Space. Золото и промахи могут увеличить длительность без изменения этой суммы. Один лазерный объект в этом коде иногда повреждает несколько блоков за кадр, поэтому число выстрелов не обязано совпадать с суммой HP.

Статистика исключает капсулы, Break, загрузку сохранения с ошибочной ценностью серебра и прочие дефекты. Каталог не содержит автоматически назначенного рейтинга сложности: комментарии к геометрии — дизайнерская интерпретация, а не замер прохождения.

## Выполненная проверка

- Прослежены вызовы от главного меню через обычную игру, старт, столкновения, бонусы, жизнь/переход, сохранение, босса и финальные экраны.
- Числа взяты из выражений; расхождения комментариев с ними указаны отдельно.
- Карты и показатели всех 32 уровней сверены с массивами; итоговые количества и очки пересчитаны.
- Локальные ссылки, явные якоря уровней и диапазоны строк исходников проверены автоматически.
- Условные расчёты вероятностей и времени отделены от непосредственно заданных констант.

## Что не установлено

Динамический плейтест не проводился. В каталоге присутствует готовый exe, но его соответствие `src/` не установлено; найденная solution ссылается на отсутствующий `Arkanoid.vcxproj`. Сборка не выполнялась. Поэтому не заявляется проверка FPS, фактического поведения неинициализированных полей, длительности прохождения или стабильности готовой игры.

Исходник не изменялся. Документы сохраняют неоднозначности и дефекты вместо молчаливого исправления правил. Для практического использования в новом проекте сначала отделить выбранный дизайн от ошибок по [разделу переноса](05-deviations-and-transfer.md), затем провести короткий плейтест.
