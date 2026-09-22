# Этап 7. Базовое выпадение и подбор бонуса

[Назад: этап 6](06-brick-hit-chain.md) · [К индексу](../README.md) · [Далее: этап 8](08-bonus-composite.md)

**Результат:** уничтоженный блок может породить pickup, который падает и применяется при контакте с платформой.

## Задачи

- [ ] `P7.1` Определить `BonusDropDefinition`: chance/weight, pickup prefab id, effect definition id.
- [ ] `P7.2` Ввести `IRandomProvider`, чтобы выпадение было воспроизводимо в тестах. Runtime implementation использует Unity random только за adapter boundary.
- [ ] `P7.3` Реализовать `BonusDropService`, который решает только факт/тип выпадения.
- [ ] `P7.4` Создать pickup prefab: движение вниз, collider trigger, визуальный тип, уничтожение в DeathZone.
- [ ] `P7.5` Создать `BonusFactory`, которая порождает pickup через VContainer-aware factory boundary.
- [ ] `P7.6` Реализовать первый одиночный бонус `ExpandPaddle` без преждевременной абстракции группы эффектов.
- [ ] `P7.7` Сбрасывать размер платформы при потере жизни и завершении gameplay scope.
- [ ] `P7.8` Добавить тесты drop chance/weight с fake random и применения одиночного эффекта.

## Gate 7

- [ ] Только отмеченные definitions могут породить бонус.
- [ ] Непойманный pickup корректно уничтожается.
- [ ] Пойманный pickup применяется один раз.
- [ ] Потеря жизни возвращает платформу к базовому размеру.
