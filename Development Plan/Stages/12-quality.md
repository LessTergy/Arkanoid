# Этап 12. Тестирование, устойчивость и производительность

[Назад: этап 11](11-feedback-audio.md) · [К индексу](../README.md) · [Далее: этап 13](13-release.md)

**Результат:** основные риски проекта покрыты автоматическими и ручными проверками.

## Задачи

- [ ] `P12.1` Довести EditMode coverage критичных правил: session transitions, lives, score decorators, combo, brick hit chain, drop selection, composite bonus effects, level completion.
- [ ] `P12.2` Держать каждый тест сфокусированным на одном наблюдаемом правиле; не тестировать private implementation details.
- [ ] `P12.3` Добавить PlayMode tests только для границ Unity: physics contact, scope composition, level load/unload, main flow.
- [ ] `P12.4` Выполнить ручную матрицу разрешений минимум 16:9 и 16:10; проверить Canvas scaling и границы камеры.
- [ ] `P12.5` Выполнить 20 последовательных restart/level transitions и проверить Console.
- [ ] `P12.6` Проверить ball edge cases: угол стены, стык colliders, край платформы, высокая скорость, pause во время контакта.
- [ ] `P12.7` Профилировать CPU/GC в Development Build. Исправлять только измеримые проблемы.
- [ ] `P12.8` Проверить Addressables handles и память после полного прохождения и возврата в меню.
- [ ] `P12.9` Проверить отсутствие per-frame allocations в основных `Update/FixedUpdate` loops.
- [ ] `P12.10` Запустить все EditMode/PlayMode tests после clean reimport и сохранить итог в release checklist.

## Gate 12

- [ ] Все тесты зелёные.
- [ ] В типичном gameplay нет повторяющегося GC allocation из собственного кода.
- [ ] Полный run не оставляет ошибок, tween warnings и загруженных level resources после возврата в меню.
