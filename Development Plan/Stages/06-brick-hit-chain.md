# Этап 6. Обработка попадания в блок через Chain of Responsibility

[Назад: этап 5](05-score-decorator.md) · [К индексу](../README.md) · [Архитектура паттернов](../ARCHITECTURE.md#обязательные-паттерны-и-критерии-их-уместности) · [Далее: этап 7](07-bonus-drops.md)

**Результат:** единый запрос попадания предсказуемо проходит цепочку защитных правил, а Chain of Responsibility не усложняет простые типы блоков.

## Задачи

- [ ] `P6.1` Создать `BrickDefinition`: base score, max health, shield charges, indestructible flag, presentation reference/id.
- [ ] `P6.2` Создать runtime `BrickState`: current health, current shield, destroyed state. Не изменять ScriptableObject во время игры.
- [ ] `P6.3` Определить `BrickHitRequest` и `BrickHitResult` с достаточными данными для feedback и score.
- [ ] `P6.4` Записать порядок правил и точки short-circuit: indestructible прекращает обработку; shield поглощает удар; damage применяется только после снятия защиты.
- [ ] `P6.5` Сравнить один явный `BrickHitProcessor`, таблицу правил и Chain of Responsibility. Зафиксировать, почему независимые handlers и ранняя остановка делают цепочку понятнее для запланированных защитных слоёв.
- [ ] `P6.6` Определить узкий `IBrickHitHandler` и способ передать запрос следующему handler или завершить обработку с `BrickHitResult`.
- [ ] `P6.7` Реализовать `IndestructibleHitHandler`, `ShieldHitHandler` и `DamageHitHandler` без дублирования состояния между слоями.
- [ ] `P6.8` Собирать цепочку в одном factory/composition-коде, а не через условные ветки в `Ball`.
- [ ] `P6.9` Сделать четыре визуально различимых типа: normal, durable, shielded, indestructible.
- [ ] `P6.10` Исключить indestructible blocks из условия завершения уровня.
- [ ] `P6.11` Добавить тесты порядка и short-circuit цепочки: indestructible не получает damage; shield поглощает удар; damage применяется после снятия shield; durable block требует N попаданий.
- [ ] `P6.12` Кратко записать в README, почему Chain of Responsibility выбран и какие дополнительные правила оправдывали бы новый handler.

## Gate 6

- [ ] Ball сообщает только факт столкновения/попадания и не знает разновидностей блоков.
- [ ] Каждый handler либо возвращает конечный результат, либо явно передаёт запрос дальше; short-circuit подтверждён тестами.
- [ ] Результат обработки однозначно определяет feedback, score и состояние блока.
- [ ] Новое защитное правило добавляется в одном месте и не требует менять Ball/UI.
- [ ] Уровень завершается после уничтожения всех разрушаемых блоков.
