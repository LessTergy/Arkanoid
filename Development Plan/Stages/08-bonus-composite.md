# Этап 8. Составные бонусы через Composite

[Назад: этап 7](07-bonus-drops.md) · [К индексу](../README.md) · [Архитектура паттернов](../ARCHITECTURE.md#обязательные-паттерны-и-критерии-их-уместности) · [Далее: этап 9](09-addressable-levels.md)

**Результат:** pickup применяет одиночные и составные бонусы через один понятный контракт без специальных проверок типов.

## Задачи

- [ ] `P8.1` Описать три реальных use case: одиночный `ExpandPaddle`, составной `Rescue = ExpandPaddle + AddLife` и вложенный `Comeback = Rescue + EnableDoubleScore`.
- [ ] `P8.2` Определить минимальный `IBonusEffect.Apply(BonusContext)` и перенести `ExpandPaddleEffect` на этот контракт.
- [ ] `P8.3` Реализовать независимые effects: `AddLifeEffect`, `AddScoreEffect`, `EnableDoubleScoreEffect`.
- [ ] `P8.4` Сравнить специальный список effects в `BonusDefinition` и `CompositeBonusEffect`, который сам реализует `IBonusEffect`. Зафиксировать, почему единый контракт нужен для переиспользования группы как дочернего эффекта.
- [ ] `P8.5` Реализовать `CompositeBonusEffect` с `IReadOnlyList<IBonusEffect>` без type checks в pickup; composite должен применяться тем же вызовом, что и leaf.
- [ ] `P8.6` Создать serializable definitions и `BonusEffectFactory`, собирающую runtime effects без передачи container внутрь effect.
- [ ] `P8.7` Создать составной бонус `Rescue`: `ExpandPaddle + AddLife`. Ограничить жизни заданным maximum.
- [ ] `P8.8` Создать вложенный составной бонус `Comeback`: `Rescue + EnableDoubleScore`, чтобы в реальном контенте проверить рекурсивную композицию.
- [ ] `P8.9` Создать одиночный бонус `DoubleScore`, действующий до потери жизни; связать его с Decorator-механизмом расчёта счёта из этапа 5.
- [ ] `P8.10` Определить политику повторного подбора: базовый scope — refresh/no stack для размера и double score.
- [ ] `P8.11` Добавить тесты: leaf, composite и nested composite применяют каждый effect один раз и в ожидаемом порядке; maximum lives и reset on life lost соблюдаются.
- [ ] `P8.12` Кратко записать в README, почему Composite выбран и какую проблему решает общий контракт leaf/group.

## Gate 8

- [ ] Pickup-код зависит только от `IBonusEffect` и не проверяет конкретный тип эффекта.
- [ ] Группировка effects находится в bonus domain/composition-коде, а не в pickup view.
- [ ] `Comeback` содержит `Rescue` как дочерний effect и подтверждает, что leaf и group взаимозаменяемы.
- [ ] Сочетание bonus effect и Decorator-механизма расчёта счёта работает после потери жизни и restart без утечки состояния.
