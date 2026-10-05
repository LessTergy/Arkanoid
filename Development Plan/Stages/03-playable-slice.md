# Этап 3. Простые блоки и первый вертикальный срез

[Назад: этап 2](02-ball.md) · [К индексу](../README.md) · [Далее: рефакторинг перед этапом 4](03-refactoring.md)

**Результат:** существует минимальная полностью играбельная партия с одним уровнем.

## Задачи

- [x] `P3.1` Создать prefab простого блока с collider, view и идентификатором.
- [x] `P3.2` Реализовать простое правило: одно попадание уничтожает блок и порождает событие `BrickDestroyed`.
- [x] `P3.3` Собрать небольшой тестовый layout вручную, без Addressables и генератора уровня.
- [x] `P3.4` Реализовать `GameSession` с состояниями `Ready`, `Playing`, `LifeLost`, `LevelComplete`, `GameOver`.
- [x] `P3.5` Реализовать `LivesModel` с 3 жизнями и явными событиями изменения.
- [x] `P3.6` Связать DeathZone с потерей жизни: остановить текущий мяч, сбросить позицию платформы/мяча, перейти в `Ready` или `GameOver`.
- [x] `P3.7` Реализовать подсчёт оставшихся разрушаемых блоков и переход в `LevelComplete` при нуле.
- [x] `P3.8` Добавить минимальный HUD: lives, score placeholder и кнопку restart. Отладочный текст состояния впоследствии убран; кнопка pause добавлена для `P3.9`.
- [x] `P3.9` Реализовать pause, блокирующую gameplay input и physics simulation выбранным единообразным способом.
- [x] `P3.10` Добавить EditMode-тесты переходов `GameSession` и `LivesModel`.
- [x] `P3.11` Добавить PlayMode smoke-test: запуск → уничтожение последнего блока → `LevelComplete`.

Перед окончательной проверкой `Gate 3`: [линейный план рефакторинга](03-refactoring.md).

`Brick.prefab` хранится в `Assets/Content/Prefabs/Gameplay`. `BrickIdentity.TypeId` имеет тип `BrickTypeId` (пока значение `Basic`) и одинаков у всех экземпляров prefab. Уничтожение блока и событие `BrickDestroyed` относятся к `P3.2`.

`GameSession` начинается в `Ready`. Допустимые переходы внутри одной сцены: `Ready → Playing`, `Playing → LifeLost/LevelComplete`, `LifeLost → Ready/GameOver`. `LevelComplete` и `GameOver` завершают текущую сессию. `LaunchHandler` разрешает запуск мяча только из `Ready` и переводит сессию в `Playing`.

Для P3.7 каждый вариант уровня собирается как prefab с `LevelView` на корне и активными дочерними `BrickView`. Экземпляр prefab размещается в `Gameplay`, а его `LevelView` назначается в `GameplayLifetimeScope`. `LevelView` считает активные разрушаемые блоки, подписывается на `Destroyed` и сообщает через `Finished`, когда уничтожен последний блок. `LevelFinishedHandler` останавливает мяч и переводит сессию в `LevelComplete`. Пустой уровень считается ошибкой настройки.

Для P3.8 prefab HUD создаётся на Canvas с компонентом `GameplayHudView`, текстами TextMeshPro (`Lives`, `Score`) и кнопками `Restart` и `Pause`. Экземпляр HUD размещается в `Gameplay` и назначается в `GameplayLifetimeScope`. `GameplayHudPresenter` отображает текущие жизни через событие модели; `Score: 0` остаётся заглушкой до этапа 5. Отображение названия состояния убрано как отладочная информация. Кнопка `Restart` вызывает `SceneNavigator.RestartGameplay()`: сцена `Gameplay` загружается заново, поэтому создаются новые `GameSession` в `Ready`, `LivesModel` с тремя жизнями и уровень со всеми блоками. Отдельных методов частичного сброса моделей нет.

Для P3.9 `GameplayPauseController` принимает действие `Pause` и нажатие кнопки HUD. Во время паузы он устанавливает `Time.timeScale = 0`, а `InputSystemPlayerInput` возвращает нейтральное движение и запрещает запуск мяча; повторное нажатие возобновляет игру. Кнопка меняет подпись `Pause`/`Resume`; её текст TextMeshPro нужно назначить в `GameplayHudView`. При restart или уничтожении gameplay scope прежний `timeScale` восстанавливается.

`LivesModel` начинает с 3 жизнями и публикует `LivesChanged` при потере жизни. Для проверки P3.6 добавь `DeathZone` на существующий объект нижнего trigger и укажи этот компонент в поле `Death Zone` на `GameplayLifetimeScope`. Первые два попадания переводят сессию через `LifeLost` в `Ready`; третье — через `LifeLost` в `GameOver`. Мяч останавливается, а платформа и мяч возвращаются к стартовым позициям после каждого попадания.

## Gate 3 — первый playable milestone

- [x] Игрок может начать партию, разбить все блоки, победить, потерять все жизни и проиграть.
- [x] Restart не требует перезапуска Editor/приложения.
- [x] HUD показывает актуальные жизни, заглушку счёта и доступное действие паузы.
- [x] Нет зависимости от будущих бонусов, Addressables или паттернов.
