# Этап 9. Data-driven уровни и Addressables

[Назад: этап 8](08-bonus-composite.md) · [К индексу](../README.md) · [Далее: этап 10](10-game-flow-ui.md)

**Результат:** три варианта уровня, собранные как prefabs с `LevelView`, загружаются локально через Addressables, сменяются без утечек и не требуют ручного размещения блоков в сцене.

## Задачи

- [ ] `P9.1` Определить идентификаторы и порядок prefab-вариантов уровня для прохождения.
- [ ] `P9.2` Подготовить три prefab-варианта с `LevelView` и дочерними блоками, не смешивая их с grid layout.
- [ ] `P9.3` Реализовать создание и уничтожение загруженного prefab уровня через фабрику на границе DI.
- [ ] `P9.4` После проверки загрузки убрать сценовый экземпляр тестового уровня этапа 3.
- [ ] `P9.5` Создать локальные Addressables groups: `LevelPrefabs`, `SharedGameplay` с осмысленными labels/addresses.
- [ ] `P9.6` Определить `ILevelLoader` и `AddressablesLevelLoader`.
- [ ] `P9.7` Возвращать из loader явный lease/loaded-level object, владеющий operation handles. Не отдавать наружу «голый» asset без владельца.
- [ ] `P9.8` При создании prefab из загруженного asset сохранять handle до уничтожения всех instances; не освобождать asset сразу после `Instantiate`.
- [ ] `P9.9` Связать загрузку UniTask с cancellation token gameplay/level scope. Обработать cancel отдельно от реальной ошибки.
- [ ] `P9.10` При смене уровня: остановить gameplay → уничтожить level instances/scope → освободить handles → загрузить следующий level → начать `Ready`.
- [ ] `P9.11` Добавить loading overlay и error state с `Retry`/`Back to Menu`; не скрывать исключение пустым catch.
- [ ] `P9.12` Создать 3 уровня с растущей сложностью и гарантированно разрушаемым layout.
- [ ] `P9.13` Выполнить Addressables content build и player build из чистого состояния.
- [ ] `P9.14` Проверить повторный цикл `Level 1 → 2 → 3 → restart → menu` и Memory Profiler/Addressables Event Viewer на удержанные handles.

## Gate 9

- [ ] Gameplay scene не содержит вручную расставленных level blocks.
- [ ] Все 3 уровня проходят по очереди.
- [ ] Ошибка загрузки приводит в управляемое UI-состояние.
- [ ] Каждый успешный load имеет симметричный release после уничтожения instances.
